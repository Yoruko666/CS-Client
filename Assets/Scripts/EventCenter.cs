using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventCenter
{
    private static readonly Dictionary<GameEvent, Delegate> _table = new();

    public static void Subscribe<T>(GameEvent eventId, Action<T> callback)
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

    public static void Unsubscribe<T>(GameEvent eventId, Action<T> callback)
    {
        if (callback == null) return;
        if (!_table.TryGetValue(eventId, out var d) || d is not Action<T> typed) return;
        var newDelegate = typed - callback;
        if (newDelegate == null) _table.Remove(eventId);
        else _table[eventId] = newDelegate;
    }

    public static void Invoke<T>(GameEvent eventId, T arg)
    {
        if (!_table.TryGetValue(eventId, out var d) || d == null) return;
        if (d is not Action<T> typed)
        {
            Debug.LogWarning($"[EventCenter] Invoke<{typeof(T).Name}> id={eventId} 类型与订阅者不匹配。");
            return;
        }
        foreach (var del in typed.GetInvocationList())
        {
            try { ((Action<T>)del)(arg); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    public static void Subscribe(GameEvent eventId, Action callback)
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

    public static void Unsubscribe(GameEvent eventId, Action callback)
    {
        if (callback == null) return;
        if (!_table.TryGetValue(eventId, out var d) || d is not Action typed) return;
        var newDelegate = typed - callback;
        if (newDelegate == null) _table.Remove(eventId);
        else _table[eventId] = newDelegate;
    }

    public static void Invoke(GameEvent eventId)
    {
        if (!_table.TryGetValue(eventId, out var d) || d is not Action typed) return;
        foreach (var del in typed.GetInvocationList())
        {
            try { ((Action)del)(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    public static void Clear() => _table.Clear();

    public static int CountListeners(GameEvent eventId)
    {
        if (!_table.TryGetValue(eventId, out var d) || d == null) return 0;
        return d.GetInvocationList().Length;
    }
}
