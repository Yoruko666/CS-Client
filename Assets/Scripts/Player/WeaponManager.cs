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
    private PlayerController playerController;

    [HideInInspector] public Animator animator;

    private float upTime = 0;
    private float upAngle = 0;
    private float firingTime = 0;

    [HideInInspector] public GameObject activeWeapon;
    [HideInInspector] public int weaponIndex = 0;
    [HideInInspector] public List<GameObject> weaponList = new();
    [HideInInspector] public List<WeaponInfo> weaponInfos = new();
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
        playerController = GetComponent<PlayerController>();
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

    public void HandleTick()
    {
        if (firingTime >= 0)
        {
            firingTime -= NetworkManager.TICK_INTERVAL;
            upTime = Mathf.MoveTowards(upTime, 0.8f, NetworkManager.TICK_INTERVAL);
            upAngle = Mathf.Pow(upTime, 2) * 10;
        }
        else
        {
            upTime = Mathf.MoveTowards(upTime, 0f, 2 * NetworkManager.TICK_INTERVAL);
            upAngle = Mathf.Pow(upTime, 2) * 10;
        }
        upAngle = Mathf.Clamp(upAngle, 0, 6);
        mainCamera.transform.rotation = playerCenter.rotation * Quaternion.Euler(-upAngle, 0, 0);
    }

    public void UpdatePlayerState(ref PlayerStateInfo playerState)
    {
        
    }

    public void Initialize()
    {
        weaponIndex = 0;
        AcquireWeapon(2, 12, 24);
        if (weaponList.Count > 1)
        {
            weaponIndex = 1;
            WeaponConfig weaponConfig = weaponList[1].GetComponent<WeaponController>().weaponConfig;
            AcquireWeapon(weaponConfig.id, weaponConfig.magazineCapacity, weaponConfig.magazineCapacity * 2);
            ApplyWeapon(weaponList[weaponIndex]);
        }
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

    public void Fire()
    {
        firingTime = Mathf.Min(1 / activeWeapon.GetComponent<WeaponController>().weaponConfig.shootSpeed, 1);

        int playerLayer = LayerMask.NameToLayer("Player");
        int CCLayer = LayerMask.NameToLayer("CharacterController");

        PlayerStateInfo state = playerController.currentState;
        Quaternion playerRotation = Quaternion.Euler(0, state.rotationY, 0);
        Quaternion cameraRotation = Quaternion.Euler(state.rotationX, 0, 0);
        Vector3 center = state.GetPosition() + new Vector3(0, state.height, 0);
        Vector3 fireDirection = playerRotation * cameraRotation * Vector3.forward;
        Vector3 startPosition = activeWeapon.GetComponent<WeaponController>().muzzle.position;
        Vector3 endPosition = center + fireDirection * 100f;

        int seed = Random.Range(int.MinValue, int.MaxValue);
        System.Random rand = new(seed);

        float speed = state.speed;
        float max = upTime * 2 + speed;
        float min = -max;

        float verticalOffset = (float)rand.NextDouble() * (max - min) + min - upAngle;
        float horizontalOffset = (float)rand.NextDouble() * (max - min) + min;

        fireDirection = Quaternion.AngleAxis(verticalOffset, playerRotation * Vector3.right) * fireDirection;
        fireDirection = Quaternion.AngleAxis(horizontalOffset, playerRotation * Vector3.up) * fireDirection;

        PlayerFire playerFire = new(NetworkManager.instance.playerName, seed);
        NetworkManager.SendMessage(new Message(MessageType.Fire, JsonConvert.SerializeObject(playerFire)));

        if (Physics.Raycast(center, fireDirection, out RaycastHit hit, 100f, ~(1 << playerLayer | 1 << CCLayer))){
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
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
            endPosition = hit.point;
        }
        StartCoroutine(ShowFireLine(startPosition, endPosition));
    }

    private IEnumerator ShowFireLine(Vector3 startPosition, Vector3 endPosition)
    {
        GameObject VFX_FireLine = Resources.Load<GameObject>("Prefabs/Impacts/VFX_FireLine");
        VFX_FireLine = Instantiate(VFX_FireLine);
        LineRenderer lineRenderer = VFX_FireLine.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        Vector3 nowPosition = startPosition;
        while(nowPosition != endPosition)
        {
            lineRenderer.SetPosition(0, nowPosition);
            nowPosition = Vector3.MoveTowards(nowPosition, endPosition, 1000 * Time.deltaTime);
            yield return null;
        }
        Destroy(VFX_FireLine);
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
        WeaponConfig weaponConfig = WeaponDic.instance.weaponDic[id];
        transform.GetComponent<PlayerState>().Cost(weaponConfig.price);
        AcquireWeapon(id, weaponConfig.magazineCapacity, weaponConfig.magazineCapacity * 2);
    }

    public void AcquireWeapon(int id, int ammoNum, int ammoReserve)
    {
        GameObject weapon = Instantiate(WeaponDic.instance.weaponDic[id].weaponPrefab, hand);
        weapon.GetComponent<WeaponController>().Initialize(NetworkManager.instance.localPlayer.transform);
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

    public void ApplyPlayerState(PlayerStateInfo playerState)
    {

    }
}
