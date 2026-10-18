using UnityEngine;
using Scripts.Core.Event;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private int _score;
    public int Score => _score;
    [SerializeField] private int _addScore = 100;
    [SerializeField] private int _subtractScore = -100;
    public int AddAmount => _addScore;
    public int SubtractAmount => _subtractScore;
    private bool _isGameOver = false;

    private void OnEnable()
    {
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
    }

    private void OnGameOver(GameOverEvent evt)
    {
        _isGameOver = true;
    }

    public void AddScore()
    {
        if (_isGameOver)
        {
            return;
        }
        _score += _addScore;
    }
    public void SubtractScore()
    {
        if (_isGameOver)
        {
            return;
        }

        if (_score + _subtractScore >= 0)
        {
            _score += _subtractScore;
        }
        else
        {
            _score = 0;
        }
    }

}
