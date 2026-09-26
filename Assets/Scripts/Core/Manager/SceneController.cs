using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scripts.Core.Singleton;
using Scripts.Core.Event;

namespace Scripts.Core.Manager
{
    public enum SceneType
    {
        Manager,
        Title,
        Game,
    }

    /// <summary>
    /// アプリケーション全体のシーンロード・アンロード・状態遷移を一括管理するマネージャークラス
    /// </summary>
    public class SceneController : SingletonMonoBehaviour<SceneController>
    {
        /// <summary>
        /// 現在アクティブなシーン
        /// </summary>
        public SceneType CurrentScene { get; private set; }

        private bool isTransitioning = false;

        protected override void Awake()
        {
            base.Awake();
            
            InitializeCurrentScene();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RequestSceneChangeEvent>(OnRequestSceneChange);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RequestSceneChangeEvent>(OnRequestSceneChange);
        }

        /// <summary>
        /// 起動時のアクティブシーン初期設定
        /// </summary>
        private void InitializeCurrentScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            
            if (System.Enum.TryParse(activeScene.name, out SceneType type))
            {
                CurrentScene = type;
            }
            else
            {
                CurrentScene = SceneType.Title;
            }
        }

        /// <summary>
        /// イベント受信時の処理ハンドラー
        /// </summary>
        private void OnRequestSceneChange(RequestSceneChangeEvent evt)
        {
            ChangeScene(evt.TargetScene);
        }

        /// <summary>
        /// 任意のシーンへ遷移します
        /// </summary>
        /// <param name="nextScene">遷移先のシーン</param>
        public void ChangeScene(SceneType nextScene)
        {
            if (isTransitioning || nextScene == CurrentScene) return;
            
            StartCoroutine(LoadSceneRoutine(nextScene));
        }

        /// <summary>
        /// シーンのアンロードと新シーンの加算ロードを行う内部コルーチン
        /// </summary>
        private IEnumerator LoadSceneRoutine(SceneType nextScene)
        {
            isTransitioning = true;

            if (CurrentScene != SceneType.Manager)
            {
                string currentSceneName = CurrentScene.ToString();
                Scene activeScene = SceneManager.GetSceneByName(currentSceneName);

                if (activeScene.isLoaded)
                {
                    AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentSceneName);
                    if (unloadOp != null)
                    {
                        yield return unloadOp;
                    }
                }
            }

            string nextSceneName = nextScene.ToString();
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);

            while (!loadOp.isDone)
            {
                yield return null;
            }

            Scene newScene = SceneManager.GetSceneByName(nextSceneName);
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
            }

            CurrentScene = nextScene;
            isTransitioning = false;
        }
    }
}