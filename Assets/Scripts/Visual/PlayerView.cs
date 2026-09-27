using UnityEngine;

/// <summary>
/// 操作盤とプレイヤーの見た目。
/// 分岐を切り替えると、プレイヤーがその色のボタンへ手を伸ばして押し、押したボタンが点灯したままになる。
/// いつ押すかは知らず、RouteInput から Press を呼ばれたときだけ動く。
///
/// 操作盤とプレイヤーの画像は、同じ大きさのキャンバスに描かれた重ね合わせ用の素材なので、
/// ボタンはすべて操作盤と同じ位置に重ね、プレイヤーだけを左右（画像の横方向）に動かして指先をボタンに合わせる
/// </summary>
public class PlayerView : MonoBehaviour
{
    [Header("プレイヤー")]
    [SerializeField] private SpriteRenderer _player;
    [SerializeField] private Sprite _waitSprite;
    [SerializeField] private Sprite _pushSprite;

    [Header("ボタン（左 = 赤 / 中央 = 青 / 右 = 黄）")]
    [SerializeField] private SpriteRenderer _leftButton;
    [SerializeField] private Sprite _leftOff;
    [SerializeField] private Sprite _leftOn;
    [SerializeField] private SpriteRenderer _centerButton;
    [SerializeField] private Sprite _centerOff;
    [SerializeField] private Sprite _centerOn;
    [SerializeField] private SpriteRenderer _rightButton;
    [SerializeField] private Sprite _rightOff;
    [SerializeField] private Sprite _rightOn;

    [Header("ボタンを押すときのプレイヤーの位置（ローカルの x）。待機中は中央のボタンの前にいる")]
    [SerializeField] private float _leftPlayerX = -0.64f;
    [SerializeField] private float _centerPlayerX = -0.07f;
    [SerializeField] private float _rightPlayerX = 0.49f;

    [Header("手を伸ばす速さと、押したまま止まる時間")]
    [SerializeField] private float _moveSpeed = 12f;
    [SerializeField] private float _pushHoldTime = 0.2f;

    private float _targetX;
    private float _holdTimer;

    private void Awake()
    {
        _targetX = _centerPlayerX;
        SetPlayerX(_centerPlayerX);
        _player.sprite = _waitSprite;
        LightButton(null);
    }

    /// <summary>ボタンを押す。Up（不良品を戻す）は対応するボタンが無いので、正面を押して全部消灯する</summary>
    public void Press(RouteType routeType)
    {
        switch (routeType)
        {
            case RouteType.Left:
                _targetX = _leftPlayerX;
                LightButton(_leftButton);
                break;
            case RouteType.Center:
                _targetX = _centerPlayerX;
                LightButton(_centerButton);
                break;
            case RouteType.Right:
                _targetX = _rightPlayerX;
                LightButton(_rightButton);
                break;
            default:
                _targetX = _centerPlayerX;
                LightButton(null);
                break;
        }

        _player.sprite = _pushSprite;
        _holdTimer = _pushHoldTime;
    }

    private void Update()
    {
        if (_holdTimer > 0f)
        {
            _holdTimer -= Time.deltaTime;
            if (_holdTimer <= 0f)
            {
                // 押し終わったら手を戻して、正面で待つ
                _player.sprite = _waitSprite;
                _targetX = _centerPlayerX;
            }
        }

        var position = _player.transform.localPosition;
        position.x = Mathf.Lerp(position.x, _targetX, 1f - Mathf.Exp(-_moveSpeed * Time.deltaTime));
        _player.transform.localPosition = position;
    }

    private void SetPlayerX(float x)
    {
        var position = _player.transform.localPosition;
        position.x = x;
        _player.transform.localPosition = position;
    }

    /// <summary>指定したボタンだけを点灯させる。null なら全部消灯</summary>
    private void LightButton(SpriteRenderer lit)
    {
        _leftButton.sprite = lit == _leftButton ? _leftOn : _leftOff;
        _centerButton.sprite = lit == _centerButton ? _centerOn : _centerOff;
        _rightButton.sprite = lit == _rightButton ? _rightOn : _rightOff;
    }
}
