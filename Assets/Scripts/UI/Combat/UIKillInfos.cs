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
        EventCenter.Subscribe<PlayerKill>(GameEvents.PlayerKilled, OnPlayerKilled);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<PlayerKill>(GameEvents.PlayerKilled, OnPlayerKilled);
    }

    private void OnPlayerKilled(PlayerKill k)
    {
        AddKillInofo(k.playerKillName, k.playerDieName, k.weaponId, k.shotHead);
    }

    public void AddKillInofo(string killName, string dieName, int weaponId, bool shotHead)
    {
        GameObject killInfoCell = Instantiate(killInfoCellPrefab, transform);
        killInfoCell.transform.Find("KillName").GetComponent<TMPro.TextMeshProUGUI>().text = killName;
        killInfoCell.transform.Find("DieName").GetComponent<TMPro.TextMeshProUGUI>().text = dieName;
        killInfoCell.transform.Find("Weapon").GetComponent<UnityEngine.UI.Image>().sprite = WeaponDic.instance.weaponDic[weaponId].icon;
        killInfoCell.transform.Find("ShotHead").gameObject.SetActive(shotHead);
        StartCoroutine(RemoveKillInfo(killInfoCell));
    }

    private IEnumerator RemoveKillInfo(GameObject killInfo)
    {
        yield return new WaitForSeconds(3);
        Destroy(killInfo);
    }
}
