using System;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("表示対象のUI（2箇所）")]
    [SerializeField] private Image _arrowImage; 
    [SerializeField] private Image _characterImage; 

    [Header("3組の画像設定 (Left / Center / Right)")]
    [SerializeField] private RouteImageData[] _routeImageSets = new RouteImageData[3];

    /// <summary>
    /// 入力されたルートに合わせて2箇所の画像を同時に更新する
    /// </summary>
    public void UpdateRouteView(RouteType routeType)
    {
        foreach (var data in _routeImageSets)
        {
            if (data.routeType == routeType)
            {
                if (_arrowImage != null && data.arrowSprite != null)
                {
                    _arrowImage.sprite = data.arrowSprite;
                }

                if (_characterImage != null && data.characterSprite != null)
                {
                    _characterImage.sprite = data.characterSprite;
                }

                break;
            }
        }
    }
}