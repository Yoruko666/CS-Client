using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class TPWeaponManager : MonoBehaviour
{
    public Transform hand;
    private Transform playerCenter;

    private int activeIndex = 0;
    private List<GameObject> weaponList = new();
    private Animator animator;
    private TPPlayerController playerController;

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
        while (nowPosition != endPosition)
        {
            lineRenderer.SetPosition(0, nowPosition);
            nowPosition = Vector3.MoveTowards(nowPosition, endPosition, 1000 * Time.deltaTime);
            yield return null;
        }
        ObjectPoolManager.Instance.VFXFireLinePool.Recycle(fireLine.GetComponent<VFX>());
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
