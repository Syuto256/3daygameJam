using UnityEngine;

/// <summary>
/// 判定された荷物の演出。
/// 正解なら箱へ吸い込まれるように縮んで消え、不正解なら赤く点滅してグラグラ揺れる。
/// 荷物は使い回されるので、有効になるたびに元の見た目へ戻す
/// </summary>
public class LuggageJudgeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _shrinkDuration = 0.25f;
    [SerializeField] private float _incorrectDuration = 0.5f;
    [SerializeField] private Color _incorrectColor = new Color(1f, 0.35f, 0.35f);

    private enum State
    {
        None,
        Correct,
        Incorrect
    }

    private Vector3 _baseScale;
    private State _state;
    private float _elapsed;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        _state = State.None;
        transform.localScale = _baseScale;
        if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
    }

    public void PlayCorrect()
    {
        _state = State.Correct;
        _elapsed = 0f;
    }

    public void PlayIncorrect()
    {
        _state = State.Incorrect;
        _elapsed = 0f;
    }

    private void Update()
    {
        if (_state == State.None) return;

        _elapsed += Time.deltaTime;

        // 荷物は Translate で自分の向きに進むので、回転はさせず大きさと色だけで見せる
        if (_state == State.Correct)
        {
            float t = Mathf.Clamp01(_elapsed / _shrinkDuration);
            float scale = t < 0.3f ? Mathf.Lerp(1f, 1.2f, t / 0.3f) : Mathf.Lerp(1.2f, 0f, (t - 0.3f) / 0.7f);
            transform.localScale = _baseScale * scale;
            if (t >= 1f) _state = State.None;
            return;
        }

        float u = Mathf.Clamp01(_elapsed / _incorrectDuration);
        float wobble = Mathf.Sin(_elapsed * 40f) * (1f - u);
        transform.localScale = Vector3.Scale(_baseScale, new Vector3(1f + wobble * 0.15f, 1f - wobble * 0.15f, 1f));

        if (_spriteRenderer != null)
        {
            bool blink = Mathf.Repeat(_elapsed, 0.12f) < 0.06f;
            _spriteRenderer.color = blink && u < 1f ? _incorrectColor : Color.white;
        }

        if (u >= 1f)
        {
            transform.localScale = _baseScale;
            _state = State.None;
        }
    }
}
