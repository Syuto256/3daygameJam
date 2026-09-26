using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts.Core.Manager
{
    /// <summary>
    /// ゲームの起動時に、Managerシーンを自動的にロードするためのクラスです。
    /// </summary>
    public static class AppBootstrapper
    {
        private const string MANAGER_SCENE_NAME = "Manager";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeManagerScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == MANAGER_SCENE_NAME)
                {
                    return;
                }
            }

            SceneManager.LoadScene(MANAGER_SCENE_NAME, LoadSceneMode.Additive);
        }
    }
}