using System.Collections;
using UnityEngine;

public class UIKillInfos : MonoBehaviour
{
    public static UIKillInfos instance;

    private GameObject killInfoCellPrefab;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        killInfoCellPrefab = Resources.Load<GameObject>("Prefabs/UI/KillInfoCell");
    }

    private void OnEnable()
    {
        EventCenter.Subscribe<PlayerKill>(GameEvent.PlayerKilled, OnPlayerKilled);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<PlayerKill>(GameEvent.PlayerKilled, OnPlayerKilled);
    }

    private void OnPlayerKilled(PlayerKill playerKill)
    {
        GameObject killInfoCell = Instantiate(killInfoCellPrefab, transform);
        killInfoCell.transform.Find("KillName").GetComponent<TMPro.TextMeshProUGUI>().text = playerKill.killerUid.ToString();
        killInfoCell.transform.Find("DieName").GetComponent<TMPro.TextMeshProUGUI>().text = playerKill.victimUid.ToString();
        killInfoCell.transform.Find("Weapon").GetComponent<UnityEngine.UI.Image>().sprite = WeaponDic.instance.weaponDic[playerKill.weaponId].icon;
        killInfoCell.transform.Find("ShotHead").gameObject.SetActive(playerKill.shotHead);
        StartCoroutine(RemoveKillInfo(killInfoCell));
    }

    private IEnumerator RemoveKillInfo(GameObject killInfo)
    {
        yield return new WaitForSeconds(3);
        Destroy(killInfo);
    }
}
