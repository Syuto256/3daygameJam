using UnityEngine;
using TMPro;

public class HPUI : MonoBehaviour
{
    [SerializeField] private HPManager _hpManager;
    [SerializeField] private TextMeshProUGUI _hpText;
    
    private void Update()
    {
        _hpText.text = "HP:" + _hpManager.HP.ToString();
    }

}
