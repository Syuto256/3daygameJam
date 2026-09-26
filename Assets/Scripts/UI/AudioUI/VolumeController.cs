using UnityEngine;
using UnityEngine.UI;
using Scripts.Core.Audio;
using Scripts.Core.Event;

namespace Scripts.UI.AudioUI
{
    [RequireComponent(typeof(Slider))]
    public class VolumeController : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider seSlider;

        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SE_VOLUME_KEY = "SEVolume";

        private const float DEFAULT_VOLUME = 0.8f;

        private void Start()
        {
            float savedBGMVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, DEFAULT_VOLUME);
            float savedSEVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, DEFAULT_VOLUME);

            if (bgmSlider != null)
            {
                bgmSlider.value = savedBGMVolume;
                bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            if (seSlider != null)
            {
                seSlider.value = savedSEVolume;
                seSlider.onValueChanged.AddListener(OnSEVolumeChanged);
            }

            ApplyVolume(VolumeType.BGM, savedBGMVolume);
            ApplyVolume(VolumeType.SE, savedSEVolume);
        }

        private void OnBGMVolumeChanged(float value)
        {
            ApplyVolume(VolumeType.BGM, value);
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, value);
            PlayerPrefs.Save();
        }

        private void OnSEVolumeChanged(float value)
        {
            ApplyVolume(VolumeType.SE, value);
            PlayerPrefs.SetFloat(SE_VOLUME_KEY, value);
            PlayerPrefs.Save();
        }

        private void ApplyVolume(VolumeType type, float volume)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetVolume(type, volume);
            }
        }
    }
}