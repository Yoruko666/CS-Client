using UnityEngine;

/// <summary>
/// 本地玩家状态（HP / gold）。
/// 严格服务端权威：所有变化都来自 ApplyPlayerState，本地不做任何预测扣减。
/// 属性 setter 中触发事件，确保任何修改路径都会通知 UI。
/// </summary>
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
        // 初值占位，开局后会被服务端 ApplyPlayerState 覆盖。
        HP = 100;
        gold = 0;
    }

    public void ApplyPlayerState(PlayerStateInfo playerState)
    {
        HP = playerState.HP;
        gold = playerState.gold;        // 服务端权威（含开局发钱、回合发钱、击杀奖励、购买扣款）
    }
}


