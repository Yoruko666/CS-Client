using UnityEngine;

public abstract class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
{
    public static T instance;

    /// <summary>
    /// 是否在场景切换时保留实例。默认 true。
    /// 重写返回 false 则单例会随当前场景销毁。
    /// </summary>
    protected virtual bool DontDestroyOnSceneLoad => true;

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = (T)this;
            if (DontDestroyOnSceneLoad) DontDestroyOnLoad(gameObject);
            OnSingletonAwake();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>子类用于额外的初始化逻辑（仅 instance 实例会调用一次）。</summary>
    protected virtual void OnSingletonAwake() { }
}
