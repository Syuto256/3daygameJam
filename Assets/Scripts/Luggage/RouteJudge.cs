using UnityEngine;
using Scripts.Core.Event;

public class RouteJudge : MonoBehaviour
{
    [SerializeField] private CorrectType _judgeRoute;
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private HPManager _hpManager;
    [SerializeField] private JudgeFeedbackView _feedbackView;
    [SerializeField] private ScreenFeedbackView _screenFeedbackView;

    private const string CORRECT_SE_NAME = "CorrectSE";
    private const string INCORRECT_SE_NAME = "IncorrectSE";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<Luggage>(out var luggage)) return;
        other.TryGetComponent<LuggageJudgeView>(out var luggageView);

        if (luggage.CurrentLuggageType == LuggageStatus.Normal)
        {
            if (luggage.CorrectRoute == _judgeRoute)
            {
                OnCorrect(luggageView);
            }
            else
            {
                OnIncorrect(luggageView);
            }
        }
        else if (luggage.CurrentLuggageType == LuggageStatus.Defective)
        {
            if (_judgeRoute == CorrectType.Up)
            {
                OnCorrect(luggageView);
            }
            else
            {
                OnIncorrect(luggageView);
            }
        }
    }

    private void OnCorrect(LuggageJudgeView luggageView)
    {
        Debug.Log("正解");
        if (_scoreManager != null) _scoreManager.AddScore();
        EventBus.Publish(new PlaySEEvent(CORRECT_SE_NAME));

        int points = _scoreManager != null ? _scoreManager.AddAmount : 0;
        if (_feedbackView != null) _feedbackView.PlayCorrect(points);
        if (luggageView != null) luggageView.PlayCorrect();
    }

    private void OnIncorrect(LuggageJudgeView luggageView)
    {
        Debug.Log("不正解");
        if (_hpManager != null) _hpManager.TakeDamage();
        if (_scoreManager != null) _scoreManager.SubtractScore();
        EventBus.Publish(new PlaySEEvent(INCORRECT_SE_NAME));

        int points = _scoreManager != null ? _scoreManager.SubtractAmount : 0;
        if (_feedbackView != null) _feedbackView.PlayIncorrect(points);
        if (luggageView != null) luggageView.PlayIncorrect();
        if (_screenFeedbackView != null) _screenFeedbackView.PlayIncorrect();
    }
}