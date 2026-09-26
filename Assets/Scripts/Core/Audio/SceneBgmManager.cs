using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scripts.Core.Manager;
using Scripts.Core.Event;

namespace Scripts.Core.Audio
{
    [Serializable]
    public struct SceneBgmConfig
    {
        public SceneType Scene;
        public string BgmName;
    }

    public class SceneBgmManager : MonoBehaviour
    {
        [Header("シーンごとのBGM設定")]
        [SerializeField] private List<SceneBgmConfig> sceneBgmList = new();

        private readonly Dictionary<SceneType, string> sceneBgmDict = new();

        private void Awake()
        {
            foreach (var config in sceneBgmList)
            {
                sceneBgmDict[config.Scene] = config.BgmName;
            }
        }

        private void Start()
        {
            PlayBgmForActiveScene();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RequestSceneChangeEvent>(OnSceneChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RequestSceneChangeEvent>(OnSceneChanged);
        }

        private void OnSceneChanged(RequestSceneChangeEvent evt)
        {
            PlayBgmForScene(evt.TargetScene);
        }

        private void PlayBgmForActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (Enum.TryParse(activeScene.name, out SceneType currentScene))
            {
                PlayBgmForScene(currentScene);
            }
        }

        private void PlayBgmForScene(SceneType scene)
        {
            if (sceneBgmDict.TryGetValue(scene, out var bgmName))
            {
                if (!string.IsNullOrEmpty(bgmName))
                {
                    EventBus.Publish(new PlayBGMEvent(bgmName));
                }
            }
        }
    }
}