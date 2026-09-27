using UnityEngine;

public class HPManager : MonoBehaviour
{
    [SerializeField] private int _maxHP = 5;
    private int _hp;
    public int HP => _hp;

    void Start()
    {
        _hp = _maxHP;
    }

    public void TakeDamage()
    {
        _hp -= 1;
        if(_hp <= 0)
        {
            Debug.Log("リザルトに行く");
        }
    }
    



}
