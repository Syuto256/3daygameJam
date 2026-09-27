using UnityEngine;

public class HPManager : MonoBehaviour
{
    [SerializeField] private int _maxHP = 5;
    [SerializeField] private HPUI _hpUI;
    private int _hp;
    public int HP => _hp;

    void Start()
    {
        _hp = _maxHP;
        if (_hpUI != null)
        {
            _hpUI.Initialize(_maxHP);
            _hpUI.ShowHP(_hp);
        }
    }

    public void TakeDamage()
    {
        _hp -= 1;
        if (_hpUI != null) _hpUI.ShowHP(_hp);
        if(_hp <= 0)
        {
            Debug.Log("リザルトに行く");
        }
    }
}
