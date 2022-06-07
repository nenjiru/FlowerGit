using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowerGit
{
    /// <summary>
    /// Status and file information.
    /// </summary>
    public class FileStatus
    {
        public string status { get; private set; }
        public string path { get; private set; }
        public string name { get; private set; }
        public string extension { get; private set; }
        public FileStatus(string state)
        {
            status = state.Substring(0, 2);
            path = state.Substring(3).Trim('"');
            if (status.Contains("R"))
            {
                var p = state.Substring(3);
                path = p.Substring(p.IndexOf("-> ") + 3).Trim('"');
            }
            name = Path.GetFileName(path);
            extension = Path.GetExtension(path);
        }
    }

    /// <summary>
    /// Commit log.
    /// </summary>
    public class CommitLog
    {
        public string hash { get; private set; }
        public string message { get; private set; }
        public CommitLog(string log)
        {
            var split = log.IndexOf(' ');
            hash = log.Substring(0, split);
            message = log.Substring(split + 1);
        }
    }

    /// <summary>
    /// Git command wrapper.
    /// </summary>
    public static class GitUtils
    {
        #region DEFINITION
        public delegate void AsyncEvent(string result, bool error);
        public static event AsyncEvent onInitComplete;
        public static event AsyncEvent onPullComplete;
        public static event AsyncEvent onPushComplete;
        public delegate void BothAsyncEvent(string pull, string push, bool error);
        public static event BothAsyncEvent onBothComplete;
        private static readonly string gitPath = Path.Combine(Directory.GetCurrentDirectory(), ".git");
        private static readonly string fetchHeadPath = Path.Combine(gitPath, "FETCH_HEAD");
        #endregion

        #region VARIABLE
        private static ProcessStartInfo _startInfo = new ProcessStartInfo
        {
            FileName = "git",
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
        };
        private static FileStatus[] _fileStatusCache = new FileStatus[0];
        private static string _rawStatusCache = "";
        private static CommitLog[] _recentLogCache = new CommitLog[0];
        private static string _rawRecentCache = "";
        private static CommitLog[] _commitLogCache = new CommitLog[0];
        private static string _rawCommitCache = "";
        #endregion

        #region EXTENSION_METHODS
        static Task RunAsync(this Process process)
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(null);
            if (!process.Start()) tcs.SetException(new Exception("Failed to start process."));
            return tcs.Task;
        }
        #endregion

        #region PUBLIC_METHODS
        public static (string result, int code) Execute(string arguments)
        {
            _startInfo.Arguments = arguments;
            using (var process = new Process())
            {
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                var timeout = (int)TimeSpan.FromMinutes(2).TotalMilliseconds;

                process.StartInfo = _startInfo;
                process.OutputDataReceived += (s, e) => { if (e.Data != null) { stdout.AppendLine(e.Data); } };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) { stderr.AppendLine(e.Data); } };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                process.CancelOutputRead();
                process.CancelErrorRead();

                var error = stderr.ToString();
                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError(error);
                }
                return (stdout.ToString().TrimEnd(), process.ExitCode);
            }
        }

        public static async Task<(string result, int code)> ExecuteAsync(string arguments)
        {
            _startInfo.Arguments = arguments;
            using (var process = new Process())
            {
                process.StartInfo = _startInfo;
                await process.RunAsync();
                var stdout = await process.StandardOutput.ReadToEndAsync();
                var stderr = await process.StandardError.ReadToEndAsync();
                var error = stderr.ToString();
                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError(error);
                }
                return (stdout.ToString().TrimEnd(), process.ExitCode);
            };
        }

        public static FileStatus[] GetStatus()
        {
            var raw = Execute("status --short --untracked-files=all").result;
            if (raw == _rawStatusCache)
            {
                return _fileStatusCache;
            }

            var status = _splitLogs(raw);
            var result = new FileStatus[status.Length];
            for (int i = 0; i < status.Length; i++)
            {
                result[i] = new FileStatus(status[i]);
            }

            _rawStatusCache = raw;
            _fileStatusCache = result;
            return result;
        }

        public static CommitLog[] GetRecentLog(string remote, int max = 10)
        {
            var log = Execute($"log --oneline --max-count {max} {remote}").result;
            if (log == _rawRecentCache)
            {
                return _recentLogCache;
            }

            var logs = _splitLogs(log);
            var result = _commitLog(logs);

            _rawRecentCache = log;
            _recentLogCache = result;
            return result;
        }

        public static CommitLog[] GetCommitLog(string remote)
        {
            var log = Execute($"log --oneline {remote}..HEAD").result;
            if (log == _rawCommitCache)
            {
                return _commitLogCache;
            }

            var logs = _splitLogs(log);
            var result = _commitLog(logs);

            _rawCommitCache = log;
            _commitLogCache = result;
            return result;
        }

        public static string LastUpdate(string format = "yyyy/M/d HH:mm")
        {
            return File.GetLastWriteTime(fetchHeadPath).ToString(format);
        }

        public static string Add(string path)
        {
            return Execute($"add \"{path}\"").result;
        }

        public static string Restore(string path, string option = "")
        {
            return Execute($"restore {option} \"{path}\"").result;
        }

        public static string Commit(string message)
        {
            return Execute($"commit -m \"{message}\"").result;
        }

        public static string Resolve(string target, string path)
        {
            Execute($"checkout {target} {path}");
            return Execute("commit -am \"Fixed Conflict\"").result;
        }

        public static async Task<string> RemoteHeadBranch()
        {
            var origin = Execute("config --local --get branch.main.remote").result;
            var remote = await ExecuteAsync("remote show origin");
            Match match = Regex.Match(remote.result, @"HEAD branch: (.*)");
            return $"{origin}/{match.Groups[1].Value}";
        }

        public static async void InitAsync(string url)
        {
            Execute("init");
            Execute("branch -m main");
            Execute($"remote add origin {url}");
            var fetch = await ExecuteAsync("fetch");
            var checkout = Execute("checkout main");
            await Task.Delay(1000);
            onInitComplete?.Invoke(fetch.result, (fetch.code + checkout.code) != 0);
        }

        public static async void BothAsync()
        {
            var pull = await ExecuteAsync("pull --no-edit");
            onPullComplete?.Invoke(pull.result, pull.code != 0);
            var push = await ExecuteAsync("push");
            onPushComplete?.Invoke(push.result, push.code != 0);
            await Task.Delay(300);
            onBothComplete?.Invoke(pull.result, push.result, (pull.code + push.code) != 0);
        }

        public static async void PullAsync()
        {
            var pull = await ExecuteAsync("pull --no-edit");
            onPullComplete?.Invoke(pull.result, pull.code != 0);
        }

        public static async void PushAsync()
        {
            var push = await ExecuteAsync("push");
            onPushComplete?.Invoke(push.result, push.code != 0);
        }

        public static void DeleteRepository()
        {
            if (Directory.Exists(gitPath))
            {
                _deleteDirectory(gitPath);
            }
        }
        #endregion

        #region PRIVATE_METHODS
        static CommitLog[] _commitLog(string[] logs)
        {
            var result = new CommitLog[logs.Length];
            for (int i = 0; i < logs.Length; i++)
            {
                result[i] = new CommitLog(logs[i]);
            }
            return result;
        }

        static string[] _splitLogs(string log)
        {
            return log.Replace("\r\n", "\n").Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        static void _deleteDirectory(string targetPath)
        {
            string[] files = Directory.GetFiles(targetPath);
            foreach (string path in files)
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }

            string[] directories = Directory.GetDirectories(targetPath);
            foreach (string path in directories)
            {
                _deleteDirectory(path);
            }

            Directory.Delete(targetPath, false);
        }
        #endregion
    }
}