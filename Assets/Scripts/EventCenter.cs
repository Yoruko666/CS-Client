using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局静态事件中心。用于解耦"事件发布者"与"事件订阅者"。
///
/// API：
///   <see cref="Subscribe{T}"/>   订阅
///   <see cref="Unsubscribe{T}"/> 取消订阅
///   <see cref="Invoke{T}"/>      触发
///
/// 使用约定：
/// 1) 事件 ID 在 <see cref="GameEvents"/> 中以 const int 集中定义，避免散落字符串。
/// 2) 订阅者通常在 OnEnable 中 Subscribe，OnDisable 中 Unsubscribe，**严格成对**，
///    避免悬挂引用导致 GameObject 销毁后仍被回调。
/// 3) 一次只有一种参数类型——T 不匹配会在 Invoke 时打 warning 并忽略（不会抛）。
/// 4) 不要把 Invoke 用在 hot path（如 128Hz tick 内每个玩家），事件查表 + 委托调用有极小开销。
///
/// 实现细节：
/// - <see cref="Dictionary{TKey, Delegate}"/>，O(1) 查表。
/// - 同一事件多订阅者用 multicast delegate 串联，调用顺序 = 订阅顺序。
/// - 单个订阅者抛异常不会影响其它订阅者（用 GetInvocationList 隔离调用）。
/// </summary>
public static class EventCenter
{
    private static readonly Dictionary<int, Delegate> _table = new();

    // ============ 带参数的事件 ============

    public static void Subscribe<T>(int eventId, Action<T> callback)
    {
        if (callback == null) return;
        _table.TryGetValue(eventId, out var d);
        if (d != null && d is not Action<T>)
        {
            Debug.LogWarning($"[EventCenter] Subscribe<{typeof(T).Name}> id={eventId} 与已有订阅签名不一致，已忽略。");
            return;
        }
        _table[eventId] = (Action<T>)d + callback;
    }

    public static void Unsubscribe<T>(int eventId, Action<T> callback)
    {
        if (callback == null) return;
        if (!_table.TryGetValue(eventId, out var d) || d is not Action<T> typed) return;
        var newDelegate = typed - callback;
        if (newDelegate == null) _table.Remove(eventId);
        else _table[eventId] = newDelegate;
    }

    public static void Invoke<T>(int eventId, T arg)
    {
        if (!_table.TryGetValue(eventId, out var d) || d == null) return;
        if (d is not Action<T> typed)
        {
            Debug.LogWarning($"[EventCenter] Invoke<{typeof(T).Name}> id={eventId} 类型与订阅者不匹配。");
            return;
        }
        // 用 GetInvocationList 隔离单个订阅者的异常
        foreach (var del in typed.GetInvocationList())
        {
            try { ((Action<T>)del)(arg); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    // ============ 无参数的事件 ============

    public static void Subscribe(int eventId, Action callback)
    {
        if (callback == null) return;
        _table.TryGetValue(eventId, out var d);
        if (d != null && d is not Action)
        {
            Debug.LogWarning($"[EventCenter] Subscribe id={eventId} 与已有订阅签名不一致，已忽略。");
            return;
        }
        _table[eventId] = (Action)d + callback;
    }

    public static void Unsubscribe(int eventId, Action callback)
    {
        if (callback == null) return;
        if (!_table.TryGetValue(eventId, out var d) || d is not Action typed) return;
        var newDelegate = typed - callback;
        if (newDelegate == null) _table.Remove(eventId);
        else _table[eventId] = newDelegate;
    }

    public static void Invoke(int eventId)
    {
        if (!_table.TryGetValue(eventId, out var d) || d is not Action typed) return;
        foreach (var del in typed.GetInvocationList())
        {
            try { ((Action)del)(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    // ============ 调试辅助 ============

    /// <summary>清空所有事件订阅（一般在场景切换或测试时使用）。</summary>
    public static void Clear() => _table.Clear();

    /// <summary>查询某个事件当前有多少订阅者（调试用）。</summary>
    public static int CountListeners(int eventId)
    {
        if (!_table.TryGetValue(eventId, out var d) || d == null) return 0;
        return d.GetInvocationList().Length;
    }
}
