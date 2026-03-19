using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDic : MonoBehaviour
{
    public WeaponDatabase weaponDatabase;
    [HideInInspector] public List<WeaponConfig> weaponDic = new();

    public static WeaponDic instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
        weaponDic = weaponDatabase.weaponDatabase;
    }
}
