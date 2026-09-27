using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    ///     いま開いているシーンを DSL テキストに書き出す。
    ///     手で組んだ画面を DSL へ移すときに使う。
    ///
    ///     書き出せるのは DSL が扱える値だけ（bool / 数値 / 文字列 / enum / Color / Vector / 参照）。
    ///     扱えないフィールドは黙って飛ばすので、書き出したあとに目視で確認すること
    /// </summary>
    public static class SceneDslExporter
    {
        private static string OutputDirectory => SceneDslPaths.ExportDirectory;

        /// <summary>DSL の Type として素直に書ける組み合わせ</summary>
        private static readonly (Type type, string name)[] TypeHints =
        {
            (typeof(Canvas), "Canvas"),
            (typeof(UnityEngine.UI.Button), "Button"),
            (typeof(TMPro.TextMeshProUGUI), "TMP_Text"),
            (typeof(UnityEngine.UI.RawImage), "RawImage"),
            (typeof(UnityEngine.UI.Image), "Image")
        };

        /// <summary>書き出しても意味がない、あるいは DSL で扱えないコンポーネント</summary>
        private static readonly HashSet<string> SkippedComponents = new()
        {
            "CanvasRenderer"
        };

        /// <summary>Unity が内部で使うだけのプロパティ</summary>
        private static readonly HashSet<string> SkippedProperties = new()
        {
            "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
            "m_GameObject", "m_Name", "m_EditorClassIdentifier", "m_Script", "m_Enabled",
            "m_Children", "m_Father", "m_RootOrder", "m_LocalEulerAnglesHint", "m_ConstrainProportionsScale",
            "m_Material", "m_Materials", "m_OnClick", "m_Navigation", "m_ChildOrder"
        };

        [MenuItem("Tools/Scene DSL/Export Scene to DSL", false, 40)]
        public static void Export()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Fail("シーンが開かれていません");
                return;
            }

            var path = ExportScene(scene);
            EditorUtility.DisplayDialog("シーンを DSL に書き出しました", path, "OK");
        }

        /// <summary>
        ///     Imata シーンを DSL に書き出す（EditorCommandBridge から呼ぶ）。
        ///     いま別のシーンを開いていても、そちらは閉じずに Imata を追加で開いて書き出す
        /// </summary>
        public static string ExportImataForAutomation()
        {
            var scenePath = SceneDslPaths.TargetScenePath;
            var loaded = SceneManager.GetSceneByPath(scenePath);
            if (loaded.IsValid() && loaded.isLoaded) return ExportScene(loaded);

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                return ExportScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>シーン1つを DSL に書き出し、出力先のパスを返す</summary>
        private static string ExportScene(UnityEngine.SceneManagement.Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var names = CollectNames(roots);

            var sb = new StringBuilder();
            AppendHeader(sb, scene.name);

            foreach (var root in roots) AppendObject(sb, root, null, names);

            Directory.CreateDirectory(OutputDirectory);
            var path = Path.Combine(OutputDirectory, ToFileName(scene.name));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();

            Debug.Log($"[SceneDsl] {scene.name} を書き出しました: {path}");
            return path;
        }

        /// <summary>
        ///     元シーンと、組み立てに使う DSL ファイルの対応。
        ///     作業用シーンから書き出したものを、そのまま本番シーンの元にしたいときに登録する。
        ///     例: { "Title_Work", "title_scene.txt" }
        ///     登録が無ければ「シーン名の小文字_scene.txt」になる（Title → title_scene.txt）
        /// </summary>
        private static readonly Dictionary<string, string> OutputNameOverrides = new();

        private static string ToFileName(string sceneName)
        {
            if (OutputNameOverrides.TryGetValue(sceneName, out var mapped)) return mapped;

            return sceneName.ToLowerInvariant().Replace(" ", "_") + "_scene.txt";
        }

        private static void AppendHeader(StringBuilder sb, string sceneName)
        {
            sb.AppendLine("// ===============================================================");
            sb.AppendLine($"// {sceneName} シーン構築 DSL（Tools > Scene DSL > Export Scene to DSL で書き出し）");
            sb.AppendLine("//");
            sb.AppendLine("// ※ 自動生成です。DSL で扱えない値は書き出されていないので、");
            sb.AppendLine("//    組み直したあとに元のシーンと見比べて確認してください。");
            sb.AppendLine("// ===============================================================");
            sb.AppendLine();
        }

        /// <summary>
        ///     DSL 内でオブジェクトを指すための名前を決める。
        ///
        ///     DSL は名前でオブジェクトを引くので、同じ名前が複数あると区別できない。
        ///     2つ目以降には "#2" のような通し番号を付けて別物として扱う。
        ///     この番号は組み立て時に外され、シーン上の名前は元のまま
        /// </summary>
        static Dictionary<GameObject, string> CollectNames(GameObject[] roots)
        {
            var result = new Dictionary<GameObject, string>();
            var counts = new Dictionary<string, int>();

            foreach (var root in roots)
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;

                if (counts.TryGetValue(go.name, out var seen))
                {
                    seen++;
                    counts[go.name] = seen;
                    result[go] = $"{go.name}#{seen}";
                    Debug.Log($"[SceneDsl] 名前が重複しているので {result[go]} として書き出します", go);
                }
                else
                {
                    counts[go.name] = 1;
                    result[go] = go.name;
                }
            }

            return result;
        }

        /// <summary>DSL 内での名前。集めそこねていたら実際の名前で代用する</summary>
        static string KeyOf(GameObject go, Dictionary<GameObject, string> names) =>
            names.TryGetValue(go, out var key) ? key : go.name;

        static void AppendObject(StringBuilder sb, GameObject go, GameObject parent,
            Dictionary<GameObject, string> names)
        {
            sb.AppendLine("Object");
            sb.AppendLine("{");
            sb.AppendLine($"    Name: {KeyOf(go, names)}");
            if (parent != null) sb.AppendLine($"    Parent: {KeyOf(parent, names)}");
            sb.AppendLine($"    Type: {ResolveType(go)}");
            if (!go.activeSelf) sb.AppendLine("    active: false");

            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null) continue;

                var typeName = component.GetType().Name;
                if (SkippedComponents.Contains(typeName)) continue;

                AppendComponent(sb, component, names);
            }

            sb.AppendLine("}");
            sb.AppendLine();

            foreach (Transform child in go.transform) AppendObject(sb, child.gameObject, go, names);
        }

        /// <summary>DSL の Type。プレハブから生えているならプレハブ名にする</summary>
        static string ResolveType(GameObject go)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (source != null) return source.name;

            foreach (var (type, name) in TypeHints)
                if (go.GetComponent(type) != null)
                    return name;

            // UI の入れ物（RectTransform だけ持つオブジェクト）も UI として作らせる
            if (go.GetComponent<RectTransform>() != null) return "Panel";

            return "Empty";
        }

        static void AppendComponent(StringBuilder sb, Component component, Dictionary<GameObject, string> names)
        {
            var type = component.GetType();
            var lines = new List<string>();

            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();

            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (SkippedProperties.Contains(property.name)) continue;

                var memberName = ToMemberName(property.name);
                var member = ReflectionHelper.FindMember(type, memberName);
                if (member == null) continue;

                var memberType = ReflectionHelper.GetMemberType(member);
                if (!TryFormat(property, memberType, names, out var value)) continue;

                lines.Add($"        {memberName}: {value}");
            }

            AppendExtras(component, lines);

            // 何も書くことがない Transform は、行を増やすだけなので省く
            if (lines.Count == 0 && component is Transform) return;

            // 名前空間まで書いておけば、他アセンブリの同名型と衝突しない
            sb.AppendLine($"    Component: {type.FullName}");
            sb.AppendLine("    {");
            foreach (var line in lines) sb.AppendLine(line);
            sb.AppendLine("    }");
        }

        /// <summary>
        ///     内部名から機械的に導けないメンバー名。
        ///     TextMeshPro の m_fontAsset は C# 側では font という名前になっている
        /// </summary>
        static readonly Dictionary<string, string> MemberNameAliases = new()
        {
            { "m_fontAsset", "font" }
        };

        /// <summary>
        ///     SerializedProperty をそのまま辿るだけでは落ちてしまう値を補う。
        ///
        ///       回転   … Quaternion のままでは DSL に書けないのでオイラー角にする
        ///       マテリアル … m_Materials は配列なので、先頭の1枚だけ sharedMaterial として書く
        /// </summary>
        static void AppendExtras(Component component, List<string> lines)
        {
            if (component is Transform transform)
            {
                var euler = transform.localEulerAngles;
                if (euler != Vector3.zero)
                    lines.Add($"        localEulerAngles: {Join(euler.x, euler.y, euler.z)}");
                return;
            }

            if (component is Renderer renderer)
            {
                var material = renderer.sharedMaterial;
                if (material == null) return;

                // 組み込みマテリアルはアセットとして引けないので書かない
                var path = AssetDatabase.GetAssetPath(material);
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)) return;

                lines.Add($"        sharedMaterial: <Material>{material.name}");
            }
        }

        /// <summary>"m_Color" のような Unity 内部名を、C# のメンバー名へ寄せる</summary>
        static string ToMemberName(string propertyName)
        {
            if (MemberNameAliases.TryGetValue(propertyName, out var alias)) return alias;

            if (!propertyName.StartsWith("m_", StringComparison.Ordinal)) return propertyName;

            var body = propertyName.Substring(2);
            if (body.Length == 0) return propertyName;

            return char.ToLowerInvariant(body[0]) + body.Substring(1);
        }

        static bool TryFormat(SerializedProperty property, Type memberType,
            Dictionary<GameObject, string> names, out string value)
        {
            value = null;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    value = property.boolValue ? "true" : "false";
                    return true;

                case SerializedPropertyType.Integer:
                    value = property.intValue.ToString(CultureInfo.InvariantCulture);
                    return true;

                case SerializedPropertyType.Float:
                    value = property.floatValue.ToString("0.####", CultureInfo.InvariantCulture);
                    return true;

                case SerializedPropertyType.String:
                    if (string.IsNullOrEmpty(property.stringValue)) return false;
                    value = property.stringValue.Replace("\n", "\\n");
                    return true;

                case SerializedPropertyType.Enum:
                    if (!memberType.IsEnum) return false;
                    var enumNames = Enum.GetNames(memberType);
                    if (property.enumValueIndex < 0 || property.enumValueIndex >= enumNames.Length) return false;
                    value = enumNames[property.enumValueIndex];
                    return true;

                case SerializedPropertyType.Color:
                    value = "#" + ColorUtility.ToHtmlStringRGBA(property.colorValue);
                    return true;

                case SerializedPropertyType.Vector2:
                    value = Join(property.vector2Value.x, property.vector2Value.y);
                    return true;

                case SerializedPropertyType.Vector3:
                    value = Join(property.vector3Value.x, property.vector3Value.y, property.vector3Value.z);
                    return true;

                case SerializedPropertyType.ObjectReference:
                    return TryFormatReference(property.objectReferenceValue, names, out value);

                default:
                    return false;
            }
        }

        static bool TryFormatReference(Object target, Dictionary<GameObject, string> names, out string value)
        {
            value = null;
            if (target == null) return false;

            // シーン内のオブジェクトなら @名前 で参照する
            var go = target as GameObject ?? (target as Component)?.gameObject;
            if (go != null && names.TryGetValue(go, out var sceneName))
            {
                value = "@" + sceneName;
                return true;
            }

            // アセットなら <型>名前 で参照する
            var path = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(path)) return false;

            value = $"<{target.GetType().Name}>{target.name}";
            return true;
        }

        static string Join(params float[] values)
        {
            var parts = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
                parts[i] = values[i].ToString("0.####", CultureInfo.InvariantCulture);

            return string.Join(",", parts);
        }

        static void Fail(string message)
        {
            Debug.LogError($"[SceneDsl] {message}");
            EditorUtility.DisplayDialog("書き出せません", message, "OK");
        }
    }
}
