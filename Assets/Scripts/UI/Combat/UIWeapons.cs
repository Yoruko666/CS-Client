using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIWeapons : MonoBehaviour
{
    public GameObject player;

    [Header("武器面板")]
    public GameObject handgunPanel;   // Weapon2 - 副武器
    public GameObject mainGunPanel;   // Weapon1 - 主武器

    private WeaponManager weaponManager;
    private GameObject[] weaponPanels;

    private CanvasGroup canvasGroup;

    private bool faded = false;
    private int preIndex = 0;
    private float switchTime = 3;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        weaponManager = player.GetComponent<WeaponManager>();
        weaponPanels = new GameObject[]
        {
            handgunPanel,   // [0] SLOT_HANDGUN
            mainGunPanel    // [1] SLOT_MAINGUN
        };
    }

    private void Start()
    {
        RefreshAllSlots();
    }

    private void OnEnable()
    {
        EventCenter.Subscribe<int>(GameEvent.WeaponSwitched, OnWeaponSwitched);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<int>(GameEvent.WeaponSwitched, OnWeaponSwitched);
    }

    private void Update()
    {
        // 仅做 fade-out 动画的计时；实际切换/购买都通过事件驱动
        if (faded) return;
        switchTime -= Time.deltaTime;
        if (switchTime <= 0)
        {
            faded = true;
            StartCoroutine(PanelFadeAnim());
        }
    }

    /// <summary>武器切换或购买时刷新对应槽位的图标 + 高亮。</summary>
    private void OnWeaponSwitched(int newIndex)
    {
        // 刷新所有槽位的图标（购买新武器的情况下需要更新图标）
        RefreshAllSlots();

        // 高亮当前槽位
        SetPanelHighlight(preIndex, false);
        SetPanelHighlight(newIndex, true);
        StartCoroutine(SwitchWeaponAnim(weaponPanels[newIndex]));
        canvasGroup.alpha = 1;
        switchTime = 3;
        faded = false;
        preIndex = newIndex;
    }

    private void RefreshAllSlots()
    {
        if (weaponPanels == null) return;
        for (int i = 0; i < WeaponManager.SLOT_COUNT; i++)
        {
            var panel = weaponPanels[i];
            if (panel == null) continue;
            var image = panel.transform.Find("Image").GetComponent<Image>();
            var ctrl = weaponManager.GetWeaponController(i);
            if (ctrl != null)
            {
                image.enabled = true;
                image.sprite = ctrl.weaponConfig.icon;
            }
            else
            {
                image.enabled = false;
            }
        }
    }

    private void SetPanelHighlight(int slot, bool highlighted)
    {
        if (weaponPanels == null || slot < 0 || slot >= weaponPanels.Length || weaponPanels[slot] == null) return;
        var image = weaponPanels[slot].transform.Find("Image").GetComponent<Image>();
        image.color = highlighted
            ? new Color(255, 255, 255, 200f / 255f)
            : new Color(255, 255, 255,  40f / 255f);
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
        while (time >= 0)
        {
            canvasGroup.alpha = time * 2;
            time -= Time.deltaTime;
            yield return null;
        }
    }
}

