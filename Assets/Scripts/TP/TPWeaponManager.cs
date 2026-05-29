using System.Collections;
using UnityEngine;

/// <summary>
/// 远程玩家（第三人称）的武器管理器。
/// 负责播放第三人称动画、显示火光特效、画弹道线。
/// 武器槽位与 <see cref="WeaponManager"/> 严格对齐：
///   weapons[0] = Handgun (副武器)
///   weapons[1] = MainGun (主武器)
/// </summary>
public class TPWeaponManager : MonoBehaviour
{
    public Transform hand;
    private Transform playerCenter;

    private GameObject[] weapons = new GameObject[WeaponManager.SLOT_COUNT];
    private int activeIndex = WeaponManager.SLOT_HANDGUN;

    private Animator animator;
    private TPPlayerController playerController;

    public GameObject ActiveWeapon => weapons[activeIndex];

    private void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<TPPlayerController>();
        playerCenter = transform.Find("Center");

        // 默认副武器（id = 2 是 Handgun，与 FP 端的 Initialize 对齐）
        AcquireWeapon(2);
    }

    public void Fire(Vector3 hitPoint)
    {
        var ctrl = weapons[activeIndex]?.GetComponent<WeaponController>();
        if (ctrl == null) return;

        animator.Play("Fire", 1, 0);
        ctrl.Fire();

        Vector3 startPosition = ctrl.muzzle.position;
        Vector3 endPosition = hitPoint;
        StartCoroutine(ShowFireLine(startPosition, endPosition));

        int playerLayer = LayerMask.NameToLayer("Player");
        int CCLayer = LayerMask.NameToLayer("CharacterController");

        if (Physics.Raycast(startPosition, endPosition - startPosition, out RaycastHit hit, 100f, ~(1 << playerLayer | 1 << CCLayer)))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                if (hit.collider.GetComponent<BodyCollider>().part == BodyPart.Head)
                {
                    VFX hitHead = ObjectPoolManager.Instance.VFXHitHeadPool.Spawn();
                    hitHead.gameObject.transform.SetPositionAndRotation(hit.point, Quaternion.identity);
                }
                else
                {
                    VFX flame = ObjectPoolManager.Instance.VFXFlamePool.Spawn();
                    flame.gameObject.transform.SetPositionAndRotation(hit.point, Quaternion.identity);
                }
            }
            else
            {
                VFX dirt = ObjectPoolManager.Instance.VFXDirtPool.Spawn();
                dirt.gameObject.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                VFX flame = ObjectPoolManager.Instance.VFXFlamePool.Spawn();
                flame.gameObject.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                VFX bulletHole = ObjectPoolManager.Instance.VFXBulletHolePool.Spawn();
                bulletHole.gameObject.transform.SetPositionAndRotation(hit.point + hit.normal * 0.0001f, Quaternion.LookRotation(hit.normal));
            }
        }
    }

    private IEnumerator ShowFireLine(Vector3 startPosition, Vector3 endPosition)
    {
        GameObject fireLine = ObjectPoolManager.Instance.VFXFireLinePool.Spawn().gameObject;
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
        ObjectPoolManager.Instance.VFXFireLinePool.Recycle(fireLine.GetComponent<VFX>());
    }

    public void Reload()
    {
        var ctrl = weapons[activeIndex]?.GetComponent<WeaponController>();
        if (ctrl == null) return;
        animator.Play("Reload", 1, 0);
        ctrl.TPReload();
    }

    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= WeaponManager.SLOT_COUNT) return;
        if (weapons[index] == null) return;          // 目标槽位没武器
        if (index == activeIndex && weapons[activeIndex] != null && weapons[activeIndex].activeSelf) return;

        if (weapons[activeIndex] != null) weapons[activeIndex].SetActive(false);
        activeIndex = index;
        weapons[activeIndex].SetActive(true);

        WeaponConfig cfg = weapons[activeIndex].GetComponent<WeaponController>().weaponConfig;
        animator.runtimeAnimatorController = cfg.TPAnimator;
        animator.Play("Reload", 1, 0.5f);
    }

    public void AcquireWeapon(int id)
    {
        var cfg = WeaponDic.instance.weaponDic[id];
        int slot = cfg.weaponType == WeaponType.MainGun
            ? WeaponManager.SLOT_MAINGUN
            : WeaponManager.SLOT_HANDGUN;

        GameObject weapon = Instantiate(cfg.weaponPrefab, hand);
        var ctrl = weapon.GetComponent<WeaponController>();
        ctrl.Initialize(transform);
        ctrl.playerCenter = playerCenter;

        // 替换该槽位
        if (weapons[slot] != null) Destroy(weapons[slot]);
        weapons[slot] = weapon;

        // 拿到新武器立刻切到该槽位（与 FP 端 AcquireWeapon 行为一致）
        SwitchWeapon(slot);
    }
}
