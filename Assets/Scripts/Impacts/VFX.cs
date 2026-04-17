using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFX : MonoBehaviour, IPoolable
{
    private float timer;
    public float time;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer > time)
            ObjectPoolManager.Instance.VFXBulletHolePool.Recycle(this);
    }

    public void OnSpawn()
    {
        timer = 0;
    }

    public void OnRecycle()
    {

    }
}
