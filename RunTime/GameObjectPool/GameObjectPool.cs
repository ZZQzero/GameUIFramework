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
        Effect,
        Max // 不能超过Max
    }
    
    /// <summary>
    /// GameObject对象池管理器
    /// </summary>
    public class GameObjectPool
    {
        private static GameObjectPool _instance;
        public static GameObjectPool Instance => _instance ??= new GameObjectPool();

        /// <summary>
        /// 默认池容量限制
        /// </summary>
        public int DefaultMaxPoolSize = 10;

        private ResourcePackage _package;

        // ===== 核心数据结构 =====
        private readonly Dictionary<(string, PoolType), Stack<GameObject>> _pool = new();
        private readonly Dictionary<(string, PoolType), AssetHandle> _assetHandles = new();
        private readonly Dictionary<(string, PoolType), int> _instanceCount = new();
        private readonly Dictionary<(string, PoolType), int> _maxPoolSizeConfig = new();
        
        // ===== 类型组织结构 =====
        private readonly Dictionary<int, Transform> _poolTypeDic = new();
        private readonly Dictionary<int, HashSet<GameObject>> _activePoolByType = new();
        
        // ===== 延迟回收队列 =====
        private readonly HashSet<GameObject> _pendingReleaseSet = new();
        private readonly List<(GameObject obj, PoolType type)> _pendingReleaseQueue = new();
        private bool _isProcessingRelease = false;

        public void Init()
        {
            var root = new GameObject("GameObjectPool");
            Object.DontDestroyOnLoad(root);
            for (int i = 0; i < (int)PoolType.Max; i++)
            {
                var child = new GameObject(((PoolType)i).ToString());
                child.transform.SetParent(root.transform);
                _poolTypeDic[i] = child.transform;
            }
        }

        public void SetPackage(ResourcePackage package) => _package = package;

        /// <summary>
        /// 配置特定资源的池容量限制
        /// </summary>
        public void SetMaxPoolSize(string assetName, PoolType type, int maxSize)
        {
            _maxPoolSizeConfig[(assetName, type)] = maxSize;
        }

        /// <summary>
        /// 获取特定资源的池容量限制
        /// </summary>
        private int GetMaxPoolSize(string assetName, PoolType type)
        {
            return _maxPoolSizeConfig.TryGetValue((assetName, type), out var size) ? size : DefaultMaxPoolSize;
        }

        /// <summary>
        /// 获取资源句柄（异步）
        /// </summary>
        private AssetHandle GetAssetHandleAsync(string assetName, PoolType type)
        {
            var key = (assetName, type);
            if (_assetHandles.TryGetValue(key, out var handle))
                return handle;

            handle = _package.LoadAssetAsync<GameObject>(assetName);
            _assetHandles[key] = handle;
            _instanceCount[key] = 0;
            return handle;
        }
        
        /// <summary>
        /// 获取资源句柄（同步）
        /// </summary>
        private AssetHandle GetAssetHandleSync(string assetName, PoolType type)
        {
            var key = (assetName, type);
            if (_assetHandles.TryGetValue(key, out var handle))
                return handle;

            handle = _package.LoadAssetSync<GameObject>(assetName);
            _assetHandles[key] = handle;
            _instanceCount[key] = 0;
            return handle;
        }

        /// <summary>
        /// 异步获取对象
        /// </summary>
        public async UniTask<GameObject> GetObjectAsync(string assetName, PoolType poolType)
        {
            var obj = await LoadObjectAsync(assetName, poolType);
            AddToActive(obj, poolType);
            return obj;
        }

        /// <summary>
        /// 同步获取对象
        /// </summary>
        public GameObject GetObjectSync(string assetName, PoolType poolType)
        {
            var obj = LoadObjectSync(assetName, poolType);
            AddToActive(obj, poolType);
            return obj;
        }

        /// <summary>
        /// 添加到活跃池
        /// </summary>
        private void AddToActive(GameObject obj, PoolType type)
        {
            if (obj == null) return;
            
            if (!_activePoolByType.TryGetValue((int)type, out var set))
            {
                set = new HashSet<GameObject>();
                _activePoolByType[(int)type] = set;
            }
            set.Add(obj);
        }

        /// <summary>
        /// 异步加载对象
        /// </summary>
        private async UniTask<GameObject> LoadObjectAsync(string assetName, PoolType poolType)
        {
            var key = (assetName, poolType);
            
            // 1. 热复用：从延迟回收队列中查找
            if (_pendingReleaseSet.Count > 0)
            {
                for (int i = _pendingReleaseQueue.Count - 1; i >= 0; i--)
                {
                    var (obj, type) = _pendingReleaseQueue[i];
                    if (obj != null && obj.name == assetName && type == poolType)
                    {
                        _pendingReleaseQueue.RemoveAt(i);
                        _pendingReleaseSet.Remove(obj);
                        return obj;
                    }
                }
            }

            // 2. 常驻池复用
            if (_pool.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                var obj = stack.Pop();
                obj.SetActive(true);
                return obj;
            }

            // 3. 真正实例化（异步路径）
            var handle = GetAssetHandleAsync(assetName, poolType);
            var op = handle.InstantiateAsync();
            await op;

            var go = op.Result;
            if (go == null)
            {
                Debug.LogError($"[GameObjectPool] 异步实例化失败：{assetName} (PoolType: {poolType})");
                return null;
            }
            
            go.name = assetName;
            go.SetActive(true);

            _instanceCount[key]++;
            return go;
        }

        /// <summary>
        /// 同步加载对象
        /// </summary>
        private GameObject LoadObjectSync(string assetName, PoolType poolType)
        {
            var key = (assetName, poolType);
            
            // 1. 热复用
            if (_pendingReleaseSet.Count > 0)
            {
                for (int i = _pendingReleaseQueue.Count - 1; i >= 0; i--)
                {
                    var (obj, type) = _pendingReleaseQueue[i];
                    if (obj != null && obj.name == assetName && type == poolType)
                    {
                        _pendingReleaseQueue.RemoveAt(i);
                        _pendingReleaseSet.Remove(obj);
                        return obj;
                    }
                }
            }
            
            // 2. 常驻池
            if (_pool.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                var obj = stack.Pop();
                obj.SetActive(true);
                return obj;
            }
            
            // 3. 实例化（同步路径）
            var handle = GetAssetHandleSync(assetName, poolType);
            var go = handle.InstantiateSync();
            
            if (go == null)
            {
                Debug.LogError($"[GameObjectPool] 同步实例化失败：{assetName} (PoolType: {poolType})");
                return null;
            }
            
            go.name = assetName;
            go.SetActive(true);
            _instanceCount[key]++;
            return go;
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public void ReleaseObject(GameObject obj, PoolType type)
        {
            if (obj == null || _pendingReleaseSet.Contains(obj)) 
                return;

            bool shouldDelay = type == PoolType.UI ||
                               UnityEngine.UI.CanvasUpdateRegistry.IsRebuildingLayout() ||
                               UnityEngine.UI.CanvasUpdateRegistry.IsRebuildingGraphics();

            if (shouldDelay)
            {
                _pendingReleaseSet.Add(obj);
                _pendingReleaseQueue.Add((obj, type));
                if (!_isProcessingRelease)
                {
                    _isProcessingRelease = true;
                    ProcessPendingReleaseAsync().Forget();
                }
            }
            else
            {
                DoRelease(obj, type);
            }
        }

        /// <summary>
        /// 实际执行回收操作
        /// </summary>
        private void DoRelease(GameObject obj, PoolType type)
        {
            if (obj == null) return;
            
            // 从活跃池移除
            _activePoolByType.GetValueOrDefault((int)type)?.Remove(obj);
            
            obj.SetActive(false);
            obj.transform.SetParent(_poolTypeDic[(int)type], false);

            var key = (obj.name, type);
            if (!_pool.TryGetValue(key, out var stack))
            {
                stack = new Stack<GameObject>();
                _pool[key] = stack;
            }
            
            stack.Push(obj);

            // 实时裁剪：超量立即销毁
            int maxSize = GetMaxPoolSize(obj.name, type);
            if (stack.Count > maxSize)
            {
                var excess = stack.Pop();
                DestroyInstance(excess.name, type);
                Object.Destroy(excess);
            }
        }

        /// <summary>
        /// 处理延迟回收队列
        /// </summary>
        private async UniTaskVoid ProcessPendingReleaseAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            foreach (var (obj, type) in _pendingReleaseQueue)
            {
                if (obj == null || !_pendingReleaseSet.Contains(obj))
                    continue;

                // 对象仍在活跃池中才执行回收
                if (_activePoolByType.GetValueOrDefault((int)type)?.Contains(obj) == true)
                {
                    DoRelease(obj, type);
                }
            }

            _pendingReleaseQueue.Clear();
            _pendingReleaseSet.Clear();
            _isProcessingRelease = false;
        }

        /// <summary>
        /// 销毁实例并更新引用计数
        /// </summary>
        private void DestroyInstance(string assetName, PoolType type)
        {
            var key = (assetName, type);
            
            if (!_instanceCount.TryGetValue(key, out int count))
                return;

            _instanceCount[key] = --count;

            // 当所有实例都被销毁时，释放资源句柄
            if (count <= 0)
            {
                if (_assetHandles.TryGetValue(key, out var handle))
                {
                    handle.Release();
                    _assetHandles.Remove(key);
                }
                _instanceCount.Remove(key);
                _pool.Remove(key);
            }
        }

        /// <summary>
        /// 回收该类型下所有的活跃对象
        /// </summary>
        public void ReleaseObjectByPoolType(PoolType type)
        {
            var parent = _poolTypeDic.GetValueOrDefault((int)type);
            if (parent == null) return;

            var hashSet = _activePoolByType.GetValueOrDefault((int)type);
            if (hashSet == null || hashSet.Count == 0) return;

            // 从待回收队列中移除该类型的对象
            for (int i = _pendingReleaseQueue.Count - 1; i >= 0; i--)
            {
                var (obj, queueType) = _pendingReleaseQueue[i];
                if (queueType == type && obj != null)
                {
                    _pendingReleaseQueue.RemoveAt(i);
                    _pendingReleaseSet.Remove(obj);
                }
            }

            // 批量回收活跃对象
            foreach (var obj in hashSet)
            {
                if (obj == null) continue;

                obj.SetActive(false);
                obj.transform.SetParent(parent, false);

                var key = (obj.name, type);
                if (!_pool.TryGetValue(key, out var stack))
                {
                    stack = new Stack<GameObject>();
                    _pool[key] = stack;
                }
                
                stack.Push(obj);

                // 裁剪检查
                int maxSize = GetMaxPoolSize(obj.name, type);
                while (stack.Count > maxSize)
                {
                    var excess = stack.Pop();
                    DestroyInstance(obj.name, type);
                    Object.Destroy(excess);
                }
            }

            hashSet.Clear();
            _activePoolByType.Remove((int)type);
        }

        /// <summary>
        /// 销毁该类型下所有的对象
        /// </summary>
        public void DestroyObjectPoolByType(PoolType type)
        {
            // 清理待回收队列
            var activeSet = _activePoolByType.GetValueOrDefault((int)type);
            for (int i = _pendingReleaseQueue.Count - 1; i >= 0; i--)
            {
                var (obj, queueType) = _pendingReleaseQueue[i];
                if (queueType == type)
                {
                    _pendingReleaseQueue.RemoveAt(i);
                    if (obj != null)
                    {
                        _pendingReleaseSet.Remove(obj);
                        activeSet?.Remove(obj);  // 从活跃池移除
                        DestroyInstance(obj.name, type);
                        Object.Destroy(obj);
                    }
                }
            }

            // 销毁剩余的活跃对象
            if (activeSet != null)
            {
                foreach (var obj in activeSet)
                {
                    if (obj == null) continue;
                    
                    DestroyInstance(obj.name, type);
                    Object.Destroy(obj);
                }
                activeSet.Clear();
                _activePoolByType.Remove((int)type);
            }

            // 销毁池中的对象
            var keysToRemove = new List<(string, PoolType)>();
            foreach (var kvp in _pool)
            {
                var (assetName, poolType) = kvp.Key;
                if (poolType == type)
                {
                    foreach (var obj in kvp.Value)
                    {
                        if (obj != null)
                        {
                            DestroyInstance(assetName, poolType);
                            Object.Destroy(obj);
                        }
                    }
                    kvp.Value.Clear();
                    keysToRemove.Add(kvp.Key);
                }
            }

            // 清理空池和资源句柄
            foreach (var key in keysToRemove)
            {
                _pool.Remove(key);
                
                if (_assetHandles.TryGetValue(key, out var handle))
                {
                    handle.Release();
                    _assetHandles.Remove(key);
                }
                _instanceCount.Remove(key);
            }
        }

        /// <summary>
        /// 销毁对象池所有对象
        /// </summary>
        public void DestroyAllObjectPool()
        {
            // 销毁待回收队列中的对象
            foreach (var (obj, _) in _pendingReleaseQueue)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }
            _pendingReleaseQueue.Clear();
            _pendingReleaseSet.Clear();
            _isProcessingRelease = false;

            // 销毁所有活跃对象
            foreach (var kvp in _activePoolByType)
            {
                foreach (var obj in kvp.Value)
                {
                    if (obj != null)
                        Object.Destroy(obj);
                }
                kvp.Value.Clear();
            }
            _activePoolByType.Clear();

            // 销毁池中的对象
            foreach (var kvp in _pool)
            {
                foreach (var obj in kvp.Value)
                {
                    if (obj != null)
                        Object.Destroy(obj);
                }
                kvp.Value.Clear();
            }
            _pool.Clear();

            // 释放所有资源句柄
            foreach (var kvp in _assetHandles)
            {
                kvp.Value?.Release();
            }
            _assetHandles.Clear();
            _instanceCount.Clear();
        }

        // ===== 调试和查询接口 =====
        
        public Dictionary<(string, PoolType), Stack<GameObject>> GetPoolDic() => _pool;
        public Dictionary<int, HashSet<GameObject>> GetActivePoolDic() => _activePoolByType;
        public Dictionary<int, Transform> GetPoolTypeDic() => _poolTypeDic;
        public Dictionary<(string, PoolType), AssetHandle> GetAssetHandleDic() => _assetHandles;
        public Dictionary<(string, PoolType), int> GetInstanceCountDic() => _instanceCount;
        
        /// <summary>
        /// 获取按类型分组的资源名称字典（用于编辑器可视化）
        /// </summary>
        public Dictionary<PoolType, HashSet<string>> GetPoolTypeNameDic()
        {
            var result = new Dictionary<PoolType, HashSet<string>>();
            
            // 从池中收集
            foreach (var ((assetName, type), _) in _pool)
            {
                if (!result.TryGetValue(type, out var set))
                {
                    set = new HashSet<string>();
                    result[type] = set;
                }
                set.Add(assetName);
            }
            
            // 从活跃对象中补充（可能有未回收的对象）
            foreach (var kvp in _activePoolByType)
            {
                var type = (PoolType)kvp.Key;
                if (!result.TryGetValue(type, out var set))
                {
                    set = new HashSet<string>();
                    result[type] = set;
                }
                
                foreach (var obj in kvp.Value)
                {
                    if (obj != null)
                        set.Add(obj.name);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取指定资源的统计信息
        /// </summary>
        public (int pooled, int active, int total) GetStats(string assetName, PoolType type)
        {
            var key = (assetName, type);
            int pooled = _pool.TryGetValue(key, out var stack) ? stack.Count : 0;
            int total = _instanceCount.TryGetValue(key, out var count) ? count : 0;
            int active = total - pooled;
            return (pooled, active, total);
        }
    }
}