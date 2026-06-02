using System.Collections.Generic;
using UnityEngine;

public class WeaponDic : SingletonMono<WeaponDic>
{
    public WeaponDatabase weaponDatabase;
    [HideInInspector] public List<WeaponConfig> weaponDic = new();

    protected override void OnSingletonAwake()
    {
        weaponDic = weaponDatabase.weaponDatabase;
    }
}
