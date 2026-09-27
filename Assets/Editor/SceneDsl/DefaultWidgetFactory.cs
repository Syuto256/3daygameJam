using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    /// Slider / Toggle / Dropdown など、Unity エディタの
    /// "GameObject > UI > ..." メニューと同じ内部ロジックで
    /// 標準の子階層（Background / Fill Area / Handle / Template など）を持つ
    /// GameObject を生成するためのヘルパー。
    ///
    /// TMP 系（TMP_Dropdown / TMP_InputField）は Unity / TextMeshPro のバージョンによって
    /// 内部クラスの API が変わることがあるため、リフレクション経由で呼び出し、
    /// 失敗した場合は最低限のコンポーネントだけを持つ GameObject にフォールバックする。
    /// </summary>
    public static class DefaultWidgetFactory
    {
        public static GameObject CreateSlider(string name, BuildLog log)
        {
            try
            {
                var go = DefaultControls.CreateSlider(GetUguiResources());
                go.name = name;
                return go;
            }
            catch (Exception e)
            {
                log?.Warning($"Slider の標準階層生成に失敗したため簡易生成にフォールバックします ({name}): {e.Message}");
                var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
                return go;
            }
        }

        public static GameObject CreateToggle(string name, BuildLog log)
        {
            try
            {
                var go = DefaultControls.CreateToggle(GetUguiResources());
                go.name = name;
                return go;
            }
            catch (Exception e)
            {
                log?.Warning($"Toggle の標準階層生成に失敗したため簡易生成にフォールバックします ({name}): {e.Message}");
                var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
                return go;
            }
        }

        public static GameObject CreateLegacyInputField(string name, BuildLog log)
        {
            try
            {
                var go = DefaultControls.CreateInputField(GetUguiResources());
                go.name = name;
                return go;
            }
            catch (Exception e)
            {
                log?.Warning($"InputField の標準階層生成に失敗したため簡易生成にフォールバックします ({name}): {e.Message}");
                var go = new GameObject(name, typeof(RectTransform), typeof(InputField));
                return go;
            }
        }

        public static GameObject CreateScrollRect(string name, BuildLog log)
        {
            try
            {
                var go = DefaultControls.CreateScrollView(GetUguiResources());
                go.name = name;
                return go;
            }
            catch (Exception e)
            {
                log?.Warning($"ScrollRect の標準階層生成に失敗したため簡易生成にフォールバックします ({name}): {e.Message}");
                var go = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
                return go;
            }
        }

        public static GameObject CreateTmpDropdown(string name, BuildLog log)
        {
            var go = TryCreateViaReflection("TMP_DefaultControls", "CreateDropdown", name, log);
            if (go != null) return go;

            log?.Warning($"TMP_Dropdown の標準階層生成に失敗したため簡易生成にフォールバックします ({name})。Existing での子オブジェクト参照は動作しない可能性があります。");
            var fallbackType = TypeResolver.FindType("TMP_Dropdown");
            var fallback = new GameObject(name, typeof(RectTransform));
            if (fallbackType != null) fallback.AddComponent(fallbackType);
            return fallback;
        }

        public static GameObject CreateTmpInputField(string name, BuildLog log)
        {
            var go = TryCreateViaReflection("TMP_DefaultControls", "CreateInputField", name, log);
            if (go != null) return go;

            log?.Warning($"TMP_InputField の標準階層生成に失敗したため簡易生成にフォールバックします ({name})。");
            var fallbackType = TypeResolver.FindType("TMP_InputField");
            var fallback = new GameObject(name, typeof(RectTransform));
            if (fallbackType != null) fallback.AddComponent(fallbackType);
            return fallback;
        }

        /// <summary>
        /// TMPro.EditorUtilities.TMP_DefaultControls.Create&lt;methodSuffix&gt;(Resources) を
        /// リフレクションで呼び出す。型・メソッドのシグネチャが多少変わっても壊れにくいようにするため。
        /// </summary>
        private static GameObject TryCreateViaReflection(string typeShortName, string methodName, string name, BuildLog log)
        {
            try
            {
                Type controlsType = TypeResolver.FindType(typeShortName);
                if (controlsType == null) return null;

                Type resourcesType = controlsType.GetNestedType("Resources", BindingFlags.Public | BindingFlags.NonPublic);
                if (resourcesType == null) return null;

                object resources = Activator.CreateInstance(resourcesType);

                MethodInfo method = controlsType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method == null) return null;

                object result = method.Invoke(null, new object[] { resources });
                var go = result as GameObject;
                if (go != null) go.name = name;
                return go;
            }
            catch (Exception e)
            {
                log?.Info($"TMP_DefaultControls.{methodName} のリフレクション呼び出しに失敗しました: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// プロジェクト内から名前が一致するプレハブを検索し、プレハブ本体との接続を保ったまま
        /// インスタンス化する。見つからなければ null を返す。
        /// </summary>
        public static GameObject TryInstantiatePrefab(string prefabName, string instanceName, BuildLog log)
        {
            string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
            string exactPath = null, firstPath = null;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (firstPath == null) firstPath = path;
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(fileName, prefabName, StringComparison.OrdinalIgnoreCase))
                {
                    exactPath = path;
                    break;
                }
            }

            string chosenPath = exactPath ?? firstPath;
            if (chosenPath == null) return null;

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(chosenPath);
            if (prefabAsset == null) return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            instance.name = instanceName;
            return instance;
        }

        private static DefaultControls.Resources GetUguiResources()
        {
            var r = new DefaultControls.Resources();
            r.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            r.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            r.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            r.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            r.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            r.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
            r.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
            return r;
        }
    }
}
