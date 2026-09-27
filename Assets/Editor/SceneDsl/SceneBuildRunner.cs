using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    ///     DSL ファイルから Imata シーンを組み立てるツール。
    ///     Scene Builder ウィンドウを開いて手で貼り付ける手間をなくすためのもの。
    ///
    ///     組み立て先は Imata シーンだけ（SceneDslPaths.TargetScenePath）。
    ///     他のシーンはチームメンバーが手で編集しているので、ここからは触らない。
    ///
    ///     動作:
    ///       1. Imata シーンを開く（手で置いたものは残る）
    ///       2. シーンプレハブ（Assets/Prefab/Scene/Imata.prefab）があれば、それを置いて土台にする
    ///       3. DSL に書かれた名前と同名のオブジェクトを、階層のどこにあっても削除する
    ///          （何度実行しても重複せず、常に新しいものへ置き換わる）
    ///          ただし Type: Existing の名前は消さず、既存オブジェクトへ値を書き込む相手として使う
    ///       4. DSL をビルドする
    ///       5. シーンを保存する
    /// </summary>
    public static class SceneBuildRunner
    {
        private class SceneBuildDefinition
        {
            public string DisplayName;
            public string DslPath;
            public string ScenePath;

            /// <summary>取り込む作業シーン。プレハブとの繋がりを保ったまま中身を移す</summary>
            public string[] MergeScenePaths = new string[0];

            /// <summary>組み立て後、他のルートをまとめる親の名前。null なら何もしない</summary>
            public string RootObjectName;

            /// <summary>
            ///     土台にするシーンプレハブ。未指定なら ScenePrefabDirectory/DisplayName.prefab を探す。
            ///     見つかればそれを置いてから、DSL の中身をその子階層へ組み立てる
            /// </summary>
            public string ScenePrefabPath;

            /// <summary>
            ///     既存シーンを開かず、毎回空のシーンから組み立て直す。
            ///     作業シーンを取り込む場合は、実行のたびに中身が二重になるので必ず true にする
            /// </summary>
            public bool RebuildFromEmpty;
        }

        private const string ImataName = "Imata";

        /// <summary>
        ///     組み立て対象は Imata シーンだけ。
        ///     既存シーンを開いて、DSL に書かれた名前のオブジェクトだけを作り直す
        ///     （RebuildFromEmpty を付けていないので、手で置いたものは残る）
        /// </summary>
        private static readonly SceneBuildDefinition ImataDefinition = new()
        {
            DisplayName = ImataName,
            DslPath = SceneDslPaths.TargetDslPath,
            ScenePath = SceneDslPaths.TargetScenePath
        };

        // %&g = Ctrl + Alt + G
        [MenuItem("Tools/Scene DSL/Build Imata Scene %&g", false, 0)]
        public static void BuildImataScene()
        {
            if (!CanRun()) return;
            RunBuilds(new[] { ImataDefinition });
        }

        [MenuItem("Tools/Scene DSL/Build Imata Scene %&g", true)]
        private static bool ValidateBuild() =>
            !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>
        ///     ダイアログを出さずに Imata シーンを組み立てる（EditorCommandBridge から呼ぶ）。
        ///     開いているシーンに保存していない変更があれば、作業を消さないよう何もしない
        /// </summary>
        public static string BuildImataForAutomation()
        {
            if (EditorApplication.isPlaying) return "[NG] 再生中は組み立てられません";

            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    return $"[NG] 保存していない変更があるシーンが開いています: {SceneManager.GetSceneAt(i).path}";

            var definition = ImataDefinition;
            var openedScenePath = SceneManager.GetActiveScene().path;
            var succeeded = TryBuildScene(definition, out var error);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(openedScenePath) && openedScenePath != definition.ScenePath && File.Exists(openedScenePath))
                EditorSceneManager.OpenScene(openedScenePath, OpenSceneMode.Single);

            var summary = succeeded ? $"[OK] {ImataName} → {definition.ScenePath}" : $"[NG] {ImataName}: {error}";
            WriteReportFile(summary);
            return summary;
        }

        private static bool CanRun()
        {
            if (EditorApplication.isPlaying)
            {
                Fail("再生中は実行できません", "Play を止めてから実行してください。");
                return false;
            }

            // 開きっぱなしの編集内容を失わないよう、先に保存を促す
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        private static void RunBuilds(IReadOnlyList<SceneBuildDefinition> definitions)
        {
            var openedScenePath = SceneManager.GetActiveScene().path;
            var report = new StringBuilder();
            var failed = 0;

            try
            {
                for (var i = 0; i < definitions.Count; i++)
                {
                    var definition = definitions[i];
                    EditorUtility.DisplayProgressBar(
                        "シーンを組み立て中", definition.DisplayName, (float)i / definitions.Count);

                    // まとめて組み立てるときは、DSL をまだ用意していないシーンを飛ばす
                    if (definitions.Count > 1 && !File.Exists(definition.DslPath))
                    {
                        report.AppendLine($"[SKIP] {definition.DisplayName}: DSL がありません ({definition.DslPath})");
                        continue;
                    }

                    if (TryBuildScene(definition, out var error))
                    {
                        report.AppendLine($"[OK]   {definition.DisplayName} → {definition.ScenePath}");
                    }
                    else
                    {
                        failed++;
                        report.AppendLine($"[NG]   {definition.DisplayName}: {error}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 元々開いていたシーンに戻す
            if (!string.IsNullOrEmpty(openedScenePath) && File.Exists(openedScenePath))
                EditorSceneManager.OpenScene(openedScenePath, OpenSceneMode.Single);

            var summary = report.ToString().TrimEnd();
            WriteReportFile(summary);

            if (failed > 0)
            {
                Fail("シーンの組み立てに失敗しました", summary);
                return;
            }

            Debug.Log($"[SceneBuild] 組み立て完了（詳細は {ReportPath}）\n{summary}");
        }

        /// <summary>
        ///     組み立て結果の全文を置く場所。
        ///     Console は長いログを途中で切ってしまうので、こちらを見れば全部読める
        /// </summary>
        private const string ReportPath = "Logs/SceneBuildReport.txt";

        private static void WriteReportFile(string summary)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, summary, new UTF8Encoding(false));
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[SceneBuild] 結果を書き出せませんでした: {e.Message}");
            }
        }

        private static bool TryBuildScene(SceneBuildDefinition definition, out string error)
        {
            if (!SceneDslPaths.IsTargetScene(definition.ScenePath))
            {
                error = $"DSL で組み立ててよいのは {SceneDslPaths.TargetScenePath} だけです: {definition.ScenePath}";
                return false;
            }

            if (!File.Exists(definition.DslPath))
            {
                error = $"DSL ファイルがありません: {definition.DslPath}";
                return false;
            }

            var dslText = File.ReadAllText(definition.DslPath, Encoding.UTF8);

            var openExisting = !definition.RebuildFromEmpty && File.Exists(definition.ScenePath);
            var scene = openExisting
                ? EditorSceneManager.OpenScene(definition.ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MergeWorkScenes(definition, scene);

            // 土台になるシーンプレハブ。中身は消さずに、同名のオブジェクトへ値を書き込む相手として使う
            var scenePrefabRoot = ResolveScenePrefabRoot(definition, scene);

            // 先に重複を畳んでから掃除する。順番が逆だと、作業シーン側の中身が
            // プレハブへ引っ越す前に消えてしまう
            AbsorbDuplicatesIntoScenePrefab(scene, scenePrefabRoot, definition);
            RemoveObjectsDeclaredInDsl(scene, dslText, scenePrefabRoot);

            var result = SceneDslBuilder.Build(dslText, scenePrefabRoot);
            if (result.Log.ErrorCount > 0)
            {
                error = FormatErrors(result.Log);
                return false;
            }

            foreach (var entry in result.Log.Entries)
                if (entry.Level == LogLevel.Warning)
                    Debug.LogWarning($"[SceneBuild] {definition.DisplayName}: {entry.Message}");

            GroupRootsUnder(scene, definition, scenePrefabRoot);

            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, definition.ScenePath))
            {
                error = $"シーンの保存に失敗しました: {definition.ScenePath}";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        ///     土台にするシーンプレハブを置く。
        ///
        ///     PrefabUtility.InstantiatePrefab で置くのでアセットとの繋がりが残り、
        ///     ここから先の書き込みはすべてシーン側のオーバーライドになる。
        ///     プレハブのアセットには一切書き戻さない（Apply はしない）
        /// </summary>
        /// <returns>プレハブのインスタンス。プレハブが無いシーンなら null（従来どおり全部を生成する）</returns>
        private static GameObject ResolveScenePrefabRoot(
            SceneBuildDefinition definition, UnityEngine.SceneManagement.Scene scene)
        {
            var path = string.IsNullOrEmpty(definition.ScenePrefabPath)
                ? $"{SceneDslPaths.ScenePrefabDirectory}/{definition.DisplayName}.prefab"
                : definition.ScenePrefabPath;

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                // 明示的に指定されていたのに無い場合だけ知らせる（規約で探しただけなら黙って従来どおり）
                if (!string.IsNullOrEmpty(definition.ScenePrefabPath))
                    Debug.LogWarning($"[SceneBuild] {definition.DisplayName}: シーンプレハブが見つかりません: {path}");

                return null;
            }

            // 開き直したシーンに既に置いてあるなら、二重に置かずそれを使う
            var existing = FindInstanceOf(scene, asset);
            if (existing != null) return existing;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            if (instance == null)
            {
                Debug.LogWarning($"[SceneBuild] {definition.DisplayName}: シーンプレハブを置けませんでした: {path}");
                return null;
            }

            instance.name = asset.name;
            return instance;
        }

        /// <summary>シーンに既に置かれている、そのプレハブのインスタンスを探す</summary>
        private static GameObject FindInstanceOf(UnityEngine.SceneManagement.Scene scene, GameObject asset)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
                if (PrefabUtility.GetCorrespondingObjectFromSource(go) != asset) continue;

                return go;
            }

            return null;
        }

        /// <summary>
        ///     作業シーンの中身を組み立て先へ移す。
        ///     Instantiate ではなく MergeScenes を使うことで、プレハブとの繋がりを保ったまま移動できる
        /// </summary>
        private static void MergeWorkScenes(
            SceneBuildDefinition definition, UnityEngine.SceneManagement.Scene destination)
        {
            if (definition.MergeScenePaths == null) return;

            foreach (var path in definition.MergeScenePaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[SceneBuild] 取り込むシーンが見つかりません: {path}");
                    continue;
                }

                var source = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                // MergeScenes は source のオブジェクトを destination へ移したうえで source を閉じる。
                // 作業シーンのファイル自体は書き換えないので、元のまま残る
                EditorSceneManager.MergeScenes(source, destination);
            }
        }

        /// <summary>
        ///     作業シーンから取り込んだもののうち、シーンプレハブが既に同じ名前で持っているものを畳む。
        ///
        ///     中身（子）はプレハブ側の同名オブジェクトへ引っ越してから抜け殻を捨てるので、
        ///     Stage の下のステージ内容などは残ったまま、オブジェクトの重複だけが消える。
        ///     コンポーネントの値はプレハブ側が残る（DSL の上書きはそのあとに乗る）
        /// </summary>
        private static void AbsorbDuplicatesIntoScenePrefab(
            UnityEngine.SceneManagement.Scene scene, GameObject scenePrefabRoot,
            SceneBuildDefinition definition)
        {
            if (scenePrefabRoot == null) return;

            var absorbed = new List<string>();

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == scenePrefabRoot) continue;

                var counterpart = FindInHierarchyByName(scenePrefabRoot.transform, root.name);
                if (counterpart == null) continue;

                MoveChildrenInto(root.transform, counterpart);
                absorbed.Add(root.name);
                Object.DestroyImmediate(root);
            }

            if (absorbed.Count == 0) return;

            Debug.Log(
                $"[SceneBuild] {definition.DisplayName}: シーンプレハブが同じ名前で持っているので、" +
                $"取り込んだ側は中身をプレハブ側へ移してから畳みました → {string.Join(", ", absorbed)}");
        }

        /// <summary>子を引っ越す。引っ越し先に同名の子がいれば、そこへさらに潜って同じことをする</summary>
        private static void MoveChildrenInto(Transform from, Transform to)
        {
            // SetParent で from の子が減るので、後ろから回す
            for (var i = from.childCount - 1; i >= 0; i--)
            {
                var child = from.GetChild(i);
                var existing = FindDirectChildByName(to, child.name);

                if (existing != null)
                {
                    MoveChildrenInto(child, existing);
                    Object.DestroyImmediate(child.gameObject);
                    continue;
                }

                // ワールド座標を保ったまま、プレハブインスタンスの子として足す（追加のオーバーライドになる）
                child.SetParent(to, true);
            }
        }

        private static Transform FindInHierarchyByName(Transform current, string name)
        {
            if (current.name == name) return current;

            for (var i = 0; i < current.childCount; i++)
            {
                var found = FindInHierarchyByName(current.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        private static Transform FindDirectChildByName(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name)
                    return parent.GetChild(i);

            return null;
        }

        /// <summary>
        ///     取り込んだオブジェクトも含めて、残りのルートをすべてシーン名のルートの下にまとめる
        /// </summary>
        private static void GroupRootsUnder(
            UnityEngine.SceneManagement.Scene scene, SceneBuildDefinition definition,
            GameObject scenePrefabRoot)
        {
            if (scenePrefabRoot == null && string.IsNullOrEmpty(definition.RootObjectName)) return;

            // シーンプレハブがあればそれが親。ぶら下げた分は「追加されたオブジェクト」のオーバーライドになる
            var rootObject = scenePrefabRoot;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (rootObject != null) break;
                if (go.name != definition.RootObjectName) continue;
                rootObject = go;
                break;
            }

            if (rootObject == null)
            {
                Debug.LogWarning(
                    $"[SceneBuild] {definition.DisplayName}: ルート '{definition.RootObjectName}' が " +
                    "DSL で作られていないため、階層をまとめられませんでした");
                return;
            }

            foreach (var go in scene.GetRootGameObjects())
            {
                if (go == rootObject) continue;

                // ワールド座標を保ったまま付け替える
                go.transform.SetParent(rootObject.transform, true);
            }
        }

        /// <summary>
        ///     DSL に書かれた名前のオブジェクトを、階層のどこにあっても削除しておく。
        ///     これで何度実行しても重複せず、常に DSL の内容へ置き換わる
        /// </summary>
        private static void RemoveObjectsDeclaredInDsl(
            UnityEngine.SceneManagement.Scene scene, string dslText, GameObject scenePrefabRoot)
        {
            var parseLog = new BuildLog();
            var objectDefs = SceneDslParser.Parse(dslText, parseLog);

            var declaredNames = new HashSet<string>();
            foreach (var def in objectDefs)
            {
                // Type: Existing は「既存の子を探して値を上書きする」指定なので消してはいけない
                if (def.Type == "Existing") continue;
                declaredNames.Add(def.Name);
            }

            if (declaredNames.Count == 0) return;

            var targets = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
                CollectByName(root.transform, declaredNames, targets, scenePrefabRoot);

            foreach (var target in targets)
            {
                // 親を先に消すと子は既に消えているので、都度確認する
                if (target == null) continue;
                Object.DestroyImmediate(target);
            }
        }

        /// <summary>
        ///     名前が一致するオブジェクトを、非アクティブなものも含めて集める。
        ///     シーンプレハブの中身は消さない（Unity がプレハブの組み替えを禁じているうえ、
        ///     同名は「値を上書きする相手」として使うため）
        /// </summary>
        private static void CollectByName(
            Transform current, HashSet<string> names, List<GameObject> results, GameObject scenePrefabRoot)
        {
            var protectedByPrefab = IsPartOfScenePrefab(current.gameObject, scenePrefabRoot);

            if (!protectedByPrefab && names.Contains(current.name))
            {
                // 一致した時点で子ごと消えるので、これ以上潜る必要はない
                results.Add(current.gameObject);
                return;
            }

            // プレハブの中身は残すが、前回のビルドで足した子は消したいので下まで潜る
            for (var i = 0; i < current.childCount; i++)
                CollectByName(current.GetChild(i), names, results, scenePrefabRoot);
        }

        /// <summary>シーンプレハブが元から持っている部分か（ビルドで足した子は含めない）</summary>
        private static bool IsPartOfScenePrefab(GameObject go, GameObject scenePrefabRoot)
        {
            if (scenePrefabRoot == null) return false;
            if (go == scenePrefabRoot) return true;
            if (!go.transform.IsChildOf(scenePrefabRoot.transform)) return false;

            return !PrefabUtility.IsAddedGameObjectOverride(go);
        }

        private static string FormatErrors(BuildLog log)
        {
            var sb = new StringBuilder();
            foreach (var entry in log.Entries)
                if (entry.Level == LogLevel.Error)
                    sb.AppendLine(entry.Message);

            return sb.ToString().Trim();
        }

        private static void Fail(string title, string message)
        {
            Debug.LogError($"[SceneBuild] {title}\n{message}");
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
