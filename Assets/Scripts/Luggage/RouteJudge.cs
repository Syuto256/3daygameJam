using UnityEngine;
using Scripts.Core.Event;

public class RouteJudge : MonoBehaviour
{
    [SerializeField] private CorrectType _judgeRoute;
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private HPManager _hpManager;

    private const string CORRECT_SE_NAME = "CorrectSE";
    private const string INCORRECT_SE_NAME = "IncorrectSE";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<Luggage>(out var luggage)) return;

        if (luggage.CurrentLuggageType == LuggageStatus.Normal)
        {
            if (luggage.CorrectRoute == _judgeRoute)
            {
                OnCorrect();
            }
            else
            {
                OnIncorrect();
            }
        }
        else if (luggage.CurrentLuggageType == LuggageStatus.Defective)
        {
            if (_judgeRoute == CorrectType.Up)
            {
                OnCorrect();
            }
            else
            {
                OnIncorrect();
            }
        }
    }

    private void OnCorrect()
    {
        Debug.Log("正解");
        if (_scoreManager != null) _scoreManager.AddScore();
        EventBus.Publish(new PlaySEEvent(CORRECT_SE_NAME));
    }

    private void OnIncorrect()
    {
        Debug.Log("不正解");
        _hpManager.TakeDamage();
        if (_scoreManager != null) _scoreManager.SubtractScore();
        EventBus.Publish(new PlaySEEvent(INCORRECT_SE_NAME));
    }
}