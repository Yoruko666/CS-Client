using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;
using UnityEngine.Analytics;

public class TPWeaponManager : MonoBehaviour
{
    public Transform hand;
    private Transform playerCenter;

    private int activeIndex = 0;
    private List<GameObject> weaponList = new();
    private Animator animator;
    private TPPlayerController playerController;

    private GameObject BulletHole;
    private GameObject VFX_Dirt;
    private GameObject VFX_Flame;
    private GameObject VFX_HitHead;

    private void Awake()
    {
        BulletHole = Resources.Load<GameObject>("Prefabs/Impacts/BulletHole");
        VFX_Dirt = Resources.Load<GameObject>("Prefabs/Impacts/VFX_Dirt");
        VFX_Flame = Resources.Load<GameObject>("Prefabs/Impacts/VFX_Flame");
        VFX_HitHead = Resources.Load<GameObject>("Prefabs/Impacts/VFX_HitHead");
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<TPPlayerController>();
        playerCenter = transform.Find("Center");
        AcquireWeapon(2);
        SwitchWeapon(0);
    }

    public void Fire(Vector3 hitPoint)
    {
        animator.Play("Fire", 1, 0);
        weaponList[activeIndex].GetComponent<WeaponController>().Fire();

        Vector3 startPosition = weaponList[activeIndex].GetComponent<WeaponController>().muzzle.position;
        Vector3 endPosition = hitPoint;
        StartCoroutine(ShowFireLine(startPosition, endPosition));

        int playerLayer = LayerMask.NameToLayer("Player");
        int CCLayer = LayerMask.NameToLayer("CharacterController");

        if (Physics.Raycast(startPosition, endPosition - startPosition, out RaycastHit hit, 100f, ~(1 << playerLayer | 1 << CCLayer)))
        {
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
    }

    private IEnumerator ShowFireLine(Vector3 startPosition, Vector3 endPosition)
    {
        GameObject VFX_FireLine = Resources.Load<GameObject>("Prefabs/Impacts/VFX_FireLine");
        VFX_FireLine = Instantiate(VFX_FireLine);
        LineRenderer lineRenderer = VFX_FireLine.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        Vector3 nowPosition = startPosition;
        while (nowPosition != endPosition)
        {
            lineRenderer.SetPosition(0, nowPosition);
            nowPosition = Vector3.MoveTowards(nowPosition, endPosition, 1000 * Time.deltaTime);
            yield return null;
        }
        Destroy(VFX_FireLine);
    }

    public void Reload()
    {
        animator.Play("Reload", 1, 0);
        weaponList[activeIndex].GetComponent<WeaponController>().TPReload();
    }

    public void SwitchWeapon(int index)
    {
        weaponList[activeIndex].SetActive(false);
        activeIndex = index;
        weaponList[activeIndex].SetActive(true);
        WeaponConfig weaponConfig = weaponList[activeIndex].GetComponent<WeaponController>().weaponConfig;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        int state = stateInfo.fullPathHash;
        float stateTime = stateInfo.normalizedTime % 1;
        animator.runtimeAnimatorController = weaponConfig.TPAnimator;
        animator.Play("Reload", 1, 0.5f);
    }

    public void AcquireWeapon(int id)
    {
        GameObject weapon = Instantiate(WeaponDic.instance.weaponDic[id].weaponPrefab, hand);
        weapon.GetComponent<WeaponController>().Initialize(transform);
        weapon.GetComponent<WeaponController>().playerCenter = playerCenter;
        if (weapon.GetComponent<WeaponController>().weaponConfig.weaponType == WeaponType.MainGun)
        {
            if (weaponList.Count > 1)
            {
                Destroy(weaponList[1]);
                weaponList[1] = weapon;
            }
            else weaponList.Add(weapon);
            SwitchWeapon(1);
        }
        else
        {
            if(weaponList.Count > 0)
                Destroy(weaponList[0]);
            else weaponList.Add(weapon);
            weaponList[0] = weapon;
        }
    }
}
