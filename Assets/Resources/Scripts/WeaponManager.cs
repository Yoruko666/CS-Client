using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public Transform arm;
    public Transform hand;
    private Camera mainCamera;
    public Camera mapCamera;
    private Transform playerCenter;

    [HideInInspector] public Animator animator;

    private float upAngle;
    private float recoilUpRate = 10f;
    private float recoilDownRate = 12f;

    [HideInInspector] public GameObject activeWeapon;
    [HideInInspector] public int weaponIndex = 0;
    [HideInInspector] public List<GameObject> weaponList = new List<GameObject>();
    [HideInInspector] public List<WeaponInfo> weaponInfos = new List<WeaponInfo>();
    private FSMController FSM;

    private GameObject BulletHole;
    private GameObject VFX_Dirt;
    private GameObject VFX_Flame;
    private GameObject VFX_HitHead;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        animator = arm.GetComponent<Animator>();
        playerCenter = transform.Find("Center");
        FSM = new FSMController(this);
        AcquireWeapon(2, 12, 24);
        activeWeapon = weaponList[weaponIndex];
        ApplyWeapon(weaponList[weaponIndex]);

        BulletHole = Resources.Load<GameObject>("Prefabs/Impacts/BulletHole");
        VFX_Dirt = Resources.Load<GameObject>("Prefabs/Impacts/VFX_Dirt");
        VFX_Flame = Resources.Load<GameObject>("Prefabs/Impacts/VFX_Flame");
        VFX_HitHead = Resources.Load<GameObject>("Prefabs/Impacts/VFX_HitHead");
    }

    private void Update()
    {
        SwitchWeapon();
        FSM.Update();
    }

    public void SwitchWeapon()
    {
        if (Input.mouseScrollDelta.y < 0)
        {
            if (weaponIndex < weaponList.Count - 1)
            {
                weaponIndex++;
                ApplyWeapon(weaponList[weaponIndex]);
                PlayerSwitchWeapon playerSwitchWeapon = new PlayerSwitchWeapon(NetworkManager.instance.playerName, weaponIndex);
                NetworkManager.SendMessage(new Message(MessageType.SwitchWeapon, JsonConvert.SerializeObject(playerSwitchWeapon)));
            }
        }
        if (Input.mouseScrollDelta.y > 0)
        {
            if (weaponIndex > 0)
            {
                weaponIndex--;
                ApplyWeapon(weaponList[weaponIndex]);
                PlayerSwitchWeapon playerSwitchWeapon = new PlayerSwitchWeapon(NetworkManager.instance.playerName, weaponIndex);
                NetworkManager.SendMessage(new Message(MessageType.SwitchWeapon, JsonConvert.SerializeObject(playerSwitchWeapon)));
            }
        }
        
    }

    public void ApplyWeapon(GameObject weapon)
    { 
        if(activeWeapon)
            activeWeapon.SetActive(false);
        activeWeapon = weapon;
        WeaponController weaponController = weapon.GetComponent<WeaponController>();
        animator.runtimeAnimatorController = weaponController.weaponConfig.FPAnimator;
        FSM.Initialize(weapon);
        activeWeapon.SetActive(true);
    }

    public void RecoilDown()
    {
        upAngle -= recoilDownRate * Time.deltaTime;
        upAngle = Mathf.Clamp(upAngle, 0, 3);
        mainCamera.transform.rotation = playerCenter.rotation * Quaternion.Euler(-upAngle, 0, 0);
    }

    public void Fire()
    {
        PlayerFire playerFire = new PlayerFire(NetworkManager.instance.playerName);
        NetworkManager.SendMessage(new Message(MessageType.Fire, JsonConvert.SerializeObject(playerFire)));
        StartCoroutine(RecoilUp());
        int layer = LayerMask.NameToLayer("CharacterController");
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hit, 100f, ~(1 << layer))){
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                if (hit.collider.GetComponent<BodyCollider>().part == BodyPart.Head)
                    Instantiate(VFX_HitHead, hit.point, Quaternion.identity);
                else
                    Instantiate(VFX_Flame, hit.point, Quaternion.identity);
            }
            else
            {
                Instantiate(VFX_Dirt, hit.point, Quaternion.LookRotation(hit.normal));
                Instantiate(VFX_Flame, hit.point, Quaternion.LookRotation(hit.normal));
                Instantiate(BulletHole, hit.point + hit.normal * 0.0001f, Quaternion.LookRotation(hit.normal));
            }
        }
    }

    private IEnumerator RecoilUp()
    {
        float time = 0;
        while (time < 0.1f)
        {
            upAngle += recoilUpRate * Time.deltaTime;
            upAngle = Mathf.Clamp(upAngle, 0, 3);
            mainCamera.transform.rotation = playerCenter.rotation * Quaternion.Euler(-upAngle, 0, 0);
            time += Time.deltaTime;
            yield return null;
        }
    }

    private Coroutine coroutine = null;
    public void AimEnter(float zoom)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
        coroutine = StartCoroutine(AimEnterHorizon(zoom));
    }
    private IEnumerator AimEnterHorizon(float zoom)
    {
        float time = 0.1f;
        while (time > 0)
        {
            mapCamera.fieldOfView = Mathf.MoveTowards(mapCamera.fieldOfView, 60 / zoom, 20 * Time.deltaTime / 0.1f);
            mainCamera.fieldOfView = Mathf.MoveTowards(mainCamera.fieldOfView, 60 / zoom, 20 * Time.deltaTime / 0.1f);
            time -= Time.deltaTime;
            yield return null;
        }
    }

    public void AimExit()
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
        coroutine = StartCoroutine(AimExitHorizon());
    }
    private IEnumerator AimExitHorizon()
    {
        float time = 0.1f;
        while (time > 0)
        {
            mapCamera.fieldOfView = Mathf.MoveTowards(mapCamera.fieldOfView, 60, 20 * Time.deltaTime / 0.1f);
            mainCamera.fieldOfView = Mathf.MoveTowards(mainCamera.fieldOfView, 60, 20 * Time.deltaTime / 0.1f);
            time -= Time.deltaTime;
            yield return null;
        }
    }

    public void PurchaseWeapon(int id)
    {
        WeaponConfig weaponConfig = WeaponDic.Instance.weaponDic[id];
        transform.GetComponent<PlayerState>().Cost(weaponConfig.price);
        AcquireWeapon(id, weaponConfig.magazineCapacity, weaponConfig.magazineCapacity * 2);
    }

    public void AcquireWeapon(int id, int ammoNum, int ammoReserve)
    {
        GameObject weapon = Instantiate(WeaponDic.Instance.weaponDic[id].weaponPrefab, hand);
        weapon.GetComponent<WeaponController>().playerCenter = playerCenter;
        int layer = LayerMask.NameToLayer("Arm");
        Transform[] children = weapon.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in children)
            t.gameObject.layer = layer;
        weapon.GetComponent<WeaponController>().SetAmmo(ammoNum, ammoReserve);
        if (weapon.GetComponent<WeaponController>().weaponConfig.weaponType == WeaponType.MainGun)
        {
            if (weaponList.Count > 1)
            {
                Destroy(weaponList[1]);
                weaponList[1] = weapon;
            }
            else  weaponList.Add(weapon);
            weaponIndex = 1;
        }
        else
        {
            if(weaponList.Count > 0)
                Destroy(weaponList[0]);
            else weaponList.Add(weapon);
            weaponList[0] = weapon;
        }
        ApplyWeapon(weapon);
    }
}
