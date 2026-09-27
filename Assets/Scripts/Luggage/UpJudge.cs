using UnityEngine;
using Scripts.Core.Event;

public class UpJudge : MonoBehaviour
{
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private JudgeFeedbackView _feedbackView;
    [SerializeField] private ScreenFeedbackView _screenFeedbackView;

    private const string CORRECT_SE_NAME = "CorrectSE";
    private const string INCORRECT_SE_NAME = "IncorrectSE";
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<Luggage>(out var luggage)) return;
        other.TryGetComponent<LuggageJudgeView>(out var luggageView);

        if(luggage.CurrentLuggageType == LuggageStatus.Defective)
        {
            Debug.Log("正解");
            _scoreManager.AddScore();
            EventBus.Publish(new PlaySEEvent(CORRECT_SE_NAME));
            if (_feedbackView != null) _feedbackView.PlayCorrect(_scoreManager.AddAmount);
            if (luggageView != null) luggageView.PlayCorrect();
        }
        else
        {
            Debug.Log("失敗");
            _scoreManager.SubtractScore();
            EventBus.Publish(new PlaySEEvent(INCORRECT_SE_NAME));
            if (_feedbackView != null) _feedbackView.PlayIncorrect(_scoreManager.SubtractAmount);
            if (luggageView != null) luggageView.PlayIncorrect();
            if (_screenFeedbackView != null) _screenFeedbackView.PlayIncorrect();
        }
    }

}
