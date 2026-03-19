using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIAmmoNum : MonoBehaviour
{
    public GameObject player;

    private TextMeshProUGUI ammoNum;
    private TextMeshProUGUI ammoReserve;
    private WeaponManager weaponManager;

    void Start()
    {
        ammoNum = transform.Find("AmmoNum").GetComponent<TextMeshProUGUI>();
        ammoReserve = transform.Find("AmmoReserve").GetComponent<TextMeshProUGUI>();
        weaponManager = player.GetComponent<WeaponManager>();
    }

    void Update()
    {
        ammoNum.text = weaponManager.activeWeapon.GetComponent<WeaponController>().ammoNum.ToString();
        ammoReserve.text = weaponManager.activeWeapon.GetComponent<WeaponController>().ammoReserve.ToString();
    }
}
