using UnityEngine;

/// <summary>
/// 子にあるベルト面のスプライトへスクロール用マテリアルを割り当て、ベルトが流れているように見せる。
/// ベルトの向き（回転）はそれぞれのスプライトのローカル下向きに流れるので、斜めのレーンもそのまま使える
/// </summary>
[ExecuteAlways]
public class BeltConveyorScroller : MonoBehaviour
{
    private static readonly int ScrollSpeedId = Shader.PropertyToID("_ScrollSpeed");

    [Header("流す対象のベルト画像")]
    [SerializeField] private Sprite _beltSprite;
    [Header("GameJam/BeltScroll シェーダーのマテリアル")]
    [SerializeField] private Material _scrollMaterial;
    [Header("流れる速さ（ワールド単位/秒）。荷物の移動速度に合わせる")]
    [SerializeField] private float _worldSpeed = 5f;

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Apply()
    {
        if (_beltSprite == null || _scrollMaterial == null) return;

        var block = new MaterialPropertyBlock();
        foreach (var belt in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (belt.sprite != _beltSprite) continue;

            belt.sharedMaterial = _scrollMaterial;

            // テクスチャ1枚分がワールドで何単位になるかで割り、見た目の速さを荷物と揃える
            float length = _beltSprite.bounds.size.y * belt.transform.lossyScale.y;
            belt.GetPropertyBlock(block);
            block.SetFloat(ScrollSpeedId, length > 0f ? _worldSpeed / length : 0f);
            belt.SetPropertyBlock(block);
        }
    }
}
