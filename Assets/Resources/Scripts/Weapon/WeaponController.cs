using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public WeaponConfig weaponConfig;
    public GameObject FX;
    public Transform muzzle;

    [HideInInspector] public Transform playerCenter;

    private Camera mainCamera;

    [HideInInspector] public int ammoNum;
    [HideInInspector] public int ammoReserve;

    [HideInInspector] public Animator animator;
    [HideInInspector] public AudioSource audioSource;

    void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        ammoNum = weaponConfig.magazineCapacity;
        ammoReserve = weaponConfig.magazineCapacity * 2;
        mainCamera = Camera.main;
    }

    void Update()
    {
    }

    public void Ready()
    {
    }

    public void Idle()
    { 
    }

    public void MagazineReload()
    {
        animator.Play("Reload");
        audioSource.PlayOneShot(weaponConfig.reloadAudio);
    }

    public void ReloadOpen()
    {
        animator.Play("ReloadOpen");
        audioSource.PlayOneShot(weaponConfig.reloadOpenAudio);
    }
    public void ReloadInsert()
    {
        animator.Play("ReloadInsert");
        audioSource.PlayOneShot(weaponConfig.reloadInsertAudio);
    }
    public void ReloadClose()
    {
        animator.Play("ReloadClose");
        audioSource.PlayOneShot(weaponConfig.reloadCloseAudio);
    }

    public void ReloadDone()
    {
        int reloadNum = weaponConfig.magazineCapacity - ammoNum;
        if (reloadNum < ammoReserve)
        {
            ammoNum += reloadNum;
            ammoReserve -= reloadNum;
        }
        else
        {
            ammoNum += ammoReserve;
            ammoReserve = 0;
        }
    }

    public void Bolt()
    {
        animator.Play("Bolt");
        audioSource.PlayOneShot(weaponConfig.boltAudio);
    }


    public void Fire()
    {
        ammoNum--;
        animator.Play("Fire", 0, 0);
        audioSource.PlayOneShot(weaponConfig.fireAudio);
        StartCoroutine(ShowFX());
        GameObject VFX_Fire = Resources.Load<GameObject>("Prefabs/Impacts/VFX_Fire");
        Instantiate(VFX_Fire, muzzle.position, Quaternion.LookRotation(playerCenter.forward));
    }

    private IEnumerator ShowFX()
    {
        FX.SetActive(true);
        yield return new WaitForSecondsRealtime(0.05f);
        FX.SetActive(false);
    }

    public void SetAmmo(int ammoNum, int ammoReserve)
    {
        this.ammoNum = ammoNum;
        this.ammoReserve = ammoReserve;
    }

    public bool IsFull()
    {
        return ammoNum == weaponConfig.magazineCapacity && ammoReserve == weaponConfig.magazineCapacity * 2;
    }
}
