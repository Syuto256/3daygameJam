using TMPro;
using UnityEngine;

/// <summary>
/// 「+100」「-100」のように、判定した場所から出て消えていく文字。
/// JudgeFeedbackView が作って使い回す
/// </summary>
public class ScorePopup : MonoBehaviour
{
    private const float Duration = 0.9f;

    private TextMeshPro _text;
    private Vector3 _startPosition;
    private bool _isCorrect;
    private float _elapsed;

    public bool IsPlaying => gameObject.activeSelf;

    public static ScorePopup Create(Transform parent, TMP_FontAsset font, int sortingOrder)
    {
        var go = new GameObject("ScorePopup");
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<TextMeshPro>();
        text.font = font;
        text.fontSize = 6f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(30, 30, 40, 255);
        text.sortingOrder = sortingOrder;

        var popup = go.AddComponent<ScorePopup>();
        popup._text = text;
        go.SetActive(false);
        return popup;
    }

    public void Play(Vector3 position, string label, Color color, bool isCorrect)
    {
        _startPosition = position;
        _isCorrect = isCorrect;
        _elapsed = 0f;

        _text.text = label;
        _text.color = color;
        transform.position = position;
        transform.localScale = Vector3.zero;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / Duration);

        // 最初の 0.15 秒でポンと大きくなる
        float pop = Mathf.Clamp01(_elapsed / 0.15f);
        float scale = pop < 1f ? Mathf.Lerp(0f, 1.25f, pop) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((_elapsed - 0.15f) / 0.1f));
        transform.localScale = Vector3.one * scale;

        Vector3 offset;
        if (_isCorrect)
        {
            // ふわっと上へ浮く
            offset = Vector3.up * (1f - (1f - t) * (1f - t)) * 1.0f;
        }
        else
        {
            // ブルッと震えてから下へ落ちる
            float shake = Mathf.Sin(_elapsed * 60f) * 0.12f * (1f - Mathf.Clamp01(_elapsed / 0.3f));
            offset = new Vector3(shake, -t * t * 0.8f, 0f);
        }
        transform.position = _startPosition + offset;

        var color = _text.color;
        color.a = 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
        _text.color = color;

        if (t >= 1f) gameObject.SetActive(false);
    }
}
