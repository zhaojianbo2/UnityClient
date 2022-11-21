using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeakSingleTon <T>: MonoBehaviour where T: WeakSingleTon<T>
{

    private static WeakReference<T> ins;
    protected WeakSingleTon() => ins = new WeakReference<T>((T)this);
    public static T Instance
    {
        get
        {
            if (ins == null)
            {
                return null;
            }
            ins.TryGetTarget(out T obj);
            return obj;
        }
    }
}
