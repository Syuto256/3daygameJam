using UnityEngine;

public class RouteJudge : MonoBehaviour
{
    [SerializeField] private CorrectType _judgeRoute;
    [SerializeField] private ScoreManager _scoreManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Luggage luggage = other.GetComponent<Luggage>();
        if(LuggageStatus.Normal == luggage.CurrentLuggageType)
        {
            if(_judgeRoute == luggage.CorrectRoute)
            {
                Debug.Log("正解");
                _scoreManager.AddScore();
            }
            else
            {
                Debug.Log("不正解");
                _scoreManager.SubtractScore();
            }
        }
        else
        {
            Debug.Log("不正解");
            _scoreManager.SubtractScore();
        }
    }
}
