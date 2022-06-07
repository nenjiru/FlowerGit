using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowerGit
{
    /// <summary>
    /// Draw each status.
    /// </summary>
    public class StatusWindowDrawer
    {
        #region VARIABLE
        private static bool _recentLogExpanded;
        private static string _commitMessage;
        #endregion

        #region PUBLIC_METHODS
        /// <summary>
        /// Draw recent commit log.
        /// </summary>
        public static void drawRecentLog(CommitLog[] logs)
        {
            _recentLogExpanded = EditorGUILayout.Foldout(_recentLogExpanded, SectionTitle.commitLog, true, StyleSet.foldOut);
            if (!_recentLogExpanded)
            {
                return;
            }
            _drawCommitLog(logs);
        }

        /// <summary>
        /// Draw un pushed commit list.
        /// </summary>
        public static void drawCommitList(CommitLog[] logs)
        {
            EditorGUILayout.LabelField(SectionTitle.commitList);
            _drawCommitLog(logs);
        }

        /// <summary>
        /// Draw staging list.
        /// </summary>
        /// <param name="statuses"></param>
        public static void drawStagingList(Status[] statuses)
        {
            EditorGUILayout.LabelField(SectionTitle.stagingList);

            if (statuses.Length == 0)
            {
                _blankBox();
                return;
            }

            foreach (Status item in statuses)
            {
                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    _fileHeader(item);
                    if (GUILayout.Button(TextLabel.cancel))
                    {
                        GitUtils.Restore(item.path, "--staged");
                        _updateStatus();
                    }
                }
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(TextLabel.commitMessageTitle);
            _commitMessage = EditorGUILayout.TextArea(_commitMessage, GUILayout.Height(50));
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("");
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_commitMessage));
                if (GUILayout.Button(TextLabel.commit, GUILayout.ExpandWidth(false)))
                {
                    GitUtils.Commit(_commitMessage);
                    _commitMessage = "";
                    _updateStatus();
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Draw a working list.
        /// </summary>
        /// <param name="statuses"></param>//
        public static void drawWorkingList(Status[] statuses)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(SectionTitle.workingList);

                if (GUILayout.Button(TextLabel.workingDetection, GUILayout.ExpandWidth(false)))
                {
                    _updateStatus();
                }
            }

            if (statuses.Length == 0)
            {
                _blankBox();
                return;
            }

            var hasConflict = statuses.Any(item => (item.condition == Status.Condition.CONFLICT));
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(statuses.Length.ToString() + " Files");
                EditorGUI.BeginDisabledGroup(hasConflict);
                if (GUILayout.Button(TextLabel.stagingAll))
                {
                    GitUtils.Add(".");
                    _updateStatus();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }

            foreach (Status item in statuses)
            {
                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    _fileHeader(item);
                    if (item.condition != Status.Condition.CONFLICT)
                    {
                        if (GUILayout.Button(TextLabel.staging))
                        {
                            GitUtils.Add(item.path);
                            _updateStatus();
                        }
                        EditorGUI.BeginDisabledGroup(item.condition != Status.Condition.DETECTED);
                        if (GUILayout.Button(TextLabel.reset))
                        {
                            GitUtils.Restore(item.path);
                            _updateStatus();
                            AssetDatabase.Refresh();
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        if (GUILayout.Button(TextLabel.resolveTheirs))
                        {
                            GitUtils.Resolve("--theirs", item.path);
                            _updateStatus();
                        }
                        if (GUILayout.Button(TextLabel.resolveOurs))
                        {
                            GitUtils.Resolve("--ours", item.path);
                            _updateStatus();
                        }
                    }
                }
            }
        }
        #endregion

        #region PRIVATE_METHODS
        private static void _updateStatus()
        {
            StatusManager.Update();
        }

        private static void _blankBox()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("");
            EditorGUILayout.EndVertical();
        }

        private static void _drawCommitLog(CommitLog[] logs)
        {
            if (logs.Length == 0)
            {
                _blankBox();
                return;
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                foreach (var item in logs)
                {
                    var log = $"<color=#999933>{item.hash}</color> {item.message}";
                    EditorGUILayout.LabelField(log, StyleSet.richStyle);
                }
            }
        }

        private static void _fileHeader(Status item)
        {
            GUILayout.Box(item.icon, GUILayout.Height(40), GUILayout.Width(40));
            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button(item.label + " " + item.name, StyleSet.richStyle))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(item.path));
                }
                EditorGUILayout.LabelField(item.path, EditorStyles.miniLabel);
            }
        }
        #endregion
    }
}