using UnityEditor;

namespace FlowerGit
{
    /// <summary>
    /// Assets database monitoring.
    /// </summary>
    public class AssetsWatcher : AssetPostprocessor
    {
        #region DEFINITION
        public delegate void AssetsWatcherEvent();
        public static event AssetsWatcherEvent onChanged;
        #endregion

        #region UNITY_EVENT
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            onChanged?.Invoke();
        }
        #endregion

    }
}
