using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace GameUI
{
    public enum PoolType
    {
        Stack,
        LinkedList
    }
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
        
        public PoolType PoolType;

        // Collection checks will throw errors if we try to release an item that is already in the pool.
        public static bool CollectionChecks = true;
        public static int MaxPoolSize = 10;
        
        private IObjectPool<GameObject> _pool;

        public IObjectPool<GameObject> Pool
        {
            get
            {
                if (_pool == null)
                {
                    if (PoolType == PoolType.Stack)
                    {
                        _pool = new ObjectPool<GameObject>(CreatePooledItem,OnTakeFromPool,OnReturnedToPool,OnDestroyPoolObject,CollectionChecks,10,MaxPoolSize);
                    }
                    else
                    {
                        _pool = new LinkedPool<GameObject>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, CollectionChecks, MaxPoolSize);
                    }
                }

                return _pool;
            }
        }
        
        
        private static GameObject CreatePooledItem()
        {
            return null;
        }
    
        private static void OnTakeFromPool(GameObject obj)
        {
            throw new System.NotImplementedException();
        }
        private static void OnReturnedToPool(GameObject obj)
        {
            throw new System.NotImplementedException();
        }
        private static void OnDestroyPoolObject(GameObject obj)
        {
            throw new System.NotImplementedException();
        }
        
    }

}
