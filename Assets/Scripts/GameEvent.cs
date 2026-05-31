public enum GameEvent
{
    // ===== 玩家 / 战斗 =====
    PlayerKilled            = 1,    // 参数: PlayerKill
    LocalPlayerHit          = 2,    // 参数: Hit
    LocalPlayerHpChanged    = 3,    // 参数: int (新 HP)
    LocalPlayerGoldChanged  = 4,    // 参数: int (新金币)
    Chat                    = 5,

    // ===== 武器 =====
    WeaponSwitched          = 20,   // 参数: int (slot)
    WeaponFired             = 21,   // 参数: (预留)

    // ===== 比赛 / 回合 =====
    RoundStateChanged       = 40,   // 参数: RoundState
    RoundWon                = 41,   // 参数: int (本方累计得分)
    RoundLost               = 42,   // 参数: int (对方累计得分)

    // ===== 网络 =====
    RttUpdated              = 60,   // 参数: int (RTT ms)
}
