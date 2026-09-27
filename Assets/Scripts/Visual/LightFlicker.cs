using UnityEngine;

/// <summary>
/// 光のスプライトをゆっくり明滅させる。たまに蛍光灯のようにチラつかせることもできる
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LightFlicker : MonoBehaviour
{
    [Header("基本の明るさ（アルファ）")]
    [SerializeField] private float _baseAlpha = 0.5f;
    [Header("ゆらぎの幅と速さ")]
    [SerializeField] private float _pulseAmplitude = 0.08f;
    [SerializeField] private float _pulseSpeed = 1.5f;
    [Header("チラつきが起きる確率（1秒あたり）。0 ならチラつかない")]
    [SerializeField] private float _flickerChance = 0f;
    [SerializeField] private float _flickerDuration = 0.12f;

    private SpriteRenderer _renderer;
    private float _phase;
    private float _flickerTimer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();

        // 複数置いたときに揃って明滅しないよう、位相をずらす
        _phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float alpha = _baseAlpha + Mathf.Sin(Time.time * _pulseSpeed + _phase) * _pulseAmplitude;

        if (_flickerTimer > 0f)
        {
            _flickerTimer -= Time.deltaTime;
            alpha *= Random.Range(0.2f, 0.6f);
        }
        else if (_flickerChance > 0f && Random.value < _flickerChance * Time.deltaTime)
            _flickerTimer = _flickerDuration;

        var color = _renderer.color;
        color.a = Mathf.Clamp01(alpha);
        _renderer.color = color;
    }
}
