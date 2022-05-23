using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace FlowerGit
{
    /// <summary>
    /// Git simple operation window.
    /// </summary>
    public class StatusWindow : EditorWindow
    {
        #region VARIABLE
        static Vector2 _scrollPos;
        static bool _isRepository;
        static string _remoteURL;
        static string _currentBranch;
        static string _progress;
        static string _lastUpdate;
        #endregion

        #region UNITY_EVENT
        [MenuItem("Assets/Flower Git")]
        public static void Open()
        {
            GetWindow<StatusWindow>("Flower Git");
        }

        void OnEnable()
        {
            var currentBranch = GitUtils.Execute("symbolic-ref --short HEAD");
            _isRepository = !currentBranch.Contains("fatal: not a git repository");
            if (_isRepository)
            {
                Init();
            }
        }

        void OnDisable()
        {
            Deinit();
        }

        void OnInspectorUpdate()
        {
            if (!string.IsNullOrEmpty(_progress))
            {
                _progress += " .";
                Repaint();
            }
        }

        void OnGUI()
        {
            if (!_isRepository)
            {
                _gitInit();
                _drawProgress();
                return;
            }

            if (GUILayout.Button($"Remote URL: <color=#3399ff>{_remoteURL}</color>", StyleSet.richStyle))
            {
                Application.OpenURL(_remoteURL);
            }
            EditorGUILayout.LabelField($"Branch: {StatusManager.remoteBranch}  {_currentBranch}");
            StyleSet.DrawSeparator(position);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(TextLabel.remoteSynchronize, GUILayout.ExpandWidth(false)))
                {
                    _progress = "Updating";
                    GitUtils.BothAsync();
                }
                EditorGUILayout.LabelField($"     Last update: {_lastUpdate}", StyleSet.note);
            }

            _drawProgress();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(3);
            StatusWindowDrawer.drawRecentLog(StatusManager.recentLogs);

            EditorGUILayout.Space();
            StatusWindowDrawer.drawCommitList(StatusManager.commitLogs);

            EditorGUILayout.Space();
            StatusWindowDrawer.drawStagingList(StatusManager.staged);

            EditorGUILayout.Space();
            StatusWindowDrawer.drawWorkingList(StatusManager.working);

            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region PUBLIC_METHODS
        /// <summary>
        /// Initialize.
        /// </summary>
        public static void Init()
        {
            AssetsWatcher.onChanged += _updateStatus;
            GitUtils.onBothComplete += _onBothComplete;
            _remoteURL = GitUtils.Execute("config --local --get remote.origin.url");
            _currentBranch = GitUtils.Execute("symbolic-ref --short HEAD");
            _lastUpdate = GitUtils.LastUpdate();
            _updateStatus();
        }

        /// <summary>
        /// Finalize.
        /// </summary>
        public static void Deinit()
        {
            AssetsWatcher.onChanged -= _updateStatus;
            GitUtils.onBothComplete -= _onBothComplete;
            StatusManager.Clear();
            _remoteURL = null;
            _currentBranch = null;
            _lastUpdate = null;
            _isRepository = false;
        }
        #endregion

        #region PRIVATE_METHODS
        static void _updateStatus()
        {
            StatusManager.Update();
        }

        static void _drawProgress()
        {
            if (!string.IsNullOrEmpty(_progress))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(_progress, StyleSet.multiLine);
            }
        }

        static void _gitInit()
        {
            EditorGUILayout.LabelField("Remote URL");
            using (new EditorGUILayout.HorizontalScope())
            {
                _remoteURL = EditorGUILayout.TextField("", _remoteURL);
                var ext = Path.GetExtension(_remoteURL);
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(ext) || ext != ".git");
                if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
                {
                    _progress = "Initialize";
                    GitUtils.onInitComplete += _onInitComplete;
                    GitUtils.InitAsync(_remoteURL.Trim());
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        static void _onInitComplete(string result, bool error)
        {
            _progress = null;
            var message = result;
            if (!error)
            {
                message = $"{TextLabel.syncCompleteMessage}\n{message}";
                _isRepository = true;
            }
            EditorUtility.DisplayDialog("Git message", message, "OK");
            GitUtils.onInitComplete -= _onInitComplete;
            Init();
        }

        static void _onBothComplete(string pull, string push, bool error)
        {
            _progress = null;
            var separator = new[] { "\r\n", "\n", "\r" };
            pull = pull.Split(separator, System.StringSplitOptions.None).FirstOrDefault();
            push = push.Split(separator, System.StringSplitOptions.None).FirstOrDefault();
            var message = $"Pull: {pull}\nPush: {push}";
            if (!error)
            {
                message = $"{TextLabel.syncCompleteMessage}\n{message}";
                _lastUpdate = GitUtils.LastUpdate();
            }
            EditorUtility.DisplayDialog("Git message", message, "OK");
            _updateStatus();
        }
        #endregion
    }
}