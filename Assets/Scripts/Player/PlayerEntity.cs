using UnityEngine;

/// <summary>
/// 玩家组件容器。一次性缓存常用组件，避免热路径上反复 GetComponent。
/// 同时统一本地玩家(FP) / 远程玩家(TP) 的访问入口。
/// 通过工厂方法创建，外部不应该直接 new。
/// </summary>
public class PlayerEntity
{
    public string name;
    public int id;
    public int team;

    public GameObject root;
    public Transform transform;

    // 本地玩家专属（远程玩家为 null）
    public PlayerController fp;
    public PlayerState state;
    public WeaponManager weapon;

    // 远程玩家专属（本地玩家为 null）
    public TPPlayerController tp;
    public TPWeaponManager tpWeapon;

    public bool IsLocal => fp != null;

    /// <summary>包装本地玩家 GameObject。</summary>
    public static PlayerEntity CreateLocal(GameObject go, string name, int id, int team)
    {
        return new PlayerEntity
        {
            name = name,
            id = id,
            team = team,
            root = go,
            transform = go.transform,
            fp = go.GetComponent<PlayerController>(),
            state = go.GetComponent<PlayerState>(),
            weapon = go.GetComponent<WeaponManager>(),
        };
    }

    /// <summary>包装远程玩家 GameObject。</summary>
    public static PlayerEntity CreateRemote(GameObject go, string name, int id, int team)
    {
        return new PlayerEntity
        {
            name = name,
            id = id,
            team = team,
            root = go,
            transform = go.transform,
            tp = go.GetComponent<TPPlayerController>(),
            tpWeapon = go.GetComponent<TPWeaponManager>(),
        };
    }
}
