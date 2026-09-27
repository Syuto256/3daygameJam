using System;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Core.Event;

public class RouteView : MonoBehaviour
{
    [Serializable]
    public struct RouteImageData
    {
        public RouteType routeType;
        [Header("レーンの方向矢印画像")]
        public Sprite arrowSprite;
        [Header("キャラクターのボタン押下画像")]
        public Sprite characterSprite;
    }

    [Header("方向矢印の表示対象 (どちらか片方を割り当て)")]
    [SerializeField] private Image _arrowImage;
    [SerializeField] private SpriteRenderer _arrowSpriteRenderer;

    [Header("キャラクターの表示対象 (どちらか片方を割り当て)")]
    [SerializeField] private Image _characterImage;
    [SerializeField] private SpriteRenderer _characterSpriteRenderer;

    [Header("3組の画像設定 (Left / Center / Right)")]
    [SerializeField] private RouteImageData[] _routeImageSets = new RouteImageData[3];

    [Header("SE設定")]
    [SerializeField] private string _switchSeName = "Switch"; 

    /// <summary>
    /// 入力されたルートに合わせて2箇所の画像を同時に更新する
    /// </summary>
    public void UpdateRouteView(RouteType routeType)
    {
        if (!string.IsNullOrEmpty(_switchSeName))
        {
            EventBus.Publish(new PlaySEEvent(_switchSeName));
        }

        foreach (var data in _routeImageSets)
        {
            if (data.routeType == routeType)
            {
                // --- 矢印画像の更新 ---
                if (data.arrowSprite != null)
                {
                    if (_arrowImage != null)
                    {
                        _arrowImage.sprite = data.arrowSprite;
                    }
                    if (_arrowSpriteRenderer != null)
                    {
                        _arrowSpriteRenderer.sprite = data.arrowSprite;
                    }
                }

                // --- キャラクター画像の更新 ---
                if (data.characterSprite != null)
                {
                    if (_characterImage != null)
                    {
                        _characterImage.sprite = data.characterSprite;
                    }
                    if (_characterSpriteRenderer != null)
                    {
                        _characterSpriteRenderer.sprite = data.characterSprite;
                    }
                }

                break;
            }
        }
    }
}