using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MessageType
{
    JoinRoom, Connect, InputInfo, AllPlayersInfo, Fire, Reload, SwitchWeapon, PurchaseWeapon, AcquireWeapon, Die
}

public class Message
{
    public MessageType type;
    public string info;
    public Message(MessageType type, string info)
    {
        this.type = type;
        this.info = info;
    }
}

public class PlayerStateInfo
{
    public string playerName;
    public int tick;
    public float positionX, positionY, positionZ;
    public float rotationX, rotationY;
    public float speed, velocity;
    public float height;
    public bool isCrouch;
    public int HP, armature, gold;
    public WeaponInfo[] weapons = new WeaponInfo[2];
    public int activeWeaponIndex;
    public PlayerStateInfo() { }
    public PlayerStateInfo(string playerName, Vector3 position, float rotationY, float rotationX, float speed, float velocity, float height, bool isCrouch)
    {
        this.playerName = playerName;
        positionX = position.x;
        positionY = position.y;
        positionZ = position.z;
        this.rotationY = rotationY;
        this.rotationX = rotationX;
        this.speed = speed;
        this.velocity = velocity;
        this.height = height;
        this.isCrouch = isCrouch;
    }

    public PlayerStateInfo(PlayerStateInfo playerStateInfo)
    {
        playerName = playerStateInfo.playerName;
        positionX = playerStateInfo.positionX;
        positionY = playerStateInfo.positionY;
        positionZ = playerStateInfo.positionZ;
        rotationY = playerStateInfo.rotationY;
        rotationX = playerStateInfo.rotationX;
        speed = playerStateInfo.speed;
        velocity = playerStateInfo.velocity;
        height = playerStateInfo.height;
        isCrouch = playerStateInfo.isCrouch;
    }

    public Vector3 GetPosition()
    {
        return new Vector3(positionX, positionY, positionZ);
    }
}

public class PlayerInputInfo
{
    public string playerName;
    public int tick;
    public float moveInputX, moveInputY;
    public float lookInputX, lookInputY;
    public bool jump;
    public bool isWalk;
    public bool isCrouch;
    public PlayerInputInfo(string playerName, float moveInputX, float moveInputY, float lookInputX, float lookInputY, bool jump, bool isWalk, bool isCrouch)
    {
        this.playerName = playerName;
        this.moveInputX = moveInputX;
        this.moveInputY = moveInputY;
        this.lookInputX = lookInputX;
        this.lookInputY = lookInputY;
        this.jump = jump;
        this.isWalk = isWalk;
        this.isCrouch = isCrouch;
    }
}

public class PlayerFire
{
    public string playerName;
    public PlayerFire(string playerName)
    {
        this.playerName = playerName;
    }
}

public class PlayerReload
{
    public string playerName; 
    public PlayerReload(string playerName)
    {
        this.playerName = playerName;
    }
}

public class PlayerSwitchWeapon
{
    public string playerName;
    public int index;
    public PlayerSwitchWeapon(string playerName, int index)
    {
        this.playerName = playerName;
        this.index = index;
    }
}

public class PlayerPurchaseWeapon
{
    public string playerName;
    public int id;
    public PlayerPurchaseWeapon(string playerName, int id)
    {
        this.playerName = playerName;
        this.id = id;
    }
}

public class PlayerAcquireWeapon
{
    public string playerName;
    public int id;
    public PlayerAcquireWeapon(string playerName, int id)
    {
        this.playerName = playerName;
        this.id = id;
    }
}

public class PlayerDie
{
    public string playerName;
    public PlayerDie(string playerName)
    {
        this.playerName = playerName;
    }
}