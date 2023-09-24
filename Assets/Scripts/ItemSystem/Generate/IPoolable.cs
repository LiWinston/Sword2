﻿using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public interface IPoolable
{
    public ObjectPool<GameObject> ThisPool { get; set; }
    bool IsExisting { get; set; }

    void SetPool(ObjectPool<GameObject> pool)
    {
        ThisPool = pool;
    }

    public void actionOnGet()
    {
        IsExisting = true;
    }

    public void actionOnRelease()
    {
        IsExisting = false;
    }

    void Release();
}