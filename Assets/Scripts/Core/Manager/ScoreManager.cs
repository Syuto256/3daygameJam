using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private int _score;
    public int Score => _score;
    [SerializeField] private int _addScore = 100;
    [SerializeField] private int _subtractScore = -100;
    public int AddAmount => _addScore;
    public int SubtractAmount => _subtractScore;

    public void AddScore()
    {
        _score += _addScore;
    }
    public void SubtractScore()
    {
        _score += _subtractScore;
    }


}
