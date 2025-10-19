using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIModeButton : MonoBehaviour, IPointerClickHandler
{
    public Transform UIGameModes;
    public GameMode gameMode;

    private Color colorBgNormal = new Color(236f / 255f, 232f / 255f, 225f / 255f);
    private Color colorBgSelect = new Color(103f/255f, 116f/255f, 255f/255f);

    private Color colorTextNormal = new Color(15f / 255f, 25f / 255f, 35f / 255f);
    private Color colorTextSelect = new Color(236f / 255f, 232f / 255f, 225f / 255f);

    public void OnPointerClick(PointerEventData eventData)
    {
        UIGameModes.GetComponent<UIGameModes>().SelectMode(gameMode);
    }

    public void SelectEnter()
    {
        GetComponent<Image>().color = colorBgSelect;
        transform.Find("Text").GetComponent<TextMeshProUGUI>().color = colorTextSelect;
    }

    public void SelectExit()
    {
        GetComponent<Image>().color = colorBgNormal;
        transform.Find("Text").GetComponent<TextMeshProUGUI>().color = colorTextNormal;
    }
}
