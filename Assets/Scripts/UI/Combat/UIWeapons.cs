using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIWeapons : MonoBehaviour
{
    public GameObject player;

    private WeaponManager weaponManager;
    private List<GameObject> weaponPanels;

    private CanvasGroup canvasGroup;

    private bool faded = false;
    private int preIndex = 0;
    private float switchTime = 3;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        weaponManager = player.GetComponent<WeaponManager>();
        weaponPanels = new List<GameObject>();
        weaponPanels.Add(transform.Find("Weapon2").gameObject);
        weaponPanels.Add(transform.Find("Weapon1").gameObject);
        for (int i = 0; i < weaponManager.weaponList.Count; i++)
            weaponPanels[i].transform.Find("Image").GetComponent<Image>().sprite = weaponManager.weaponList[i].GetComponent<WeaponController>().weaponConfig.icon;
    }

    void Update()
    {
        if (weaponManager.weaponIndex != preIndex)
        {
            weaponPanels[preIndex].transform.Find("Image").GetComponent<Image>().color = new Color(255, 255, 255, 40f / 255f);
            weaponPanels[weaponManager.weaponIndex].transform.Find("Image").GetComponent<Image>().color = new Color(255, 255, 255, 200f / 255f);
            StartCoroutine(SwitchWeaponAnim(weaponPanels[weaponManager.weaponIndex]));
            canvasGroup.alpha = 1;
            switchTime = 3;
            faded = false;
        }
        else 
        {
            switchTime -= Time.deltaTime;
            if (!faded && switchTime <= 0)
            {
                faded = true;
                StartCoroutine(PanelFadeAnim());
            }
        }
        preIndex = weaponManager.weaponIndex;
    }

    private IEnumerator SwitchWeaponAnim(GameObject weaponPanel)
    {
        RectTransform rectTransform = weaponPanel.GetComponent<RectTransform>();
        float positionY = rectTransform.anchoredPosition.y;
        rectTransform.anchoredPosition = new Vector3(-4, positionY, 0);
        yield return new WaitForSecondsRealtime(0.05f);
        rectTransform.anchoredPosition = new Vector3(0, positionY, 0);
    }

    private IEnumerator PanelFadeAnim()
    {
        float time = 0.5f;
        while(time >= 0)
        {
            canvasGroup.alpha = time * 2;
            time -= Time.deltaTime;
            yield return null;
        }
    }
}
