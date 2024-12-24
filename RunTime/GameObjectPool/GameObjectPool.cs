using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class GameObjectPool
{
    private static GameObjectPool _instance;
    
    public static GameObjectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObjectPool();
            }
            return _instance;
        }
    }

    public ObjectPool<GameObject> pool = new ObjectPool<GameObject>(CreateFunc,OnGet,OnRelease,OnObjDestroy);

    private static void OnObjDestroy(GameObject obj)
    {
        throw new System.NotImplementedException();
    }
    private static void OnRelease(GameObject obj)
    {
        throw new System.NotImplementedException();
    }
    private static void OnGet(GameObject obj)
    {
        throw new System.NotImplementedException();
    }
    private static GameObject CreateFunc()
    {
        throw new System.NotImplementedException();
    }
}
