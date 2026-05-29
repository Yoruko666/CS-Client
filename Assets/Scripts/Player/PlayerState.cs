using UnityEngine;

public class PlayerState : MonoBehaviour
{
    // 属性 setter 中触发事件，确保任何修改路径都会通知 UI。
    // 同时只在数值真的变化时 emit，避免每帧 ApplyPlayerState 都触发。

    private int _hp;
    public int HP
    {
        get => _hp;
        set
        {
            if (_hp == value) return;
            _hp = value;
            EventCenter.Invoke(GameEvents.LocalPlayerHpChanged, _hp);
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
            EventCenter.Invoke(GameEvents.LocalPlayerGoldChanged, _gold);
        }
    }

    [HideInInspector] public int armor;

    private void Start()
    {
        HP = 100;
        gold = 10000;
    }

    public void Cost(int num)
    {
        gold -= num;
    }

    public void ApplyPlayerState(PlayerStateInfo playerState)
    {
        HP = playerState.HP;
        // TODO（P2 共享数据后）：gold 也应该从服务端同步。当前协议未带，先保留客户端预测值。
    }
}

