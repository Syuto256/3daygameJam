using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private TextMeshProUGUI _text;

    private void Update()
    {
        _text.text = "Score: " + _scoreManager.Score.ToString();
    }

}
