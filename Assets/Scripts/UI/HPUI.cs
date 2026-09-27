using UnityEngine;
using UnityEngine.UI;

public class HPUI : MonoBehaviour
{
    [SerializeField] private HPManager _hpManager;
    [SerializeField] private Image[] _hpImage;
    
    private void Update()
    {
        int missCount = _hpImage.Length - _hpManager.HP;
        for(int hpImageIndex = 0;
                hpImageIndex < _hpImage.Length;
                hpImageIndex++)
                {
                    if(hpImageIndex < missCount)
                    {
                        _hpImage[hpImageIndex].gameObject.SetActive(true);
                    }
                    else
                    {
                        _hpImage[hpImageIndex].gameObject.SetActive(false);
                    }
                }
        
    }

}
