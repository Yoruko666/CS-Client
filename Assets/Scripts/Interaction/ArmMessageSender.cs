using UnityEngine;

public class ArmMessageSender : MonoBehaviour
{
    public void ReloadDone()
    {
        GameObject.Find("PlayerRoot").GetComponent<WeaponManager>().activeWeapon.GetComponent<WeaponController>().ReloadDone();
    }
}
