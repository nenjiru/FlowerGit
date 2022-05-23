using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlowerGit
{
    /// <summary>
    /// Organize raw status.
    /// </summary>
    public class StatusManager
    {
        #region DEFINITION
        public delegate void StatusManagerEvent();
        public static event StatusManagerEvent onUpdate;
        #endregion

        #region VARIABLE
        public static string remoteBranch = "origin/main";
        public static Status[] staged = new Status[0];
        public static Status[] working = new Status[0];
        public static CommitLog[] recentLogs = new CommitLog[0];
        public static CommitLog[] commitLogs = new CommitLog[0];
        private static FileStatus[] _statusCache = new FileStatus[0];
        private static List<Status> _stagedList = new List<Status>();
        private static List<Status> _workingList = new List<Status>();
        #endregion

        #region PUBLIC_METHODS
        /// <summary>
        /// Status clear.
        /// </summary>
        public static void Clear()
        {
            _statusCache = new FileStatus[0];
            _stagedList.Clear();
            _workingList.Clear();
            staged = _stagedList.ToArray();
            working = _workingList.ToArray();
        }

        /// <summary>
        /// Get status and sort.
        /// </summary>
        public static void Update()
        {
            recentLogs = GitUtils.GetRecentLog(remoteBranch, 10);
            commitLogs = GitUtils.GetCommitLog(remoteBranch);

            var status = GitUtils.GetStatus();
            if (status.SequenceEqual(_statusCache))
            {
                onUpdate?.Invoke();
                return;
            }

            _stagedList.Clear();
            _workingList.Clear();
            _statusCache = status;

            foreach (var item in status)
            {
                if (Status.isStaged(item.status))
                {
                    var state = new Status(item, Status.Stage.STAGED);
                    _stagedList.Add(state);
                }
                if (Status.isWorking(item.status))
                {
                    var state = new Status(item, Status.Stage.WORKING);
                    _workingList.Add(state);
                }
            }

            staged = _stagedList.ToArray();
            working = _workingList.ToArray();
            onUpdate?.Invoke();
        }
        #endregion
    }
}