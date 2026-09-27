using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 荷物を判定した場所（箱・工場）の演出。
/// スコアの文字、パーティクル、箱の揺れ、ランプの光をまとめて出す。
/// いつ出すかは知らず、RouteJudge / UpJudge から呼ばれたときだけ動く
/// </summary>
public class JudgeFeedbackView : MonoBehaviour
{
    [Header("弾ませる・揺らす対象（箱など）")]
    [SerializeField] private Transform _bounceTarget;
    [Header("正解・不正解で光らせるランプ（加算のスプライト）")]
    [SerializeField] private SpriteRenderer _lamp;
    [SerializeField] private BurstParticles _correctParticles;
    [SerializeField] private BurstParticles _incorrectParticles;

    [Header("スコアの文字")]
    [SerializeField] private TMP_FontAsset _font;
    [SerializeField] private Vector3 _popupOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Color _correctColor = new Color(0.55f, 1f, 0.45f);
    [SerializeField] private Color _incorrectColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private int _popupSortingOrder = 60;

    private const float BounceDuration = 0.3f;
    private const float LampDuration = 0.5f;

    private readonly List<ScorePopup> _popups = new List<ScorePopup>();
    private Vector3 _baseScale;
    private Vector3 _basePosition;
    private float _bounceElapsed = BounceDuration;
    private bool _bounceIsCorrect;
    private float _lampElapsed = LampDuration;
    private float _lampMaxAlpha;

    private void Awake()
    {
        if (_bounceTarget != null)
        {
            _baseScale = _bounceTarget.localScale;
            _basePosition = _bounceTarget.localPosition;
        }

        if (_lamp != null)
        {
            _lampMaxAlpha = _lamp.color.a;
            SetLampAlpha(0f);
        }
    }

    public void PlayCorrect(int points)
    {
        ShowPopup($"+{points}", _correctColor, true);
        if (_correctParticles != null) _correctParticles.Play();
        StartBounce(true);
        StartLamp(_correctColor);
    }

    public void PlayIncorrect(int points)
    {
        ShowPopup($"-{Mathf.Abs(points)}", _incorrectColor, false);
        if (_incorrectParticles != null) _incorrectParticles.Play();
        StartBounce(false);
        StartLamp(_incorrectColor);
    }

    private void Update()
    {
        UpdateBounce();
        UpdateLamp();
    }

    // ---- スコアの文字 ------------------------------------------------

    private void ShowPopup(string label, Color color, bool isCorrect)
    {
        if (_font == null) return;

        ScorePopup popup = null;
        foreach (var candidate in _popups)
        {
            if (candidate.IsPlaying) continue;
            popup = candidate;
            break;
        }

        if (popup == null)
        {
            popup = ScorePopup.Create(transform, _font, _popupSortingOrder);
            _popups.Add(popup);
        }

        popup.Play(transform.position + _popupOffset, label, color, isCorrect);
    }

    // ---- 箱の揺れ ----------------------------------------------------

    private void StartBounce(bool isCorrect)
    {
        if (_bounceTarget == null) return;

        _bounceIsCorrect = isCorrect;
        _bounceElapsed = 0f;
    }

    private void UpdateBounce()
    {
        if (_bounceTarget == null || _bounceElapsed >= BounceDuration) return;

        _bounceElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_bounceElapsed / BounceDuration);
        float fade = 1f - t;

        if (_bounceIsCorrect)
        {
            // ポヨンと潰れて伸びる
            float wave = Mathf.Sin(t * Mathf.PI * 2f) * fade;
            _bounceTarget.localScale = Vector3.Scale(_baseScale, new Vector3(1f + wave * 0.12f, 1f - wave * 0.12f, 1f));
            _bounceTarget.localPosition = _basePosition;
        }
        else
        {
            // ガタガタと左右に揺れる
            float shake = Mathf.Sin(t * Mathf.PI * 8f) * fade;
            _bounceTarget.localScale = _baseScale;
            _bounceTarget.localPosition = _basePosition + new Vector3(shake * 0.25f, 0f, 0f);
        }

        if (t >= 1f)
        {
            _bounceTarget.localScale = _baseScale;
            _bounceTarget.localPosition = _basePosition;
        }
    }

    // ---- ランプ ------------------------------------------------------

    private void StartLamp(Color color)
    {
        if (_lamp == null) return;

        color.a = _lamp.color.a;
        _lamp.color = color;
        _lampElapsed = 0f;
    }

    private void UpdateLamp()
    {
        if (_lamp == null || _lampElapsed >= LampDuration) return;

        _lampElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_lampElapsed / LampDuration);

        // パッと点いて、ゆっくり消える
        SetLampAlpha(_lampMaxAlpha * (1f - t) * (1f - t));
    }

    private void SetLampAlpha(float alpha)
    {
        var color = _lamp.color;
        color.a = alpha;
        _lamp.color = color;
    }
}
