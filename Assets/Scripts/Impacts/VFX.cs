using UnityEngine;

public class VFX : MonoBehaviour, IPoolable
{
    private float timer;
    public float time;

    /// <summary>由对象池在 Spawn 时回填，保证 Recycle 回到正确的池。</summary>
    [System.NonSerialized] public VFXType poolType;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer > time)
            ObjectPoolManager.Instance.Recycle(poolType, this);
    }

    public void OnSpawn()
    {
        timer = 0;
    }

    public void OnRecycle()
    {
    }
}
