using UnityEngine;
using Scripts.Core.Event;

public class HPManager : MonoBehaviour
{
    [SerializeField] private int _maxHP = 5;
    [SerializeField] private HPUI _hpUI;
    private int _hp;
    public int HP => _hp;

    void Start()
    {
        _hp = 0; 
        if (_hpUI != null)
        {
            _hpUI.Initialize(_maxHP);
            _hpUI.ShowMissCount(_hp);
        }
    }

    public void TakeDamage()
    {
        _hp += 1;
        if (_hpUI != null) _hpUI.ShowMissCount(_hp);

        if (_hp >= 5)
        {
            Debug.Log("ゲームオーバー：リザルトを表示します");
            EventBus.Publish(new GameOverEvent());
        }
    }
}