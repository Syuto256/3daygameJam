using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    /// ObjectDef のリストから実際にシーンオブジェクトを構築する。
    ///
    /// ビルドは次の5段階で行う。これにより DSL 内での前方参照・自己参照・
    /// 兄弟コンポーネント参照がすべて解決できる。
    ///   1. 生成   : Type ごとに GameObject（または標準UI階層 / プレハブ）を作る
    ///   2. 親子付け: Parent 指定に従って階層を組む
    ///   3. Existing解決: Type: Existing を実際の子オブジェクトに解決する
    ///   4. コンポーネント確保: Component ブロックごとに Add/GetComponent する
    ///   5. 値の設定: すべてのコンポーネントが揃った状態でフィールドへ値を設定する（@参照もここで解決）
    /// </summary>
    public static class SceneDslBuilder
    {
        private static readonly HashSet<string> AutoHierarchyTypes = new HashSet<string>
        {
            "Slider", "Toggle", "TMP_Dropdown", "Dropdown", "TMP_InputField", "InputField", "ScrollRect"
        };

        private static readonly HashSet<string> SimpleUiTypes = new HashSet<string>
        {
            "Empty", "Canvas", "Image", "RawImage", "TMP_Text", "Text", "Button", "Panel", "Mask", "RectMask2D"
        };

        public class BuildResult
        {
            public Dictionary<string, GameObject> Registry = new Dictionary<string, GameObject>();
            public BuildLog Log = new BuildLog();
        }

        /// <summary>
        /// 実際にシーンへオブジェクトを生成する。Undo に対応している。
        /// </summary>
        public static BuildResult Build(string dslText) => Build(dslText, null);

        /// <summary>
        ///     シーンプレハブのインスタンスを土台にして組み立てる。
        ///
        ///     baseRoot の中に DSL と同名のオブジェクトがあれば、作り直さずにそれを使い、
        ///     値はプレハブインスタンスのオーバーライドとして書き込む。
        ///     プレハブのアセット自体には一切触れないので、いつでも Revert / Apply できる。
        ///     baseRoot に無い名前だけが新しく作られ、あとで baseRoot の下にぶら下がる
        /// </summary>
        /// <param name="baseRoot">シーンプレハブのインスタンス。null なら従来どおり全部を新規生成する</param>
        public static BuildResult Build(string dslText, GameObject baseRoot)
        {
            var result = new BuildResult();
            var log = result.Log;

            // 他のシーンはチームメンバーが手で編集しているので、Imata 以外には組み立てない
            var activeScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
            if (!SceneDslPaths.IsTargetScene(activeScenePath))
            {
                log.Error(
                    $"DSL で組み立ててよいのは {SceneDslPaths.TargetScenePath} だけです。" +
                    $"いま開いているシーン: {(string.IsNullOrEmpty(activeScenePath) ? "（未保存のシーン）" : activeScenePath)}");
                return result;
            }

            var objectDefs = SceneDslParser.Parse(dslText, log);
            if (objectDefs.Count == 0)
            {
                log.Warning("Object ブロックが1つも見つかりませんでした。");
                return result;
            }

            CheckDuplicateNames(objectDefs, log);

            Undo.SetCurrentGroupName("Build Scene From DSL");
            int undoGroup = Undo.GetCurrentGroup();

            var registry = result.Registry;

            // シーンプレハブから借りてきたオブジェクト。階層は動かさず、値だけ上書きする
            var reused = new HashSet<GameObject>();

            // --- 1. 生成 ---
            foreach (var def in objectDefs)
            {
                if (def.Type == "Existing") continue;
                if (registry.ContainsKey(def.Name))
                {
                    // 名前でオブジェクトを引く仕組みなので、同名があると後の定義を作れない。
                    // 書き出し側が "#2" を付けて区別するので、DSL を書き出し直せば直る
                    log.Error(
                        $"同名のオブジェクトが複数あるため '{def.Name}' を作成できません。" +
                        "Ctrl + Alt + T で DSL を書き出し直してください（2つ目以降に #2 が付きます）。");
                    continue;
                }

                // シーンプレハブの中に同名があれば作り直さない。
                // ここで拾ったものへの変更は、すべてプレハブインスタンスのオーバーライドになる
                var borrowed = FindInBaseRoot(baseRoot, ToObjectName(def.Name), reused);
                if (borrowed != null)
                {
                    reused.Add(borrowed);
                    registry[def.Name] = borrowed;
                    continue;
                }

                GameObject go;
                try
                {
                    go = CreateBaseObject(def, log);
                }
                catch (Exception e)
                {
                    log.Error($"オブジェクトの生成に失敗しました ({def.Name}): {e.Message}");
                    continue;
                }

                if (go == null)
                {
                    log.Error($"オブジェクトを生成できませんでした: {def.Name}");
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(go, "Create " + def.Name);
                registry[def.Name] = go;
            }

            // --- 2. 親子付け ---
            foreach (var def in objectDefs)
            {
                if (def.Type == "Existing") continue;
                if (string.IsNullOrEmpty(def.Parent)) continue;
                if (!registry.TryGetValue(def.Name, out var go)) continue;

                // プレハブの中身は組み替えられない（Unity が禁じている）ので、そのままの場所で使う
                if (reused.Contains(go))
                {
                    if (registry.TryGetValue(def.Parent, out var declared) && declared != null &&
                        go.transform.parent != declared.transform)
                        log.Warning(
                            $"'{def.Name}' はシーンプレハブの中にあるので、Parent '{def.Parent}' への" +
                            "付け替えはしません（プレハブ側の階層を直してください）");

                    continue;
                }

                if (!registry.TryGetValue(def.Parent, out var parentGo))
                {
                    // DSL で作っていない相手なら、既にシーンに居るオブジェクトを名前で探す
                    // （作業シーンから取り込んだ Player などの下に置きたいとき）
                    parentGo = SceneObjectLookup.Find(def.Parent);
                    if (parentGo == null)
                    {
                        log.Error($"Parent '{def.Parent}' が見つかりません（Object: {def.Name}）。ルートのまま残します。");
                        continue;
                    }
                }
                go.transform.SetParent(parentGo.transform, false);
            }

            // --- 3. Existing 解決 ---
            foreach (var def in objectDefs)
            {
                if (def.Type != "Existing") continue;

                // Parent 省略時は、シーンに既にあるオブジェクトを名前で探して設定を上書きする
                // （作業シーンから取り込んだ Main Camera などをそのまま設定したいとき）
                if (string.IsNullOrEmpty(def.Parent))
                {
                    var existing = SceneObjectLookup.Find(def.Name);
                    if (existing == null)
                    {
                        log.Error($"Existing '{def.Name}' がシーンに見つかりません。");
                        continue;
                    }

                    registry[def.Name] = existing;
                    continue;
                }

                if (!registry.TryGetValue(def.Parent, out var parentGo))
                {
                    log.Error($"Existing '{def.Name}' の Parent '{def.Parent}' が見つかりません。");
                    continue;
                }

                string relativePath = ToObjectName(def.Name);
                string prefix = ToObjectName(def.Parent) + "/";
                if (relativePath.StartsWith(prefix)) relativePath = relativePath.Substring(prefix.Length);

                var found = parentGo.transform.Find(relativePath);
                if (found == null)
                {
                    log.Error($"既存の子オブジェクトが見つかりません: {def.Parent}/{relativePath}（標準UI階層の生成に失敗している可能性があります）");
                    continue;
                }

                registry[def.Name] = found.gameObject;
            }

            // --- 4 & 5. コンポーネント確保 + 値設定 ---
            var pending = new List<(Component comp, string key, string rawValue, string ownerName)>();

            // プレハブインスタンスに書き込んだものは、最後にオーバーライドとして記録する
            var touched = new List<UnityEngine.Object>();

            foreach (var def in objectDefs)
            {
                if (!registry.TryGetValue(def.Name, out var go) || go == null) continue;

                foreach (var compDef in def.Components)
                {
                    // "active" は GameObject の有効/無効を切り替える擬似フィールドとして扱う
                    var activeField = compDef.Fields.FirstOrDefault(f => string.Equals(f.Key, "active", StringComparison.OrdinalIgnoreCase));
                    if (activeField.Key != null)
                    {
                        try
                        {
                            bool b = ValueConverter.ParseBool(activeField.Value);
                            Undo.RecordObject(go, "Set Active");
                            go.SetActive(b);
                            touched.Add(go);
                        }
                        catch (Exception e)
                        {
                            log.Error($"active の値が不正です ({def.Name}): {e.Message}");
                        }
                    }

                    // "layer" は GameObject のレイヤーを子ごと切り替える擬似フィールドとして扱う（例: layer: UI）
                    var layerField = compDef.Fields.FirstOrDefault(f => string.Equals(f.Key, "layer", StringComparison.OrdinalIgnoreCase));
                    if (layerField.Key != null)
                    {
                        var layer = LayerMask.NameToLayer(layerField.Value.Trim());
                        if (layer < 0 && !int.TryParse(layerField.Value.Trim(), out layer))
                        {
                            log.Error($"レイヤー '{layerField.Value}' がありません ({def.Name})");
                        }
                        else
                        {
                            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                            {
                                Undo.RecordObject(t.gameObject, "Set Layer");
                                t.gameObject.layer = layer;
                                touched.Add(t.gameObject);
                            }
                        }
                    }

                    Type compType = TypeResolver.FindComponentType(compDef.TypeName);
                    if (compType == null)
                    {
                        log.Error($"コンポーネント型が見つかりません: {compDef.TypeName}（Object: {def.Name}）");
                        continue;
                    }

                    // Component 派生に限定しても見つからなかった場合（名前空間つきで
                    // Component 以外を指定した場合など）はここで弾く
                    if (!typeof(Component).IsAssignableFrom(compType))
                    {
                        log.Error(
                            $"'{compDef.TypeName}' は Component ではありません（Object: {def.Name}）。" +
                            $"解決結果: {compType.FullName}（{compType.Assembly.GetName().Name}）。" +
                            "名前空間まで書いて指定してください（例: UnityEngine.UI.Image）");
                        continue;
                    }

                    Component comp;
                    try
                    {
                        comp = GetOrAddComponent(go, compType);
                    }
                    catch (Exception e)
                    {
                        log.Error($"コンポーネントの追加に失敗しました: {compDef.TypeName}（Object: {def.Name}）: {e.Message}");
                        continue;
                    }

                    foreach (var (key, rawValue) in compDef.Fields)
                    {
                        if (string.Equals(key, "active", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(key, "layer", StringComparison.OrdinalIgnoreCase)) continue;
                        pending.Add((comp, key, rawValue, def.Name));
                    }
                }
            }

            foreach (var (comp, key, rawValue, ownerName) in pending)
            {
                if (comp == null) continue;

                var member = ReflectionHelper.FindMember(comp.GetType(), key);
                if (member == null)
                {
                    log.Warning($"フィールド '{key}' が {comp.GetType().Name} に見つかりません（Object: {ownerName}）");
                    continue;
                }

                Type memberType = ReflectionHelper.GetMemberType(member);
                object value;
                try
                {
                    value = ValueConverter.Convert(rawValue, memberType, registry, log);
                }
                catch (Exception e)
                {
                    log.Error($"値の変換に失敗しました: {ownerName}.{key} = '{rawValue}' ({e.Message})");
                    continue;
                }

                // 参照が解決できなかったときに書き込むと、
                // プレハブが元から持っていたメッシュやスプライトまで消えてしまう
                if (value == null && rawValue.StartsWith("<", StringComparison.Ordinal))
                {
                    log.Warning($"参照を解決できないので設定を飛ばします: {ownerName}.{key} = '{rawValue}'");
                    continue;
                }

                try
                {
                    Undo.RecordObject(comp, "Set " + key);
                    ReflectionHelper.SetMemberValue(comp, member, value);
                    EditorUtility.SetDirty(comp);
                    touched.Add(comp);
                }
                catch (Exception e)
                {
                    log.Error($"値の設定に失敗しました: {ownerName}.{key} ({e.Message})");
                }
            }

            RecordPrefabOverrides(touched);

            Undo.CollapseUndoOperations(undoGroup);

            log.Info($"完了: {registry.Count} オブジェクト / エラー {log.ErrorCount} 件 / 警告 {log.WarningCount} 件");
            return result;
        }

        /// <summary>
        /// シーンを変更せずに DSL の静的な整合性のみを検証する。
        /// （重複名 / 未定義の Parent・@参照 / 未解決のコンポーネント型 / アセット未検出 をチェック）
        /// </summary>
        public static BuildLog Validate(string dslText)
        {
            var log = new BuildLog();
            var objectDefs = SceneDslParser.Parse(dslText, log);
            if (objectDefs.Count == 0)
            {
                log.Warning("Object ブロックが1つも見つかりませんでした。");
                return log;
            }

            var declaredNames = new HashSet<string>(objectDefs.Select(d => d.Name));
            CheckDuplicateNames(objectDefs, log);

            foreach (var def in objectDefs)
            {
                if (!string.IsNullOrEmpty(def.Parent) && !declaredNames.Contains(def.Parent))
                {
                    log.Error($"Parent '{def.Parent}' は定義されていません（Object: {def.Name}）");
                }

                foreach (var compDef in def.Components)
                {
                    if (TypeResolver.FindType(compDef.TypeName) == null)
                    {
                        log.Error($"コンポーネント型が見つかりません: {compDef.TypeName}（Object: {def.Name}）");
                    }

                    foreach (var (key, rawValue) in compDef.Fields)
                    {
                        string v = rawValue.Trim();
                        if (v.StartsWith("@"))
                        {
                            string refName = v.Substring(1).Trim();
                            if (!declaredNames.Contains(refName))
                            {
                                log.Error($"参照 '@{refName}' は定義されていません（{def.Name}.{key}）");
                            }
                        }
                        else if (v.StartsWith("<"))
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(v, @"^<([^>]+)>(.*)$");
                            if (m.Success)
                            {
                                string tag = m.Groups[1].Value;
                                string name = m.Groups[2].Value.Trim();
                                AssetResolver.Resolve(tag, name, null, log);
                            }
                            else
                            {
                                log.Warning($"アセット参照の形式が不正です: '{v}'（{def.Name}.{key}）");
                            }
                        }
                    }
                }
            }

            log.Info("Existing の実体確認・実際の代入可否はビルド時にのみ検証されます（検証は静的チェックのみです）。");
            log.Info($"検証完了: エラー {log.ErrorCount} 件 / 警告 {log.WarningCount} 件");
            return log;
        }

        /// <summary>
        ///     シーンプレハブのインスタンスの中から、同じ名前のオブジェクトを探す（自分自身も対象）。
        ///     同名が複数ある場合に備え、既に使ったものは飛ばして階層の順に割り当てる
        /// </summary>
        private static GameObject FindInBaseRoot(GameObject baseRoot, string name, HashSet<GameObject> claimed)
        {
            if (baseRoot == null || string.IsNullOrEmpty(name)) return null;

            return FindInBaseRoot(baseRoot.transform, name, claimed);
        }

        private static GameObject FindInBaseRoot(Transform current, string name, HashSet<GameObject> claimed)
        {
            if (current.name == name && !claimed.Contains(current.gameObject)) return current.gameObject;

            for (var i = 0; i < current.childCount; i++)
            {
                var found = FindInBaseRoot(current.GetChild(i), name, claimed);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        ///     プレハブインスタンスへの書き込みを「オーバーライド」として確定させる。
        ///     これを呼ばないと、シーンを開き直したときにプレハブ側の値へ戻ってしまうことがある。
        ///     プレハブのアセットには触らないので、Revert すればいつでも元に戻せる
        /// </summary>
        private static void RecordPrefabOverrides(List<UnityEngine.Object> touched)
        {
            var done = new HashSet<UnityEngine.Object>();
            foreach (var target in touched)
            {
                if (target == null || !done.Add(target)) continue;
                if (!PrefabUtility.IsPartOfPrefabInstance(target)) continue;

                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        private static void CheckDuplicateNames(List<ObjectDef> objectDefs, BuildLog log)
        {
            var seen = new HashSet<string>();
            foreach (var def in objectDefs)
            {
                if (!seen.Add(def.Name))
                {
                    log.Warning($"名前が重複しています: {def.Name}");
                }
            }
        }

        /// <summary>
        ///     DSL 内の名前から、同名オブジェクトを区別するための "#2" を落とす。
        ///     シーン上の名前は元シーンと同じにしておきたいので、番号は持ち込まない
        /// </summary>
        public static string ToObjectName(string key)
        {
            int index = key.LastIndexOf('#');
            return index > 0 ? key.Substring(0, index) : key;
        }

        private static GameObject CreateBaseObject(ObjectDef def, BuildLog log)
        {
            string type = def.Type;
            string name = ToObjectName(def.Name);

            switch (type)
            {
                case "Slider": return DefaultWidgetFactory.CreateSlider(name, log);
                case "Toggle": return DefaultWidgetFactory.CreateToggle(name, log);
                case "TMP_Dropdown": return DefaultWidgetFactory.CreateTmpDropdown(name, log);
                case "Dropdown": return DefaultWidgetFactory.CreateTmpDropdown(name, log);
                case "TMP_InputField": return DefaultWidgetFactory.CreateTmpInputField(name, log);
                case "InputField": return DefaultWidgetFactory.CreateLegacyInputField(name, log);
                case "ScrollRect": return DefaultWidgetFactory.CreateScrollRect(name, log);
            }

            if (!SimpleUiTypes.Contains(type))
            {
                // 既知のキーワードでなければプレハブ名とみなす
                var prefabGo = DefaultWidgetFactory.TryInstantiatePrefab(type, name, log);
                if (prefabGo != null) return prefabGo;

                log.Warning($"未知の Type '{type}' はプレハブとしても見つかりませんでした。空オブジェクトとして作成します（Object: {def.Name}）。");
            }

            bool needsRectTransform = BodyDeclaresRectTransform(def) ||
                                       type == "Canvas" || type == "Image" || type == "RawImage" ||
                                       type == "TMP_Text" || type == "Text" || type == "Button" ||
                                       type == "Panel" || type == "Mask" || type == "RectMask2D";

            return needsRectTransform
                ? new GameObject(name, typeof(RectTransform))
                : new GameObject(name);
        }

        /// <summary>
        /// Component ブロックに RectTransform（またはその派生）が書かれているか。
        /// "RectTransform" と "UnityEngine.RectTransform" のどちらの書き方でも拾う
        /// </summary>
        private static bool BodyDeclaresRectTransform(ObjectDef def)
        {
            foreach (var c in def.Components)
            {
                var type = TypeResolver.FindComponentType(c.TypeName);
                if (type != null && typeof(RectTransform).IsAssignableFrom(type)) return true;
            }
            return false;
        }

        private static Component GetOrAddComponent(GameObject go, Type type)
        {
            if (type == typeof(Transform)) return go.transform;
            if (type == typeof(RectTransform))
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt == null) throw new Exception("この GameObject は RectTransform を持っていません（Type: Empty で RectTransform を使うには Component: RectTransform を明示してください）");
                return rt;
            }

            var existing = go.GetComponent(type);
            if (existing != null) return existing;

            return Undo.AddComponent(go, type);
        }
    }
}
