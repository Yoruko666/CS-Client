using UnityEngine;

public class PlayerState : MonoBehaviour
{
    private int _hp;
    public int HP
    {
        get => _hp;
        set
        {
            if (_hp == value) return;
            _hp = value;
            EventCenter.Invoke(GameEvent.LocalPlayerHpChanged, _hp);
        }
    }

    private int _gold;
    public int gold
    {
        get => _gold;
        set
        {
            if (_gold == value) return;
            _gold = value;
            EventCenter.Invoke(GameEvent.LocalPlayerGoldChanged, _gold);
        }
    }

    [HideInInspector] public int armor;

    private void Start()
    {
        HP = 100;
        gold = 0;
    }

    public void ApplyPlayerState(PlayerStateInfo playerState)
    {
        HP = playerState.HP;
        gold = playerState.gold; 
    }
}


