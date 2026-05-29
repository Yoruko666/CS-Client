using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// VFX 对象池管理器：按 VFXType 字典索引，避免为每种 VFX 单独写一个字段。
/// 想新增一种 VFX 只要：
///   1) 在 VFXType 加一个枚举值
///   2) 在 vfxAddresses 加一行映射
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private const int poolCapacity = 10;

    // VFXType -> Addressables 地址
    private static readonly Dictionary<VFXType, string> vfxAddresses = new()
    {
        { VFXType.Dirt,       "VFX_Dirt"       },
        { VFXType.Flame,      "VFX_Flame"      },
        { VFXType.HitHead,    "VFX_HitHead"    },
        { VFXType.BulletHole, "VFX_BulletHole" },
        { VFXType.FireLine,   "VFX_FireLine"   },
    };

    private readonly Dictionary<VFXType, ObjectPool<VFX>> pools = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        foreach (var kv in vfxAddresses)
        {
            var type = kv.Key;
            Addressables.LoadAssetAsync<GameObject>(kv.Value).Completed += obj =>
            {
                pools[type] = new ObjectPool<VFX>(poolCapacity, obj.Result.GetComponent<VFX>(), transform);
            };
        }
    }

    /// <summary>取池：未加载完时返回 null。调用方需自行判空。</summary>
    public ObjectPool<VFX> GetPool(VFXType type)
    {
        pools.TryGetValue(type, out var p);
        return p;
    }

    /// <summary>语法糖：从指定池里 Spawn 一个 VFX，并回填 poolType 以便 Recycle。</summary>
    public VFX Spawn(VFXType type)
    {
        if (!pools.TryGetValue(type, out var p)) return null;
        var vfx = p.Spawn();
        if (vfx != null) vfx.poolType = type;
        return vfx;
    }

    /// <summary>回收：根据 vfx 自身记录的 type 回到对应池。</summary>
    public void Recycle(VFXType type, VFX vfx)
    {
        if (pools.TryGetValue(type, out var p)) p.Recycle(vfx);
    }
}

public enum VFXType
{
    Dirt,
    Flame,
    HitHead,
    BulletHole,
    FireLine,
}
