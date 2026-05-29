/// <summary>
/// 全局事件 ID 常量集中定义。
///
/// 命名规范：
/// - 用动词的过去时（已发生的事实），例如 PlayerKilled / GoldChanged。
/// - 同模块的事件 ID 留出连号空间，方便后续插入。
///
/// 注意：const int 而不是 enum，是为了让事件 ID 可在不同程序集间稳定共享，
/// 不会因为枚举值在中间插入而错位。
/// </summary>
public static class GameEvents
{
    // ===== 玩家 / 战斗 =====
    public const int PlayerKilled       = 1;     // 参数: PlayerKill
    public const int LocalPlayerHit     = 2;     // 参数: Hit
    public const int LocalPlayerHpChanged = 3;   // 参数: int (新 HP)
    public const int LocalPlayerGoldChanged = 4; // 参数: int (新金币)

    // ===== 武器 =====
    public const int WeaponSwitched = 20;        // 参数: int (slot)
    public const int WeaponFired    = 21;        // 参数: (预留)

    // ===== 比赛 / 回合 =====
    public const int RoundStateChanged = 40;     // 参数: RoundState
    public const int RoundWon          = 41;     // 参数: int (本方累计得分)
    public const int RoundLost         = 42;     // 参数: int (对方累计得分)

    // ===== 网络 =====
    public const int RttUpdated = 60;            // 参数: int (RTT ms)
}
