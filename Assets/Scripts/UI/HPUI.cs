using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP（仕分けエラーの残り回数）の表示。
/// ミスするたびにランプが左から1つずつ赤く点き、パネルが揺れる。残り1回になると点いたランプが脈打って危険を知らせる。
/// いつ表示を変えるかは知らず、HPManager から Initialize / ShowMissCount を呼ばれたときだけ動く
/// </summary>
public class HPUI : MonoBehaviour
{
    [Header("ランプを並べる入れ物（HorizontalLayoutGroup を付けておく）")]
    [SerializeField] private RectTransform _lampContainer;
    [SerializeField] private Sprite _lampOffSprite;
    [SerializeField] private Sprite _lampOnSprite;
    [SerializeField] private Vector2 _lampSize = new Vector2(64f, 60f);

    [Header("ミスしたときに揺らすパネル")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _shakeStrength = 10f;
    [SerializeField] private float _shakeDuration = 0.35f;

    private const float PopDuration = 0.35f;

    private readonly List<RectTransform> _lamps = new List<RectTransform>();
    private readonly List<Image> _lampLights = new List<Image>();
    private int _maxHP;
    private int _missCount;
    private int _poppingIndex = -1;
    private float _popElapsed = PopDuration;
    private float _shakeElapsed;
    private Vector2 _panelBasePosition;

    /// <summary>HP の最大値に合わせてランプを並べる</summary>
    public void Initialize(int maxHP)
    {
        _maxHP = maxHP;
        _missCount = 0;
        _shakeElapsed = _shakeDuration;
        if (_panel != null) _panelBasePosition = _panel.anchoredPosition;

        foreach (var lamp in _lamps) Destroy(lamp.gameObject);
        _lamps.Clear();
        _lampLights.Clear();

        for (int i = 0; i < maxHP; i++) CreateLamp(i);
    }

    /// <summary>ミスの回数を表示する。増えていたら、新しく点いたランプを弾ませてパネルを揺らす</summary>
    public void ShowMissCount(int count)
    {
        int missCount = Mathf.Clamp(count, 0, _maxHP);

        if (missCount > _missCount)
        {
            _poppingIndex = missCount - 1;
            _popElapsed = 0f;
            _shakeElapsed = 0f;
        }

        _missCount = missCount;
        for (int i = 0; i < _lampLights.Count; i++)
            _lampLights[i].gameObject.SetActive(i < missCount);
    }

    private void CreateLamp(int index)
    {
        var lamp = new GameObject($"Lamp_{index}", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        lamp.SetParent(_lampContainer, false);
        lamp.sizeDelta = _lampSize;

        var off = lamp.GetComponent<Image>();
        off.sprite = _lampOffSprite;
        off.preserveAspect = true;
        off.raycastTarget = false;

        // 点灯した見た目は、消灯の上に重ねて出し入れする
        var light = new GameObject("On", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        light.SetParent(lamp, false);
        light.anchorMin = Vector2.zero;
        light.anchorMax = Vector2.one;
        light.sizeDelta = Vector2.zero;

        var on = light.GetComponent<Image>();
        on.sprite = _lampOnSprite;
        on.preserveAspect = true;
        on.raycastTarget = false;
        light.gameObject.SetActive(false);

        _lamps.Add(lamp);
        _lampLights.Add(on);
    }

    private void Update()
    {
        UpdatePop();
        UpdateDanger();
        UpdateShake();
    }

    private void UpdatePop()
    {
        if (_poppingIndex < 0 || _poppingIndex >= _lampLights.Count) return;

        _popElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_popElapsed / PopDuration);

        // 大きく点いて、少し行き過ぎてから元の大きさに収まる
        float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.6f * (1f - t);
        _lampLights[_poppingIndex].rectTransform.localScale = Vector3.one * scale;

        if (t >= 1f)
        {
            _lampLights[_poppingIndex].rectTransform.localScale = Vector3.one;
            _poppingIndex = -1;
        }
    }

    /// <summary>残り1回になったら、点いているランプを脈打たせる</summary>
    private void UpdateDanger()
    {
        bool danger = _maxHP > 0 && _maxHP - _missCount == 1;
        float pulse = danger ? 0.75f + Mathf.Sin(Time.time * 10f) * 0.25f : 1f;

        for (int i = 0; i < _lampLights.Count; i++)
        {
            if (i == _poppingIndex) continue;
            _lampLights[i].color = new Color(1f, 1f, 1f, pulse);
        }
    }

    private void UpdateShake()
    {
        if (_panel == null || _shakeElapsed >= _shakeDuration) return;

        _shakeElapsed += Time.deltaTime;
        float fade = 1f - Mathf.Clamp01(_shakeElapsed / _shakeDuration);
        _panel.anchoredPosition = _panelBasePosition + Random.insideUnitCircle * _shakeStrength * fade;

        if (fade <= 0f) _panel.anchoredPosition = _panelBasePosition;
    }
}
