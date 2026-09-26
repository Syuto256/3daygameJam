using UnityEngine;
using UnityEngine.UI;
using Scripts.Core.Manager;
using Scripts.Core.Event;

namespace Scripts.UI
{
    /// <summary>
    /// ボタンを押すと指定されたシーンに遷移するUIコンポーネント
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SceneChangeButton : MonoBehaviour
    {
        [Header("遷移先のシーン")]
        [SerializeField] private SceneType targetScene;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        private void OnButtonClicked()
        {
            EventBus.Publish(new RequestSceneChangeEvent(targetScene));
        }
    }
}