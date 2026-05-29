using TMPro;
using UnityEngine;

public class UIWeaponShop : MonoBehaviour
{
    private PlayerState playerState;
    private WeaponManager weaponManager;

    private TextMeshProUGUI goldNum;

    private void Start()
    {
        var local = NetworkManager.instance.LocalEntity;
        playerState = local.state;
        weaponManager = local.weapon;
        goldNum = transform.Find("Gold/GoldNum").GetComponent<TextMeshProUGUI>();
        // 进入面板时刷一次（订阅是事件驱动，但首次显示需要立刻有值）
        goldNum.text = playerState.gold.ToString();
    }

    private void OnEnable()
    {
        EventCenter.Subscribe<int>(GameEvents.LocalPlayerGoldChanged, OnGoldChanged);
        // 面板被重新打开时主动同步一次显示
        if (playerState != null && goldNum != null)
            goldNum.text = playerState.gold.ToString();
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<int>(GameEvents.LocalPlayerGoldChanged, OnGoldChanged);
    }

    private void OnGoldChanged(int newGold)
    {
        if (goldNum != null) goldNum.text = newGold.ToString();
    }

    public void PurchaseWeapon(int id)
    {
        WeaponConfig weaponConfig = WeaponDic.instance.weaponDic[id];

        // 已经持有同 id 武器且弹药满 → 无需重复购买
        int slot = weaponConfig.weaponType == WeaponType.MainGun
            ? WeaponManager.SLOT_MAINGUN
            : WeaponManager.SLOT_HANDGUN;
        var current = weaponManager.GetWeaponController(slot);
        if (current != null && current.weaponConfig.id == id && current.IsFull()) return;

        if (playerState.gold > weaponConfig.price)
        {
            var info = new PlayerPurchaseWeapon(NetworkManager.instance.playerName, id);
            NetworkManager.Send(MessageType.PurchaseWeapon, info);
        }
    }
}
