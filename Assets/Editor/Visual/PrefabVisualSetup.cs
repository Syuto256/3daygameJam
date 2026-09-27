using UnityEditor;
using UnityEngine;

namespace GameJam.Editor.Visual
{
    /// <summary>
    ///     プレハブへ見た目用のコンポーネントを付ける。
    ///     荷物はシーンに置かれず実行中に生成されるので、DSL ではなくプレハブのアセットへ直接付ける。
    ///     すでに付いていれば参照を設定し直すだけなので、何度実行してもよい
    /// </summary>
    public static class PrefabVisualSetup
    {
        private const string LuggagePrefabPath = "Assets/Prefab/Luggage.prefab";

        [MenuItem("Tools/Visual/Setup Prefabs", false, 20)]
        public static void SetupFromMenu() => Debug.Log($"[PrefabVisualSetup] {SetupAll()}");

        public static string SetupAll()
        {
            var root = PrefabUtility.LoadPrefabContents(LuggagePrefabPath);
            try
            {
                if (!root.TryGetComponent<LuggageJudgeView>(out var view))
                    view = root.AddComponent<LuggageJudgeView>();

                var serialized = new SerializedObject(view);
                serialized.FindProperty("_spriteRenderer").objectReferenceValue = root.GetComponent<SpriteRenderer>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, LuggagePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return $"[OK] {LuggagePrefabPath} に LuggageJudgeView を付けました";
        }
    }
}
