using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
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
        
        
        
        public int MaxPoolSize = 10;
        public int CheckInterval = 1000 * 10;//检测间隔时间(毫秒)
        private ResourcePackage _package;
        private Dictionary<string, Stack<GameObject>> _pool = new();//回收进池中的对象
        private Dictionary<string, HashSet<GameObject>> _activePool = new();//活跃中的对象池
        private Dictionary<string,AssetHandleData> _handleDic = new();//资源句柄
        private Dictionary<int, Transform> _poolTypeDic = new();//对象池类型
        private Dictionary<int,HashSet<string>> _poolTypeNameDic = new();//每个对象池类型的名字，key ---> 对象池类型，value ---> 对象池名字
        
        public class AssetHandleData
        {
            public AssetHandle Handle;
            public int Count;
        }
        
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
            CheckObjectPoolCount().Forget();
        }

        public void SetPackage(ResourcePackage package)
        {
            _package = package;
        }

        /// <summary>
        /// 异步获取并记录活跃对象
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
            AddPoolTypeNameList(assetName,poolType);
            var stack = GetPoolObjectStack(assetName);
            if (stack != null)
            {
                if (stack.Count > 0)
                {
                    var obj = stack.Pop();
                    return obj;
                }
                return await LoadAssetAsync(assetName,poolType);
            }
            stack = new Stack<GameObject>();
            _pool.Add(assetName,stack);
            return await LoadAssetAsync(assetName,poolType);
        }
        
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private async UniTask<GameObject> LoadAssetAsync(string assetName,PoolType poolType)
        {
            InstantiateOperation operation;
            var handleData = GetAssetHandle(assetName);
            if (handleData != null)
            {
                handleData.Count++;
                operation = handleData.Handle.InstantiateAsync();
            }
            else
            {
                var assetHandle = _package.LoadAssetAsync<GameObject>(assetName);
                operation = assetHandle.InstantiateAsync();
                AddAssetHandle(assetName, assetHandle, poolType);
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
            AddPoolTypeNameList(assetName,poolType);
            var stack = GetPoolObjectStack(assetName);
            if (stack != null)
            {
                if (stack.Count > 0)
                {
                    var obj = stack.Pop();
                    obj.SetActive(true);
                    return obj;
                }

                return  LoadAssetSync(assetName,poolType);
            }
            stack = new Stack<GameObject>();
            _pool.Add(assetName,stack);
            return LoadAssetSync(assetName,poolType);
        }
        
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private GameObject LoadAssetSync(string assetName,PoolType poolType)
        {
            GameObject obj;
            var handleData = GetAssetHandle(assetName);
            if (handleData != null)
            {
                handleData.Count++;
                obj = handleData.Handle.InstantiateSync();
            }
            else
            {
                var assetHandle = _package.LoadAssetSync<GameObject>(assetName);
                obj = assetHandle.InstantiateSync();
                AddAssetHandle(assetName,assetHandle,poolType);
            }
            obj.name = assetName;
            obj.SetActive(true);
            return obj;
        }

        private void AddPoolTypeNameList(string assetName,PoolType poolType)
        {
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
        }

        private void AddAssetHandle(string assetName, AssetHandle handle, PoolType type)
        {
            AssetHandleData data = new AssetHandleData();
            data.Count++;
            data.Handle = handle;
            _handleDic.Add(assetName, data);
        }

        private void ReleaseAssetHandle(string assetName)
        {
            var handleData = GetAssetHandle(assetName);
            if (handleData != null)
            {
                handleData.Count--;
                if (handleData.Count <= 0)
                {
                    handleData.Count = 0;
                    handleData.Handle.Release();
                    _handleDic.Remove(assetName);
                }
            }
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
        /// 回收改类型下所有的对象
        /// </summary>
        /// <param name="type"></param>
        public void ReleaseObjectByPoolType(PoolType type)
        {
            var parent = GetPoolTypeTransform(type);
            if (parent != null)
            {
                var nameList = GetPoolTypeNameList(type);
                foreach (var name in nameList)
                {
                    var list = GetActivePoolObjectList(name);
                    var stack = GetPoolObjectStack(name);
                    if (list is { Count: > 0 })
                    {
                        foreach (var item in list)
                        {
                            item.transform.SetParent(parent,false);
                            item.SetActive(false);
                        
                            if(stack != null)
                            {
                                stack.Push(item);
                            }
                        }
                        list.Clear();
                    }
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
                            // 释放资源句柄
                            ReleaseAssetHandle(obj.name);
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
                            // 释放资源句柄
                            ReleaseAssetHandle(obj.name);
                            Object.Destroy(obj);
                        }
                        stack.Clear();
                    }
                    
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
                        // 释放资源句柄
                        ReleaseAssetHandle(obj.name);
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
                        // 释放资源句柄
                        ReleaseAssetHandle(obj.name);
                        Object.Destroy(obj);
                    }
                    stack.Clear();
                }
            }

            _handleDic.Clear();
            _pool.Clear();
            _activePool.Clear();
            _poolTypeNameDic.Clear();
        }

        /// <summary>
        /// 当前对象池中的对象数量超过最大限制时，销毁多余的对象
        /// </summary>
        /// <returns></returns>
        private void DestroyObjectPoolByMaxSize()
        {
            foreach (var pool in _pool)
            {
                if (pool.Value != null)
                {
                    while (pool.Value.Count > MaxPoolSize)
                    {
                        var obj = pool.Value.Pop();
                        ReleaseAssetHandle(obj.name);
                        Object.Destroy(obj);
                    }
                }
            }
        }
        
        /// <summary>
        /// 定时检测对象池中的对象数量，销毁多余的对象
        /// </summary>
        /// <returns></returns>
        private async UniTask CheckObjectPoolCount()
        {
            while (true)
            {
                await UniTask.Delay(CheckInterval);
                DestroyObjectPoolByMaxSize();
            }
        }
        
        public AssetHandleData GetAssetHandle(string assetName)
        {
            return _handleDic.GetValueOrDefault(assetName);
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

        public Dictionary<string,AssetHandleData> GetAssetHandleDic()
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

    }

}
