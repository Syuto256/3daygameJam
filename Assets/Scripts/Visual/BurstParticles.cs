using UnityEngine;

/// <summary>
/// 呼ばれた瞬間に粒をパッと弾けさせるパーティクル。
/// ParticleSystem の細かい設定はここでまとめて行うので、シーン側では数や色などを決めるだけでよい
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class BurstParticles : MonoBehaviour
{
    [SerializeField] private Material _material;
    [SerializeField] private int _count = 16;
    [SerializeField] private Vector2 _speedRange = new Vector2(2f, 5f);
    [SerializeField] private Vector2 _lifetimeRange = new Vector2(0.4f, 0.8f);
    [SerializeField] private Vector2 _sizeRange = new Vector2(0.2f, 0.5f);
    [SerializeField] private Color _colorA = Color.white;
    [SerializeField] private Color _colorB = Color.white;
    [Header("重力（マイナスで上へ昇る）")]
    [SerializeField] private float _gravity = 1f;
    [Header("粒が飛び出す向きの広がり（度）。360 で全方向")]
    [SerializeField] private float _spreadAngle = 360f;
    [SerializeField] private float _rotationSpeed = 180f;
    [Header("寿命の終わりに向けて大きくなるか（煙向け）")]
    [SerializeField] private bool _growOverLifetime;
    [SerializeField] private int _sortingOrder = 30;

    private ParticleSystem _particleSystem;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        Setup();
    }

    /// <summary>粒を一度に出す</summary>
    public void Play()
    {
        _particleSystem.Emit(_count);
    }

    private void Setup()
    {
        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(_lifetimeRange.x, _lifetimeRange.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(_speedRange.x, _speedRange.y);
        main.startSize = new ParticleSystem.MinMaxCurve(_sizeRange.x, _sizeRange.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(_colorA, _colorB);
        main.gravityModifier = _gravity;
        main.maxParticles = Mathf.Max(_count * 4, 32);

        var emission = _particleSystem.emission;
        emission.enabled = false;

        // 上向きを中心に、_spreadAngle の範囲へ飛ばす
        var shape = _particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
        shape.arc = _spreadAngle;
        shape.rotation = new Vector3(0f, 0f, 90f - _spreadAngle * 0.5f);

        var rotation = _particleSystem.rotationOverLifetime;
        rotation.enabled = _rotationSpeed != 0f;
        rotation.z = new ParticleSystem.MinMaxCurve(-_rotationSpeed * Mathf.Deg2Rad, _rotationSpeed * Mathf.Deg2Rad);

        var size = _particleSystem.sizeOverLifetime;
        size.enabled = true;
        size.size = _growOverLifetime
            ? new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.6f))
            : new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var color = _particleSystem.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        var particleRenderer = GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sharedMaterial = _material;
        particleRenderer.sortingOrder = _sortingOrder;
    }
}
