using UnityEngine;
using Scripts.Core.Manager;

namespace Scripts.Core.Event
{
    // --- シーンイベント ---
    public readonly struct RequestSceneChangeEvent
    {
        public readonly SceneType TargetScene;
        public RequestSceneChangeEvent(SceneType targetScene) => TargetScene = targetScene;
    }

    // --- オーディオイベント ---
    /// <summary>
    /// SE再生リクエストイベント
    /// </summary>
    public readonly struct PlaySEEvent
    {
        public readonly string SeName;
        public readonly float Volume;

        public PlaySEEvent(string seName, float volume = 1.0f)
        {
            SeName = seName;
            Volume = volume;
        }
    }

    /// <summary>
    /// BGM再生リクエストイベント
    /// </summary>
    public readonly struct PlayBGMEvent
    {
        public readonly string BgmName;
        public readonly bool Loop;
        public readonly float FadeDuration;

        public PlayBGMEvent(string bgmName, bool loop = true, float fadeDuration = 0.5f)
        {
            BgmName = bgmName;
            Loop = loop;
            FadeDuration = fadeDuration;
        }
    }
}