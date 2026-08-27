using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : class
{
    private readonly List<PoolItem<T>> items = new();
    private readonly Func<T> create;
    private readonly Action<T> reset;
    private readonly Action<T> dispose;
    private readonly int MAX_POOL_SIZE;

    public ObjectPool(Func<T> create, int maxPoolSize, Action<T> reset = null, Action<T> dispose = null)
    {
        this.create = create;
        this.reset = reset;
        this.dispose = dispose;

        this.MAX_POOL_SIZE = maxPoolSize;
    }

    public T Get()
    {
        foreach(var item in items)
        {
            if(!item.InUse)
            {
                item.InUse = true;
                return item.Item;
            }
        }

        var newItem = new PoolItem<T>
        {
            Item = create(),
            InUse = true
        };

        items.Add(newItem);
        return newItem.Item;
    }

    public void Return(T obj)
    {
        foreach(var item in items)
        {
            if (ReferenceEquals(item.Item, obj))
            {
                item.InUse = false;
                reset?.Invoke(obj);
                return;
            }
        }
    }

    public void PreWarm(int count)
    {
        T[] preWarm = new T[count];

        for(int  i = 0; i < count; i++)
        {
           preWarm[i] = Get();
        }

        foreach(var item in preWarm)
        {
            Return(item);
        }
    }

    public void Cleanup()
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];

            if (item.InUse)
                continue;

            dispose?.Invoke(item.Item);
            items.RemoveAt(i);
        }
    }

    public int Count() => items.Count;
}
