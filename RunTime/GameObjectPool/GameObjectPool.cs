using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using YooAsset;

namespace GameUI
{
    public enum PoolType
    {
        Normal = 0,
        Role,
        UI,
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
        private Dictionary<string, Stack<GameObject>> _pool = new Dictionary<string, Stack<GameObject>>();
        private Dictionary<string,List<AssetHandle>> _handleDic = new();
        private Dictionary<int, Transform> _poolTypeDic = new();


        public void Init()
        {
            GameObject pool = new GameObject("GameObjectPool");
            Object.DontDestroyOnLoad(pool);
            for (int i = 0; i <= (int)PoolType.UI; i++)
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
        /// 异步加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public async UniTask<GameObject> GetObjectAsync(string assetName)
        {
            if (_pool.TryGetValue(assetName, out var stack))
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
            return await LoadAssetAsync(assetName);
        }
        
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private async UniTask<GameObject> LoadAssetAsync(string assetName)
        {
            var handle = _package.LoadAssetAsync<GameObject>(assetName);
            await handle;
            if (_handleDic.TryGetValue(assetName, out var list))
            {
                list.Add(handle);
            }
            else
            {
                list = new List<AssetHandle>();
                list.Add(handle);
                _handleDic.Add(assetName,list);
            }
            var operation = handle.InstantiateAsync();
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
        public GameObject GetObjectSync(string assetName)
        {
            if (_pool.TryGetValue(assetName, out var stack))
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
            return LoadAssetSync(assetName);
        }
        
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private GameObject LoadAssetSync(string assetName)
        {
            var handle = _package.LoadAssetSync<GameObject>(assetName);
            if (_handleDic.TryGetValue(assetName, out var list))
            {
                list.Add(handle);
            }
            else
            {
                list = new List<AssetHandle>();
                list.Add(handle);
                _handleDic.Add(assetName,list);
            }
            var obj = handle.InstantiateSync();
            obj.name = assetName;
            obj.SetActive(true);
            return obj;
        }
        
        public void RecycleObject(GameObject obj,PoolType type)
        {
            if (_poolTypeDic.TryGetValue((int) type, out Transform parent))
            {
                obj.transform.SetParent(parent,false);
                obj.SetActive(false);
                if(_pool.TryGetValue(obj.name,out var stack))
                {
                    stack.Push(obj);
                }
            }
        }
    }

}
