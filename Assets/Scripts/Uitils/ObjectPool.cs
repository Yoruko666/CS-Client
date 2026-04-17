using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : MonoBehaviour, IPoolable
{
    private int _capacity;
    private Stack<T> _pool;
    private T _itemPrefab;
    private Transform _root;

    public ObjectPool(int capacity, T itemPrefab, Transform root)
    {
        _capacity = capacity;
        _itemPrefab = itemPrefab;
        _pool = new Stack<T>(capacity);
        _root = root;
        Expand();
    }

    private void Expand()
    {
        for (int i = 0; i < _capacity; i++)
        {
            T item = Object.Instantiate(_itemPrefab, _root);
            item.gameObject.SetActive(false);
            _pool.Push(item);
        }
    }

    public T Spawn()
    {
        if (_pool.Count == 0)
        {
            Expand();
        }
        T item = _pool.Pop();
        item.gameObject.SetActive(true);
        item.OnSpawn();
        return item;
    }

    public void Recycle(T item)
    {
        item.OnRecycle();
        item.gameObject.SetActive(false);
        _pool.Push(item);
    }

    public void Clear()
    {
        while (_pool.Count > 0)
        {
            T item = _pool.Pop();
            Object.Destroy(item.gameObject);
        }
    }
}

public interface IPoolable
{
    public void OnSpawn();
    public void OnRecycle();
}
