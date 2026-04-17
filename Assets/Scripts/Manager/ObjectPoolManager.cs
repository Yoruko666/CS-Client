using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private const int poolCapacity = 10;
    public ObjectPool<VFX> VFXDirtPool;
    public ObjectPool<VFX> VFXFlamePool;
    public ObjectPool<VFX> VFXHitHeadPool;
    public ObjectPool<VFX> VFXBulletHolePool;
    public ObjectPool<VFX> VFXFireLinePool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        Addressables.LoadAssetAsync<GameObject>("VFX_Dirt").Completed += (obj) =>
        {
            VFXDirtPool = new ObjectPool<VFX>(poolCapacity, obj.Result.GetComponent<VFX>(), transform);
        };
        Addressables.LoadAssetAsync<GameObject>("VFX_Flame").Completed += (obj) =>
        {
            VFXFlamePool = new ObjectPool<VFX>(poolCapacity, obj.Result.GetComponent<VFX>(), transform);
        };
        Addressables.LoadAssetAsync<GameObject>("VFX_HitHead").Completed += (obj) =>
        {
            VFXHitHeadPool = new ObjectPool<VFX>(poolCapacity, obj.Result.GetComponent<VFX>(), transform);
        };
        Addressables.LoadAssetAsync<GameObject>("VFX_BulletHole").Completed += (obj) =>
        {
            VFXBulletHolePool = new ObjectPool<VFX>(poolCapacity, obj.Result.GetComponent<VFX>(), transform);
        };
        Addressables.LoadAssetAsync<GameObject>("VFX_FireLine").Completed += (obj) =>
        {
            VFXFireLinePool = new ObjectPool<VFX>(poolCapacity, obj.Result.GetComponent<VFX>(), transform);
        };
    }
}
