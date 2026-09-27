using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    ///     DSL の名前から、開いているシーンのオブジェクトを探す（非アクティブも対象）。
    ///
    ///     "Container#2" のように "#番号" が付いていたら、同じ名前のうち階層の上から数えて何番目かで探す。
    ///     数え方は SceneDslExporter の書き出しと同じなので、書き出した名前をそのまま使える
    /// </summary>
    public static class SceneObjectLookup
    {
        public static GameObject Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            var name = key;
            var index = 1;
            var separator = key.LastIndexOf('#');
            if (separator > 0 && int.TryParse(key.Substring(separator + 1), out var parsed))
            {
                name = key.Substring(0, separator);
                index = parsed;
            }

            var count = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != name) continue;
                    if (++count == index) return t.gameObject;
                }
            }

            return null;
        }
    }
}
