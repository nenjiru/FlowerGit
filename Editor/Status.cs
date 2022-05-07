using UnityEditor;
using UnityEngine;

namespace FlowerGit
{
    /// <summary>
    /// Formatted git status data.
    /// </summary>
    public class Status
    {
        #region DEFINITION
        public enum Condition
        {
            DETECTED,
            UNTRACKED,
            CONFLICT,
        }

        public enum Stage
        {
            STAGED,
            WORKING,
        }
        #endregion

        #region VARIABLE
        public string status { get; private set; }
        public string path { get; private set; }
        public string name { get; private set; }
        public Stage stage { get; private set; }
        public Condition condition { get; private set; }
        public Texture icon { get; private set; }
        public string label { get; private set; }
        #endregion

        #region PUBLIC_METHODS
        public Status(FileStatus item, Stage stage)
        {
            this.status = item.status;
            this.path = item.path;
            this.name = item.name;
            this.stage = stage;

            if (item.status.Contains("??"))
            {
                this.condition = Condition.UNTRACKED;
            }
            else if (item.status.Contains("UU"))
            {
                this.condition = Condition.CONFLICT;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(this.path);
            this.icon = AssetPreview.GetMiniThumbnail(asset);
            if (this.icon == null && !this.status.Contains("D"))
            {
                switch (item.extension)
                {
                    case ".meta": this.icon = FileIcon.metaIcon; break;
                    case ".asset": this.icon = FileIcon.settingIcon; break;
                    default: this.icon = FileIcon.defaultIcon; break;
                }
            }

            this.label = _label();
        }

        public static bool isStaged(string status)
        {
            if (status.Contains("??") || status.Contains("UU"))
            {
                return false;
            }
            return !string.IsNullOrWhiteSpace(status.Substring(0, 1));
        }

        public static bool isWorking(string status)
        {
            return !string.IsNullOrWhiteSpace(status.Substring(1, 1));
        }
        #endregion

        #region PRIVATE_METHODS
        string _label()
        {
            if (this.condition == Condition.CONFLICT)
            {
                return StatusLabel.conflict;
            }
            else if (this.condition == Condition.UNTRACKED)
            {
                return StatusLabel.added;
            }
            else
            {
                var state = this.status.Substring((int)this.stage, 1);
                switch (state)
                {
                    case "A": return StatusLabel.added;
                    case "D": return StatusLabel.deleted;
                    default: return StatusLabel.modified;
                }
            }
        }
        #endregion
    }
}