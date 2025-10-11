using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIWeaponCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public int id;
    private UIWeaponShop weaponShop;

    void Start()
    {
        WeaponConfig weaponConfig = WeaponDic.Instance.weaponDic[id];
        weaponShop = transform.parent.parent.GetComponent<UIWeaponShop>();
        transform.Find("Name").GetComponent<TextMeshProUGUI>().text = weaponConfig.weaponName;
        transform.Find("Price").GetComponent<TextMeshProUGUI>().text = weaponConfig.price.ToString();
    }

    void Update()
    {
        
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {

    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {

    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        weaponShop.PurchaseWeapon(id);
    }
}
