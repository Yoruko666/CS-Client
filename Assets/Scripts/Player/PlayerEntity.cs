using UnityEngine;

public class PlayerEntity
{
    public int uid;
    public int slot;
    public int team;

    public GameObject root;
    public Transform transform;

    public PlayerController fp;
    public PlayerState state;
    public WeaponManager weapon;

    public TPPlayerController tp;
    public TPWeaponManager tpWeapon;

    public bool IsLocal => fp != null;

    /// <summary>包装本地玩家 GameObject。</summary>
    public static PlayerEntity CreateLocal(GameObject go, int uid, int slot, int team)
    {
        return new PlayerEntity
        {
            uid = uid,
            slot = slot,
            team = team,
            root = go,
            transform = go.transform,
            fp = go.GetComponent<PlayerController>(),
            state = go.GetComponent<PlayerState>(),
            weapon = go.GetComponent<WeaponManager>(),
        };
    }

    /// <summary>包装远程玩家 GameObject。</summary>
    public static PlayerEntity CreateRemote(GameObject go, int uid, int slot, int team)
    {
        return new PlayerEntity
        {
            uid = uid,
            slot = slot,
            team = team,
            root = go,
            transform = go.transform,
            tp = go.GetComponent<TPPlayerController>(),
            tpWeapon = go.GetComponent<TPWeaponManager>(),
        };
    }
}
