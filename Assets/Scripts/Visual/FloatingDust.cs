using UnityEngine;

/// <summary>
/// 工場の空気中を漂うほこりを、小さなスプライトをたくさん動かして表現する。
/// 範囲の外へ出たら反対側へ戻すので、数は増えも減りもしない
/// </summary>
public class FloatingDust : MonoBehaviour
{
    [SerializeField] private Sprite _sprite;
    [SerializeField] private int _count = 40;
    [Header("漂わせる範囲（この位置を中心とした幅・高さ）")]
    [SerializeField] private Vector2 _area = new Vector2(18f, 10f);
    [SerializeField] private Vector2 _sizeRange = new Vector2(0.04f, 0.12f);
    [SerializeField] private Vector2 _riseSpeedRange = new Vector2(0.05f, 0.25f);
    [SerializeField] private float _swayAmount = 0.3f;
    [SerializeField] private Color _color = new Color(1f, 0.95f, 0.85f, 0.35f);
    [SerializeField] private int _sortingOrder = 50;

    private Transform[] _particles;
    private float[] _riseSpeeds;
    private float[] _swayPhases;
    private float[] _baseX;

    private void Start()
    {
        if (_sprite == null) return;

        _particles = new Transform[_count];
        _riseSpeeds = new float[_count];
        _swayPhases = new float[_count];
        _baseX = new float[_count];

        for (int i = 0; i < _count; i++)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(transform, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sortingOrder = _sortingOrder;

            var color = _color;
            color.a *= Random.Range(0.4f, 1f);
            renderer.color = color;

            float size = Random.Range(_sizeRange.x, _sizeRange.y) / Mathf.Max(0.0001f, _sprite.bounds.size.x);
            go.transform.localScale = Vector3.one * size;

            _baseX[i] = Random.Range(-_area.x, _area.x) * 0.5f;
            go.transform.localPosition = new Vector3(_baseX[i], Random.Range(-_area.y, _area.y) * 0.5f, 0f);

            _riseSpeeds[i] = Random.Range(_riseSpeedRange.x, _riseSpeedRange.y);
            _swayPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            _particles[i] = go.transform;
        }
    }

    private void Update()
    {
        if (_particles == null) return;

        float halfHeight = _area.y * 0.5f;
        for (int i = 0; i < _particles.Length; i++)
        {
            var position = _particles[i].localPosition;
            position.y += _riseSpeeds[i] * Time.deltaTime;
            if (position.y > halfHeight) position.y -= _area.y;

            position.x = _baseX[i] + Mathf.Sin(Time.time * 0.5f + _swayPhases[i]) * _swayAmount;
            _particles[i].localPosition = position;
        }
    }
}
