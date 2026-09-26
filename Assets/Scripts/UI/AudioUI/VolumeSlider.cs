using UnityEngine;
using UnityEngine.UI;
using Scripts.Core.Event;

namespace Scripts.UI.AudioUI
{
    [RequireComponent(typeof(Slider))]
    public class VolumeSlider : MonoBehaviour
    {
        [Header("対象の音量タイプ")]
        [SerializeField] private VolumeType volumeType = VolumeType.BGM;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            
            slider.minValue = 0.0001f;
            slider.maxValue = 1.0f;
        }

        private void OnEnable()
        {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        private void OnSliderValueChanged(float value)
        {
            EventBus.Publish(new ChangeVolumeEvent(volumeType, value));
        }
    }
}