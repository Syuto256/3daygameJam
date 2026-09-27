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

    // --- 音量イベント ---
    /// <summary>
    /// 音量カテゴリの定義
    /// </summary>
    public enum VolumeType
    {
        BGM,
        SE
    }

    /// <summary>
    /// 音量変更リクエストイベント (0.0f ～ 1.0f)
    /// </summary>
    public readonly struct ChangeVolumeEvent
    {
        public readonly VolumeType Type;
        public readonly float Volume; 

        public ChangeVolumeEvent(VolumeType type, float volume)
        {
            Type = type;
            Volume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// ゲームオーバーイベント
    /// </summary>
    public readonly struct GameOverEvent
    {
        // 必要に応じてスコアなどを保持して渡すことも可能です
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