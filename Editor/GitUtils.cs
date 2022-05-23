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
        private static readonly string fetchHeadPath = Path.Combine(Directory.GetCurrentDirectory(), ".git", "FETCH_HEAD");
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
        public static string Execute(string arguments)
        {
            _startInfo.Arguments = arguments;
            using (var process = Process.Start(_startInfo))
            {
                process.WaitForExit();
                var error = process.StandardError.ReadToEnd().TrimEnd();
                if (string.IsNullOrEmpty(error) || error.IndexOf("warning:") == 0)
                {
                    return process.StandardOutput.ReadToEnd().TrimEnd();
                }
                else
                {
                    UnityEngine.Debug.LogError(error);
                    return error;
                };
            }
        }

        public static async Task<(string result, int code)> ExecuteAsync(string arguments)
        {
            _startInfo.Arguments = arguments;
            using (var process = new Process())
            {
                process.StartInfo = _startInfo;
                await process.RunAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                var result = (!string.IsNullOrEmpty(output)) ? output.TrimEnd() : error.TrimEnd();
                if (process.ExitCode != 0 && !string.IsNullOrEmpty(result))
                {
                    UnityEngine.Debug.LogError(error);
                }
                return (result, process.ExitCode);
            };
        }

        public static FileStatus[] GetStatus()
        {
            var raw = Execute("status --short --untracked-files=all");
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
            var log = Execute($"log --oneline --max-count {max} {remote}");
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
            var log = Execute($"log --oneline {remote}..HEAD");
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
            return Execute($"add \"{path}\"");
        }

        public static string Restore(string path, string option = "")
        {
            return Execute($"restore {option} \"{path}\"");
        }

        public static string Commit(string message)
        {
            return Execute($"commit -m \"{message}\"");
        }

        public static string Resolve(string target, string path)
        {
            Execute($"checkout {target} {path}");
            return Execute("commit -am \"Fixed Conflict\"");
        }

        public static async Task<string> RemoteHeadBranch()
        {
            var origin = Execute("config --local --get branch.main.remote");
            var remote = await ExecuteAsync("remote show origin");
            Match matche = Regex.Match(remote.result, @"HEAD branch: (.*)");
            return $"{origin}/{matche.Groups[1].Value}";
        }

        public static async void InitAsync(string url)
        {
            GitUtils.Execute("init");
            GitUtils.Execute("branch -m main");
            GitUtils.Execute($"remote add origin {url}");
            var fetch = await GitUtils.ExecuteAsync("fetch");
            GitUtils.Execute("checkout main");
            await Task.Delay(1000);
            onInitComplete?.Invoke(fetch.result, fetch.code != 0);
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
        #endregion
    }
}