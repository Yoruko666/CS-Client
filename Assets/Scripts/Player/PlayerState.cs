using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [HideInInspector] public int HP;
    [HideInInspector] public int armor;
    [HideInInspector] public int gold;

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
    }
}
