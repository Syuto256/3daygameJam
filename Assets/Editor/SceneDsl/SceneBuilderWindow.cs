using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    /// Tools > Scene DSL > Scene Builder（Ctrl + Alt + B）から開くエディタウィンドウ。
    /// DSL テキストを貼り付け／ファイル読み込みし、検証・ビルドを行う。
    /// </summary>
    public class SceneBuilderWindow : EditorWindow
    {
        [SerializeField] private string dslText = "";
        [SerializeField] private Vector2 editorScroll;
        [SerializeField] private Vector2 logScroll;
        [SerializeField] private string lastLogText = "";
        [SerializeField] private bool lastRunHadError;

        // %&b = Ctrl + Alt + B。
        [MenuItem("Tools/Scene DSL/Scene Builder %&b", false, 60)]
        public static void Open()
        {
            var window = GetWindow<SceneBuilderWindow>("Scene Builder");
            window.minSize = new Vector2(480, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("シーン構築 DSL", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Object { Name / Type / Parent / Component: X { key: value } } 形式のテキストを貼り付けてください。\n" +
                "「Validate」で保存前チェック、「Build Scene」で実際にシーンへ配置します。",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("ファイルから読み込み", GUILayout.Width(160)))
                {
                    string path = EditorUtility.OpenFilePanel("DSL ファイルを選択", Application.dataPath, "txt");
                    if (!string.IsNullOrEmpty(path))
                    {
                        dslText = File.ReadAllText(path, Encoding.UTF8);
                    }
                }
                if (GUILayout.Button("ファイルへ保存", GUILayout.Width(120)))
                {
                    string path = EditorUtility.SaveFilePanel("DSL ファイルを保存", Application.dataPath, "scene", "txt");
                    if (!string.IsNullOrEmpty(path))
                    {
                        File.WriteAllText(path, dslText, new UTF8Encoding(false));
                        AssetDatabase.Refresh();
                    }
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("クリア", GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("確認", "DSL テキストを空にします。よろしいですか？", "はい", "キャンセル"))
                    {
                        dslText = "";
                    }
                }
            }

            editorScroll = EditorGUILayout.BeginScrollView(editorScroll, GUILayout.MinHeight(220));
            dslText = EditorGUILayout.TextArea(dslText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate（シーンは変更しません）", GUILayout.Height(28)))
                {
                    var log = SceneDslBuilder.Validate(dslText);
                    ShowLog(log);
                }

                GUI.backgroundColor = new Color(0.65f, 0.85f, 1f);
                if (GUILayout.Button("Build Scene", GUILayout.Height(28)))
                {
                    if (EditorUtility.DisplayDialog("Build Scene",
                        "DSL の内容に従ってシーンへオブジェクトを生成します。\n（Undo で取り消せます）続行しますか？",
                        "実行", "キャンセル"))
                    {
                        var result = SceneDslBuilder.Build(dslText);
                        ShowLog(result.Log);
                        if (result.Log.ErrorCount == 0)
                        {
                            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                            EditorSceneManagerMarkDirty(activeScene);
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(lastRunHadError ? "結果（エラーあり）" : "結果", EditorStyles.boldLabel);

            logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.MinHeight(160));
            EditorGUILayout.TextArea(lastLogText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private static void EditorSceneManagerMarkDirty(UnityEngine.SceneManagement.Scene scene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        private void ShowLog(BuildLog log)
        {
            var sb = new StringBuilder();
            foreach (var entry in log.Entries)
            {
                string prefix = entry.Level == LogLevel.Error ? "[ERROR] " :
                                 entry.Level == LogLevel.Warning ? "[WARN]  " : "[INFO]  ";
                sb.AppendLine(prefix + entry.Message);
            }
            lastLogText = sb.ToString();
            lastRunHadError = log.ErrorCount > 0;
            Repaint();
        }
    }
}
