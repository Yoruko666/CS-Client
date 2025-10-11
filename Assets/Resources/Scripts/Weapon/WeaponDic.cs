using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDic : MonoBehaviour
{
    public WeaponDatabase weaponDatabase;
    [HideInInspector] public List<WeaponConfig> weaponDic = new List<WeaponConfig>();

    public static WeaponDic Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
        weaponDic = weaponDatabase.weaponDatabase;
    }
}
