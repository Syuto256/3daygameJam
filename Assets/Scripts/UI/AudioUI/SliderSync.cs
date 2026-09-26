using UnityEngine;
using UnityEngine.UI;
using Scripts.Core.Manager;

namespace Scripts.UI.AudioUI
{
    [RequireComponent(typeof(Slider))]
    public class SliderSync : MonoBehaviour
    {
        [Header("Audio Type")]
        [SerializeField] private AudioSliderType sliderType = AudioSliderType.BGM;

        private Slider slider;
        private bool isUpdatingSelf = false;

        private void Awake()
        {
            slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            TryConnectManager();

            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderUIChanged);
                slider.onValueChanged.AddListener(OnSliderUIChanged);
            }
        }

        private void Start()
        {
            TryConnectManager();
        }

        private void OnDisable()
        {
            if (SettingsManager.HasInstance)
            {
                if (sliderType == AudioSliderType.BGM)
                {
                    SettingsManager.Instance.OnBGMVolumeChanged -= OnValueChangedFromManager;
                }
                else if (sliderType == AudioSliderType.SE)
                {
                    SettingsManager.Instance.OnSEVolumeChanged -= OnValueChangedFromManager;
                }
            }

            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderUIChanged);
            }
        }

        /// <summary>
        /// SettingsManagerが存在すればイベント登録とUIの更新を行う
        /// </summary>
        private void TryConnectManager()
        {
            if (SettingsManager.HasInstance)
            {
                isUpdatingSelf = true;
                UpdateSliderUI(SettingsManager.Instance.GetVolume(sliderType));
                isUpdatingSelf = false;

                if (sliderType == AudioSliderType.BGM)
                {
                    SettingsManager.Instance.OnBGMVolumeChanged -= OnValueChangedFromManager;
                    SettingsManager.Instance.OnBGMVolumeChanged += OnValueChangedFromManager;
                }
                else if (sliderType == AudioSliderType.SE)
                {
                    SettingsManager.Instance.OnSEVolumeChanged -= OnValueChangedFromManager;
                    SettingsManager.Instance.OnSEVolumeChanged += OnValueChangedFromManager;
                }
            }
        }

        private void OnSliderUIChanged(float value)
        {
            if (isUpdatingSelf) return;

            if (SettingsManager.HasInstance)
            {
                SettingsManager.Instance.SetVolume(sliderType, value);
            }
        }

        private void OnValueChangedFromManager(float newValue)
        {
            UpdateSliderUI(newValue);
        }

        private void UpdateSliderUI(float value)
        {
            isUpdatingSelf = true;
            if (slider != null)
            {
                slider.value = value;
            }
            isUpdatingSelf = false;
        }
    }
}