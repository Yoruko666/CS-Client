using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPWeaponManager : MonoBehaviour
{
    public Transform hand;
    private Transform playerCenter;

    private int activeIndex = 0;
    private List<GameObject> weaponList = new List<GameObject>();
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerCenter = transform.Find("Center");
        AcquireWeapon(2);
        SwitchWeapon(0);
    }

    void Update()
    {

    }

    public void Fire()
    {
        animator.Play("Fire", 1, 0);
        weaponList[activeIndex].GetComponent<WeaponController>().Fire();
    }

    public void Reload()
    {
        animator.Play("Reload", 1, 0);
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
        GameObject weapon = Instantiate(WeaponDic.Instance.weaponDic[id].weaponPrefab, hand);
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
