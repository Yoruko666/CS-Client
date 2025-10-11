using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIWeaponShop : MonoBehaviour
{
    private PlayerState playerState;
    private WeaponManager weaponManager;

    private TextMeshProUGUI goldNum;

    private void Start()
    {
        playerState = NetworkManager.instance.localPlayer.GetComponent<PlayerState>();
        weaponManager = NetworkManager.instance.localPlayer.GetComponent<WeaponManager>();
        goldNum = transform.Find("Gold/GoldNum").GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        goldNum.text = playerState.gold.ToString();
    }

    public void PurchaseWeapon(int id)
    {
        WeaponConfig weaponConfig = WeaponDic.Instance.weaponDic[id];
        if (weaponManager.weaponList.Count > 1)
        {
            WeaponController playerMainGun = weaponManager.weaponList[1].GetComponent<WeaponController>();
            if (weaponConfig.weaponType == WeaponType.MainGun && playerMainGun.weaponConfig.id == weaponConfig.id && playerMainGun.IsFull()) return;
        }
        WeaponController playerHandgun = weaponManager.weaponList[0].GetComponent<WeaponController>();
        if (weaponConfig.weaponType == WeaponType.Handgun && playerHandgun.weaponConfig.id == weaponConfig.id && playerHandgun.IsFull()) return;
        if (playerState.gold > weaponConfig.price)
        {
            PlayerPurchaseWeapon info = new PlayerPurchaseWeapon(NetworkManager.instance.playerName, id);
            NetworkManager.SendMessage(new Message(MessageType.PurchaseWeapon, JsonConvert.SerializeObject(info)));
        }
    }
}
