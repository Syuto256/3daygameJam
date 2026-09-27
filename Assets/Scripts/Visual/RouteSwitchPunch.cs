using UnityEngine;

/// <summary>
/// 分岐パーツをポンと弾ませる見た目の担当。
/// いつ弾ませるかは知らず、RouteInput から Play() を呼ばれたときだけ動く
/// </summary>
public class RouteSwitchPunch : MonoBehaviour
{
    [Header("弾む大きさ（元の大きさに対する倍率）")]
    [SerializeField] private float _punchScale = 1.12f;
    [Header("弾んで戻るまでの秒数")]
    [SerializeField] private float _duration = 0.18f;

    private Vector3 _baseScale;
    private float _elapsed;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _elapsed = _duration;
    }

    /// <summary>弾ませる。途中で呼ばれたら最初からやり直す</summary>
    public void Play()
    {
        _elapsed = 0f;
    }

    private void Update()
    {
        if (_elapsed >= _duration) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        // 一気に膨らんで、ゆっくり戻る。t = 1 で sin が 0 になり元の大きさに戻る
        float wave = Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);
        transform.localScale = _baseScale * Mathf.Lerp(1f, _punchScale, wave);
    }
}
