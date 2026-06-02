using UnityEngine;

/// <summary>
/// 玩家在客户端的统一抽象。本地玩家走 <see cref="LocalPlayerEntity"/>，
/// 远程玩家走 <see cref="RemotePlayerEntity"/>。
/// 拆成两个子类后，调用处不再需要 <c>entity?.tp != null</c> 这种链式 null 判断 ——
/// 持有 <see cref="RemotePlayerEntity"/> 字典的代码就直接拿 <c>tp</c>/<c>tpWeapon</c> 即可。
/// </summary>
public abstract class PlayerEntity
{
    public int uid;
    public int slot;
    public int team;
    public GameObject root;

    protected PlayerEntity(GameObject go, int uid, int slot, int team)
    {
        this.root = go;
        this.uid = uid;
        this.slot = slot;
        this.team = team;
    }
}

/// <summary>本地玩家：第一人称控制 + 客户端预测。</summary>
public class LocalPlayerEntity : PlayerEntity
{
    public PlayerController fp;
    public PlayerState state;
    public WeaponManager weapon;

    public LocalPlayerEntity(GameObject go, int uid, int slot, int team)
        : base(go, uid, slot, team)
    {
        fp = go.GetComponent<PlayerController>();
        state = go.GetComponent<PlayerState>();
        weapon = go.GetComponent<WeaponManager>();
    }
}

/// <summary>远程玩家：第三人称插值表现。</summary>
public class RemotePlayerEntity : PlayerEntity
{
    public TPPlayerController tp;
    public TPWeaponManager tpWeapon;

    public RemotePlayerEntity(GameObject go, int uid, int slot, int team)
        : base(go, uid, slot, team)
    {
        tp = go.GetComponent<TPPlayerController>();
        tpWeapon = go.GetComponent<TPWeaponManager>();
    }
}
