using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Scripts.Core.Event;
using Scripts.Core.Manager;

namespace Scripts.UI
{
    /// <summary>
    /// ゲームオーバー時に表示されるリザルトパネルのUI制御クラス
    /// </summary>
    public class ResultPanel : MonoBehaviour
    {
        [Header("UI 参照")]
        [SerializeField] private GameObject _resultPanelObject; // PanelObject ではなく見た目の ResultPanel を割り当て
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;

        [Header("参照マネージャー")]
        [SerializeField] private ScoreManager _scoreManager;

        private void Awake()
        {
            // PanelObject ではなく、中身の ResultPanel のみを非表示にする
            if (_resultPanelObject != null)
            {
                _resultPanelObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameOverEvent>(OnGameOver);

            if (_retryButton != null) _retryButton.onClick.AddListener(OnRetryClicked);
            if (_titleButton != null) _titleButton.onClick.AddListener(OnTitleClicked);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);

            if (_retryButton != null) _retryButton.onClick.RemoveListener(OnRetryClicked);
            if (_titleButton != null) _titleButton.onClick.RemoveListener(OnTitleClicked);
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (_scoreText != null && _scoreManager != null)
            {
                _scoreText.text = $"Score: {_scoreManager.Score}";
            }

            // ゲームオーバー時に中身の ResultPanel を表示
            if (_resultPanelObject != null)
            {
                _resultPanelObject.SetActive(true);
            }
        }

        private void OnRetryClicked()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private void OnTitleClicked()
        {
            EventBus.Publish(new RequestSceneChangeEvent(SceneType.Title));
        }
    }
}