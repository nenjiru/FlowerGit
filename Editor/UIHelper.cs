using UnityEditor;
using UnityEngine;

namespace FlowerGit
{
    internal static class TextLabel
    {
        public readonly static string cancel = "キャンセル";
        public readonly static string commit = "コミット";
        public readonly static string file = "ファイル";
        public readonly static string staging = "確定";
        public readonly static string reset = "リセット";
        public readonly static string syncCompleteMessage = "同期が正常に完了しました";
        public readonly static string workingDetection = "変更を検出";
        public readonly static string remoteSynchronize = "リモートと同期";
        public readonly static string commitMessageTitle = "メッセージ（必須）";
        public readonly static string resolveTheirs = "相手の変更を適用";
        public readonly static string resolveOurs = "自分の変更を適用";
    }

    internal static class StatusLabel
    {
        public readonly static string conflict = "<color=red>コンフリクト</color>";
        public readonly static string added = "<color=cyan>新規</color>";
        public readonly static string modified = "<color=orange>更新</color>";
        public readonly static string deleted = "<color=magenta>削除</color>";
    }

    internal static class FileIcon
    {
        public readonly static Texture metaIcon = EditorGUIUtility.IconContent("MetaFile Icon").image;
        public readonly static Texture settingIcon = EditorGUIUtility.IconContent("EditorSettings Icon").image;
        public readonly static Texture defaultIcon = EditorGUIUtility.IconContent("DefaultAsset Icon").image;
        public readonly static Texture refresh = EditorGUIUtility.IconContent("Refresh").image;
        public readonly static Texture cloud = EditorGUIUtility.IconContent("CloudConnect").image;
    }

    internal static class SectionTitle
    {
        public static GUIContent commitList = new GUIContent("確定コミット");
        public static GUIContent stagingList = new GUIContent("コミット候補");
        public static GUIContent workingList = new GUIContent("作業ファイル");
        static SectionTitle()
        {
            commitList.image = EditorGUIUtility.IconContent("PreMatCylinder").image;
            stagingList.image = EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow").image;
            workingList.image = EditorGUIUtility.IconContent("Search Icon").image;
        }
    }

    internal static class StyleSet
    {
        public static GUIStyle richStyle = new GUIStyle(EditorStyles.label) { richText = true };
        public static GUIStyle multiLine = new GUIStyle(EditorStyles.label) { wordWrap = true };
        public static GUIStyle note = new GUIStyle() { alignment = TextAnchor.MiddleLeft, fontSize = 11 };
        public static Color lineColor = new Color(0.15f, 0.15f, 0.15f, 1.0f);
        static StyleSet()
        {
            note.normal.textColor = Color.gray;
        }

        public static void DrawSeparator(Rect position)
        {
            EditorGUILayout.Space(3);
            var lineRect = EditorGUILayout.GetControlRect(false, GUILayout.Height(1));
            lineRect.x = 0;
            lineRect.width = position.width;
            EditorGUI.DrawRect(lineRect, lineColor);
            EditorGUILayout.Space();
        }
    }
}
