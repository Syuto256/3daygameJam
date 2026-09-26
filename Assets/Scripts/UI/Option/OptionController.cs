using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Scripts.Core.Audio;
using Scripts.Core.Event;

namespace Scripts.UI.Option
{
    /// <summary>
    /// オプションメニューのUIコントローラー
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class OptionController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private GameObject volumePanel;

        [Header("Option Buttons")]
        [SerializeField] private Button optionButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button volumeMenuButton;
        [SerializeField] private Button exitGameButton;

        [Header("Volume Controls")]
        [SerializeField] private Button audioToggleButton;
        [SerializeField] private TextMeshProUGUI audioToggleText;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider seSlider;
        [SerializeField] private Button volumeCloseButton;

        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SE_VOLUME_KEY = "SEVolume";
        private const string AUDIO_MUTE_KEY = "AudioMute";

        private const float DEFAULT_VOLUME = 0.8f;
        private bool isMuted = false;

        private void Start()
        {
            if (optionPanel != null) optionPanel.SetActive(false);
            if (volumePanel != null) volumePanel.SetActive(false);

            // ボタンイベントの登録
            if (optionButton != null) optionButton.onClick.AddListener(OpenOptionPanel);
            if (resumeButton != null) resumeButton.onClick.AddListener(CloseOptionPanel);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartGame);
            if (volumeMenuButton != null) volumeMenuButton.onClick.AddListener(OpenVolumePanel);
            if (exitGameButton != null) exitGameButton.onClick.AddListener(OnExitGame);

            if (audioToggleButton != null) audioToggleButton.onClick.AddListener(ToggleAudio);
            if (volumeCloseButton != null) volumeCloseButton.onClick.AddListener(CloseVolumePanel);

            // 1. 設定データの読み込みと初期化（※スライダーイベント登録前に読み込みます）
            LoadAndApplySettings();

            // 2. 初期化後にスライダーイベントを登録（初期化時のイベント発火による値上書き防止）
            if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            if (seSlider != null) seSlider.onValueChanged.AddListener(OnSEVolumeChanged);
        }

        /// <summary>
        /// アプリ終了時に確実に PlayerPrefs を保存する
        /// </summary>
        private void OnApplicationQuit()
        {
            SaveAllSettings();
        }

        /// <summary>
        /// モバイル等のバックグラウンド移行時にも保存する
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveAllSettings();
            }
        }

        private void OnDisable()
        {
            SaveAllSettings();
        }

        // --- パネル開閉処理 ---
        public void OpenOptionPanel()
        {
            if (optionPanel != null) optionPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void CloseOptionPanel()
        {
            if (optionPanel != null) optionPanel.SetActive(false);
            if (volumePanel != null) volumePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void OpenVolumePanel()
        {
            if (volumePanel != null) volumePanel.SetActive(true);
        }

        public void CloseVolumePanel()
        {
            if (volumePanel != null) volumePanel.SetActive(false);
        }

        private void OnRestartGame()
        {
            SaveAllSettings();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnExitGame()
        {
            SaveAllSettings();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // --- データロード・セーブ処理 ---
        private void LoadAndApplySettings()
        {
            // AUDIO_MUTE_KEY の読み込み (0: ON[未ミュート], 1: OFF[ミュート])
            isMuted = PlayerPrefs.GetInt(AUDIO_MUTE_KEY, 0) == 1;

            float savedBGM = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, DEFAULT_VOLUME);
            float savedSE = PlayerPrefs.GetFloat(SE_VOLUME_KEY, DEFAULT_VOLUME);

            if (bgmSlider != null) bgmSlider.value = savedBGM;
            if (seSlider != null) seSlider.value = savedSE;

            UpdateAudioToggleUI();
            ApplyAllVolumes();
        }

        private void SaveAllSettings()
        {
            PlayerPrefs.SetInt(AUDIO_MUTE_KEY, isMuted ? 1 : 0);
            if (bgmSlider != null) PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmSlider.value);
            if (seSlider != null) PlayerPrefs.SetFloat(SE_VOLUME_KEY, seSlider.value);
            PlayerPrefs.Save();
        }

        private void ToggleAudio()
        {
            isMuted = !isMuted;
            
            // 状態の保存
            PlayerPrefs.SetInt(AUDIO_MUTE_KEY, isMuted ? 1 : 0);
            PlayerPrefs.Save();

            UpdateAudioToggleUI();
            ApplyAllVolumes();
        }

        private void UpdateAudioToggleUI()
        {
            if (audioToggleText != null)
            {
                audioToggleText.text = isMuted ? "音声: OFF" : "音声: ON";
            }
        }

        private void OnBGMVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, value);
            PlayerPrefs.Save();

            ApplyVolume(VolumeType.BGM, value);
        }

        private void OnSEVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(SE_VOLUME_KEY, value);
            PlayerPrefs.Save();

            ApplyVolume(VolumeType.SE, value);
        }

        private void ApplyAllVolumes()
        {
            if (bgmSlider != null) ApplyVolume(VolumeType.BGM, bgmSlider.value);
            if (seSlider != null) ApplyVolume(VolumeType.SE, seSlider.value);
        }

        private void ApplyVolume(VolumeType type, float value)
        {
            float targetVolume = isMuted ? 0f : value;

            EventBus.Publish(new ChangeVolumeEvent(type, targetVolume));

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetVolume(type, targetVolume);
            }
        }
    }
}