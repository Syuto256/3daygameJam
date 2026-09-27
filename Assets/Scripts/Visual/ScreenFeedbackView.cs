using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面全体の演出。不正解のときにカメラを揺らし、画面の縁を赤く光らせる。
/// カメラに付けて、RouteJudge / UpJudge から呼ばれたときだけ動く
/// </summary>
public class ScreenFeedbackView : MonoBehaviour
{
    [Header("画面の縁を光らせる UI 画像")]
    [SerializeField] private Image _edgeFlash;
    [SerializeField] private float _flashAlpha = 0.55f;
    [SerializeField] private float _flashDuration = 0.4f;

    [Header("カメラの揺れ")]
    [SerializeField] private float _shakeStrength = 0.15f;
    [SerializeField] private float _shakeDuration = 0.25f;

    private Vector3 _basePosition;
    private float _shakeElapsed;
    private float _flashElapsed;

    private void Awake()
    {
        _basePosition = transform.localPosition;
        _shakeElapsed = _shakeDuration;
        _flashElapsed = _flashDuration;
        SetFlashAlpha(0f);
    }

    public void PlayIncorrect()
    {
        _shakeElapsed = 0f;
        _flashElapsed = 0f;
    }

    private void LateUpdate()
    {
        UpdateShake();
        UpdateFlash();
    }

    private void UpdateShake()
    {
        if (_shakeElapsed >= _shakeDuration) return;

        _shakeElapsed += Time.deltaTime;
        float fade = 1f - Mathf.Clamp01(_shakeElapsed / _shakeDuration);

        if (fade <= 0f)
        {
            transform.localPosition = _basePosition;
            return;
        }

        var offset = Random.insideUnitCircle * _shakeStrength * fade;
        transform.localPosition = _basePosition + new Vector3(offset.x, offset.y, 0f);
    }

    private void UpdateFlash()
    {
        if (_edgeFlash == null || _flashElapsed >= _flashDuration) return;

        _flashElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_flashElapsed / _flashDuration);
        SetFlashAlpha(_flashAlpha * (1f - t));
    }

    private void SetFlashAlpha(float alpha)
    {
        if (_edgeFlash == null) return;

        var color = _edgeFlash.color;
        color.a = alpha;
        _edgeFlash.color = color;
    }
}
