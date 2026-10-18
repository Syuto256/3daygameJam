using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.OXQuiz.UI
{
    public class RulePanelPresenter : MonoBehaviour
    {
        [Serializable]
        public struct RulePage
        {
            public Sprite ImageSprite;
            public Sprite SecondaryImageSprite;
            [TextArea(3, 5)] public string DescriptionText;
        }

        [Header("UI References")]
        [SerializeField] private GameObject _rulePanel;
        [SerializeField] private Image _pageImage;
        [SerializeField] private Image _secondaryPageImage;
        [SerializeField] private TextMeshProUGUI _pageText;

        [Header("Buttons")]
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _backButton;

        [Header("Pages Setup")]
        [SerializeField] private List<RulePage> _pages = new List<RulePage>();

        private int _currentPageIndex = 0;

        private void Awake()
        {
            _openButton.onClick.AddListener(OpenPanel);
            _closeButton.onClick.AddListener(ClosePanel);
            _nextButton.onClick.AddListener(OnClickNext);
            _backButton.onClick.AddListener(OnClickBack);

            _rulePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            _openButton.onClick.RemoveListener(OpenPanel);
            _closeButton.onClick.RemoveListener(ClosePanel);
            _nextButton.onClick.RemoveListener(OnClickNext);
            _backButton.onClick.RemoveListener(OnClickBack);
        }

        public void OpenPanel()
        {
            _currentPageIndex = 0;
            UpdatePageDisplay();
            _rulePanel.SetActive(true);
        }

        public void ClosePanel()
        {
            _rulePanel.SetActive(false);
        }

        private void OnClickNext()
        {
            if (_currentPageIndex < _pages.Count - 1)
            {
                _currentPageIndex++;
                UpdatePageDisplay();
            }
        }

        private void OnClickBack()
        {
            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                UpdatePageDisplay();
            }
        }

        private void UpdatePageDisplay()
        {
            var page = _pages[_currentPageIndex];
            bool hasSecondaryImage = page.SecondaryImageSprite != null;

            _pageImage.sprite = page.ImageSprite;
            _pageImage.preserveAspect = true;
            _pageImage.rectTransform.anchoredPosition = hasSecondaryImage
                ? new Vector2(0f, 105f)
                : Vector2.zero;
            _pageImage.rectTransform.sizeDelta = hasSecondaryImage
                ? new Vector2(920f, 170f)
                : new Vector2(1010f, 430f);

            if (_secondaryPageImage != null)
            {
                _secondaryPageImage.sprite = page.SecondaryImageSprite;
                _secondaryPageImage.gameObject.SetActive(hasSecondaryImage);
            }

            _pageText.text = page.DescriptionText;

            _backButton.gameObject.SetActive(_currentPageIndex > 0);
            _nextButton.gameObject.SetActive(_currentPageIndex < _pages.Count - 1);
        }
    }
}
