using System.Collections;
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

    /// <summary>武器槽：[0]=Handgun (副武器)，[1]=MainGun (主武器)。</summary>
    public const int SLOT_HANDGUN = 0;
    public const int SLOT_MAINGUN = 1;
    public const int SLOT_COUNT = 2;
    private GameObject[] weapons = new GameObject[SLOT_COUNT];

    [HideInInspector] public GameObject activeWeapon;
    [HideInInspector] public int weaponIndex = 0;
    private FSMController FSM;

    /// <summary>当前持有的武器数量。</summary>
    public int WeaponCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < SLOT_COUNT; i++) if (weapons[i] != null) n++;
            return n;
        }
    }

    /// <summary>按槽位直接获取武器 GameObject，没装备时返回 null。</summary>
    public GameObject GetWeapon(int slot) => (slot >= 0 && slot < SLOT_COUNT) ? weapons[slot] : null;

    /// <summary>对应槽位是否已装备武器。</summary>
    public bool HasWeapon(int slot) => GetWeapon(slot) != null;

    /// <summary>按槽位获取 WeaponController（含 weaponConfig / 弹药等），没装备时返回 null。</summary>
    public WeaponController GetWeaponController(int slot)
    {
        var go = GetWeapon(slot);
        return go != null ? go.GetComponent<WeaponController>() : null;
    }

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
    }

    private void Update()
    {
        HandleSwitchInput();
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
        // 镜头偏移 = 开火上抬（-upAngle）+ 落地踉跄低头（+landKickAngle）
        float pitchOffset = -upAngle + (playerController != null ? playerController.LandKickAngle : 0f);
        mainCamera.transform.rotation = playerCenter.rotation * Quaternion.Euler(pitchOffset, 0, 0);
    }

    public void UpdatePlayerState(ref PlayerStateInfo playerState) { }

    public void Initialize()
    {
        // 副武器复位
        AcquireWeapon(2, 12, 24);
        // 如果之前有主武器，重新刷弹给它
        if (weapons[SLOT_MAINGUN] != null)
        {
            WeaponConfig cfg = weapons[SLOT_MAINGUN].GetComponent<WeaponController>().weaponConfig;
            AcquireWeapon(cfg.id, cfg.magazineCapacity, cfg.magazineCapacity * 2);
        }
    }

    /// <summary>响应鼠标滚轮切换武器。</summary>
    private void HandleSwitchInput()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll == 0) return;

        // 滚轮向下 -> 切到下一个槽位（0->1），向上 -> 切到上一个槽位（1->0）
        int target = scroll < 0 ? SLOT_MAINGUN : SLOT_HANDGUN;
        if (target == weaponIndex || weapons[target] == null) return;

        weaponIndex = target;
        ApplyWeapon(weapons[weaponIndex]);
        var msg = new PlayerSwitchWeapon(NetworkManager.instance.playerName, weaponIndex);
        NetworkManager.Send(MessageType.SwitchWeapon, msg);
    }

    public void ApplyWeapon(GameObject weapon)
    {
        // 切武器时停掉旧武器相关的所有协程，避免飞线、瞄准协程串到新武器上
        StopAllCoroutines();
        aimCoroutine = null;

        if (activeWeapon != null && activeWeapon != weapon)
            activeWeapon.SetActive(false);
        activeWeapon = weapon;
        WeaponController weaponController = weapon.GetComponent<WeaponController>();
        animator.runtimeAnimatorController = weaponController.weaponConfig.FPAnimator;
        FSM.Initialize(weapon);
        activeWeapon.SetActive(true);

        // 通知 UI 等订阅者：武器槽位发生变化（包括切换 / 拔枪 / 购买）
        EventCenter.Invoke(GameEvents.WeaponSwitched, weaponIndex);
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
        NetworkManager.Send(MessageType.Fire, playerFire);

        if (Physics.Raycast(center, fireDirection, out RaycastHit hit, 100f, ~(1 << playerLayer | 1 << CCLayer)))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                if (hit.collider.GetComponent<BodyCollider>().part == BodyPart.Head)
                {
                    VFX hitHead = ObjectPoolManager.Instance.Spawn(VFXType.HitHead);
                    hitHead.gameObject.transform.SetPositionAndRotation(hit.point, Quaternion.identity);
                }
                else
                {
                    VFX flame = ObjectPoolManager.Instance.Spawn(VFXType.Flame);
                    flame.gameObject.transform.SetPositionAndRotation(hit.point, Quaternion.identity);
                }
            }
            else
            {
                VFX dirt = ObjectPoolManager.Instance.Spawn(VFXType.Dirt);
                dirt.gameObject.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                VFX flame = ObjectPoolManager.Instance.Spawn(VFXType.Flame);
                flame.gameObject.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                VFX bulletHole = ObjectPoolManager.Instance.Spawn(VFXType.BulletHole);
                bulletHole.gameObject.transform.SetPositionAndRotation(hit.point + hit.normal * 0.0001f, Quaternion.LookRotation(hit.normal));
            }
            endPosition = hit.point;
        }
        StartCoroutine(ShowFireLine(startPosition, endPosition));
    }

    private IEnumerator ShowFireLine(Vector3 startPosition, Vector3 endPosition)
    {
        GameObject fireLine = ObjectPoolManager.Instance.Spawn(VFXType.FireLine).gameObject;
        LineRenderer lineRenderer = fireLine.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        Vector3 nowPosition = startPosition;
        // 安全保护：超过 0.5 秒强制结束（防止协程在异常状态下残留）
        float timeout = 0.5f;
        while (nowPosition != endPosition && timeout > 0)
        {
            lineRenderer.SetPosition(0, nowPosition);
            nowPosition = Vector3.MoveTowards(nowPosition, endPosition, 1000 * Time.deltaTime);
            timeout -= Time.deltaTime;
            yield return null;
        }
        ObjectPoolManager.Instance.Recycle(VFXType.FireLine, fireLine.GetComponent<VFX>());
    }

    private Coroutine aimCoroutine = null;
    public void AimEnter(float zoom)
    {
        if (aimCoroutine != null) StopCoroutine(aimCoroutine);
        aimCoroutine = StartCoroutine(AimEnterHorizon(zoom));
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
        if (aimCoroutine != null) StopCoroutine(aimCoroutine);
        aimCoroutine = StartCoroutine(AimExitHorizon());
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
        GetComponent<PlayerState>().Cost(weaponConfig.price);
        AcquireWeapon(id, weaponConfig.magazineCapacity, weaponConfig.magazineCapacity * 2);
    }

    public void AcquireWeapon(int id, int ammoNum, int ammoReserve)
    {
        var cfg = WeaponDic.instance.weaponDic[id];
        int slot = cfg.weaponType == WeaponType.MainGun ? SLOT_MAINGUN : SLOT_HANDGUN;

        GameObject weapon = Instantiate(cfg.weaponPrefab, hand);
        var ctrl = weapon.GetComponent<WeaponController>();
        ctrl.Initialize(NetworkManager.instance.localPlayer.transform);
        ctrl.playerCenter = playerCenter;
        ctrl.SetAmmo(ammoNum, ammoReserve);

        int armLayer = LayerMask.NameToLayer("Arm");
        foreach (Transform t in weapon.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = armLayer;

        // 替换槽位：旧武器销毁 + 新武器入位
        if (weapons[slot] != null) Destroy(weapons[slot]);
        weapons[slot] = weapon;
        weaponIndex = slot;
        ApplyWeapon(weapon);
    }

    public void ApplyPlayerState(PlayerStateInfo playerState) { }
}
