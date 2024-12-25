using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using YooAsset;

namespace GameUI
{
    /// <summary>
    /// 对象池类型
    /// </summary>
    public enum PoolType
    {
        Normal = 0,
        Role,
        UI,
        
        
        Max //不能超过Max
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
        
        public static bool CollectionChecks = true;
        public static int MaxPoolSize = 10;

        private ResourcePackage _package;
        private Dictionary<string, Stack<GameObject>> _pool = new();//回收进池中的对象
        private Dictionary<string, HashSet<GameObject>> _activePool = new();//活跃中的对象池
        private Dictionary<string,AssetHandle> _handleDic = new();//资源句柄
        private Dictionary<int, Transform> _poolTypeDic = new();//对象池类型
        private Dictionary<int,HashSet<string>> _poolTypeNameDic = new();//每个对象池类型的名字，key ---> 对象池类型，value ---> 对象池名字
        


        public void Init()
        {
            GameObject pool = new GameObject("GameObjectPool");
            Object.DontDestroyOnLoad(pool);
            for (int i = 0; i < (int)PoolType.Max; ++i)
            {
                GameObject child = new GameObject(((PoolType) i).ToString());
                child.transform.SetParent(pool.transform);
                _poolTypeDic.Add(i,child.transform);
            }
        }

        public void SetPackage(ResourcePackage package)
        {
            _package = package;
        }

        /// <summary>
        /// 获取并记录活跃对象
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="poolType"></param>
        /// <returns></returns>
        public async UniTask<GameObject> GetObjectAsync(string assetName, PoolType poolType)
        {
            var obj = await LoadObjectAsync(assetName, poolType);
            if (!_activePool.TryGetValue(assetName, out var hashSet))
            {
                hashSet = new HashSet<GameObject>();
                _activePool.Add(assetName, hashSet);
            }
            hashSet.Add(obj);
            return obj;
        }
        
        /// <summary>
        /// 同步获取并记录活跃对象
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="poolType"></param>
        /// <returns></returns>
        public GameObject GetObjectSync(string assetName, PoolType poolType)
        {
            var obj = LoadObjectSync(assetName, poolType);
            if (!_activePool.TryGetValue(assetName, out var hashSet))
            {
                hashSet = new HashSet<GameObject>();
                _activePool.Add(assetName, hashSet);
            }
            hashSet.Add(obj);
            return obj;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private async UniTask<GameObject> LoadObjectAsync(string assetName,PoolType poolType)
        {
            var stack = GetPoolObjectStack(assetName);
            if (stack != null)
            {
                if (stack.Count > 0)
                {
                    var obj = stack.Pop();
                    obj.SetActive(true);
                    return obj;
                }
                return await LoadAssetAsync(assetName);
            }
            stack = new Stack<GameObject>();
            _pool.Add(assetName,stack);

            var nameList = GetPoolTypeNameList(poolType);
            if(nameList != null)
            {
                nameList.Add(assetName);
            }
            else
            {
                nameList = new HashSet<string>();
                nameList.Add(assetName);
                _poolTypeNameDic.Add((int)poolType,nameList);
            }
            
            return await LoadAssetAsync(assetName);
        }
        
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private async UniTask<GameObject> LoadAssetAsync(string assetName)
        {
            InstantiateOperation operation;
            var handle = GetAssetHandle(assetName);
            if (handle != null)
            {
                operation = handle.InstantiateAsync();
            }
            else
            {
                var assetHandle = _package.LoadAssetAsync<GameObject>(assetName);
                _handleDic.Add(assetName, assetHandle);
                operation = assetHandle.InstantiateAsync();
            }
            await operation;
            var obj = operation.Result;
            obj.name = assetName;
            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <returns></returns>
        private GameObject LoadObjectSync(string assetName,PoolType poolType)
        {
            var stack = GetPoolObjectStack(assetName);
            if (stack != null)
            {
                if (stack.Count > 0)
                {
                    var obj = stack.Pop();
                    obj.SetActive(true);
                    return obj;
                }

                return  LoadAssetSync(assetName);
            }
            stack = new Stack<GameObject>();
            _pool.Add(assetName,stack);

            var nameList = GetPoolTypeNameList(poolType);
            if(nameList != null)
            {
                nameList.Add(assetName);
            }
            else
            {
                nameList = new HashSet<string>();
                nameList.Add(assetName);
                _poolTypeNameDic.Add((int)poolType,nameList);
            }
            
            return LoadAssetSync(assetName);
        }
        
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private GameObject LoadAssetSync(string assetName)
        {
            GameObject obj;
            var handle = GetAssetHandle(assetName);
            if (handle != null)
            {
                obj = handle.InstantiateSync();
            }
            else
            {
                var assetHandle = _package.LoadAssetSync<GameObject>(assetName);
                _handleDic.Add(assetName, assetHandle);
                obj = assetHandle.InstantiateSync();
            }
            obj.name = assetName;
            obj.SetActive(true);
            return obj;
        }
        
        public AssetHandle GetAssetHandle(string assetName)
        {
            return _handleDic.GetValueOrDefault(assetName);
        }
        public void ReleaseAssetHandle(string assetName)
        {
            var handle = GetAssetHandle(assetName);
            if (handle != null)
            {
                handle.Release();
            }

            _handleDic.Remove(assetName);
        }
        
        public Transform GetPoolTypeTransform(PoolType type)
        {
            return _poolTypeDic[(int)type];
        }
        
        public HashSet<string> GetPoolTypeNameList(PoolType type)
        {
            return _poolTypeNameDic.GetValueOrDefault((int)type);
        }
        
        public Stack<GameObject> GetPoolObjectStack(string assetName)
        {
            return _pool.GetValueOrDefault(assetName);
        }
        
        public HashSet<GameObject> GetActivePoolObjectList(string assetName)
        {
            return _activePool.GetValueOrDefault(assetName);
        }
        
        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        public void ReleaseObject(GameObject obj,PoolType type)
        {
            var parent = GetPoolTypeTransform(type);
            if (parent != null)
            {
                obj.transform.SetParent(parent,false);
                obj.SetActive(false);
                
                //查找_activePool中对应的GameObject
                if (_activePool.TryGetValue(obj.name, out var hashSet))
                {
                    if (hashSet.Contains(obj))
                    {
                        hashSet.Remove(obj);
                    }
                }
                
                var stack = GetPoolObjectStack(obj.name);
                if(stack != null)
                {
                    stack.Push(obj);
                }
            }
        }
        
        /// <summary>
        /// 删除该类型下所有的对象
        /// </summary>
        /// <param name="type"></param>
        public void DestroyObjectPoolByType(PoolType type)
        {
            var list = GetPoolTypeNameList(type);
            if (list != null)
            {
                foreach (var assetName in list)
                {
                    // 销毁活跃的对象
                    var hashSet = GetActivePoolObjectList(assetName);
                    if (hashSet != null)
                    {
                        foreach (var obj in hashSet)
                        {
                            Object.Destroy(obj);
                        }
                        hashSet.Clear();
                    }
                    
                    // 销毁池中的对象
                    var stack = GetPoolObjectStack(assetName);
                    if (stack != null)
                    {
                        foreach (var obj in stack)
                        {
                            Object.Destroy(obj);
                        }
                        stack.Clear();
                    }
                    // 释放资源句柄
                    ReleaseAssetHandle(assetName);
                    _pool.Remove(assetName);
                    _activePool.Remove(assetName);
                }
                
                // 移除对象池类型中的名字列表
                _poolTypeNameDic.Remove((int)type);
            }
        }
        
        /// <summary>
        /// 销毁对象池所有对象
        /// <summary>
        public void DestroyAllObjectPool()
        {
            foreach (var pool in _pool)
            {
                var assetName = pool.Key;
                // 销毁活跃的对象
                var hashSet = GetActivePoolObjectList(assetName);
                if (hashSet != null)
                {
                    foreach (var obj in hashSet)
                    {
                        Object.Destroy(obj);
                    }
                    hashSet.Clear();
                }
                
                // 销毁池中的对象
                var stack = GetPoolObjectStack(assetName);
                if (stack != null)
                {
                    foreach (var obj in stack)
                    {
                        Object.Destroy(obj);
                    }
                    stack.Clear();
                }
                // 释放资源句柄
                ReleaseAssetHandle(assetName);
            }
            _pool.Clear();
            _activePool.Clear();
            _poolTypeNameDic.Clear();
        }
        
        //-------------测试---------------
        public Dictionary<string,AssetHandle> GetAssetHandleDic()
        {
            return _handleDic;
        }

        public Dictionary<string, Stack<GameObject>> GetPoolDic()
        {
            return _pool;
        }
        
        public Dictionary<string, HashSet<GameObject>> GetActivePoolDic()
        {
            return _activePool;
        }
        
        public Dictionary<int, Transform> GetPoolTypeDic()
        {
            return _poolTypeDic;
        }
        
        public Dictionary<int,HashSet<string>> GetPoolTypeNameDic()
        {
            return _poolTypeNameDic;
        }

        //-------------测试---------------
    }

}
