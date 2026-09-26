using UnityEngine;

public class UpJudge : MonoBehaviour
{
    [SerializeField] private ScoreManager _scoreManager;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Luggage luggage = other.GetComponent<Luggage>();
        if(luggage.CurrentLuggageType == LuggageStatus.Defective)
        {
            Debug.Log("正解");
            _scoreManager.AddScore();
        }
        else
        {
            Debug.Log("失敗");
            _scoreManager.SubtractScore();
        }
    }

}
