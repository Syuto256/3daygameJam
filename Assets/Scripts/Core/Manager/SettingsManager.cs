using System;
using UnityEngine;
using Scripts.Core.Singleton;

namespace Scripts.Core.Manager
{
    /// <summary>
    /// 音量の種類を定義する列挙型
    /// </summary>
    public enum AudioSliderType
    {
        BGM,
        SE
    }

    /// <summary>
    /// ゲームの設定を管理するシングルトンマネージャークラス
    /// </summary>
    public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
    {
        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float seVolume = 0.8f;

        public event Action<float> OnBGMVolumeChanged;
        public event Action<float> OnSEVolumeChanged;

        public float BGMVolume
        {
            get => bgmVolume;
            set => SetVolume(AudioSliderType.BGM, value);
        }

        public float SEVolume
        {
            get => seVolume;
            set => SetVolume(AudioSliderType.SE, value);
        }

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            OnBGMVolumeChanged?.Invoke(bgmVolume);
            OnSEVolumeChanged?.Invoke(seVolume);
        }

        /// <summary>
        /// 指定された種類の音量を取得
        /// </summary>
        public float GetVolume(AudioSliderType type)
        {
            return type switch
            {
                AudioSliderType.BGM => bgmVolume,
                AudioSliderType.SE => seVolume,
                _ => 1.0f
            };
        }

        /// <summary>
        /// 指定された種類の音量を更新
        /// </summary>
        public void SetVolume(AudioSliderType type, float newValue)
        {
            switch (type)
            {
                case AudioSliderType.BGM:
                    if (Mathf.Approximately(bgmVolume, newValue)) return;
                    bgmVolume = newValue;
                    OnBGMVolumeChanged?.Invoke(bgmVolume);
                    break;

                case AudioSliderType.SE:
                    if (Mathf.Approximately(seVolume, newValue)) return;
                    seVolume = newValue;
                    OnSEVolumeChanged?.Invoke(seVolume);
                    break;
            }
        }
    }
}