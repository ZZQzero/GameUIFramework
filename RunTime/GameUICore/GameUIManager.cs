using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameUI;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace GameUI
{
    //ui层级
    public enum EGameUILayer
    {
        Main = 0,
        Normal,
        Popup,
        Tips,
        Loading,
        SystemNotify,//系统提示
        Mask,//UI遮罩层 防止UI在响应中被操作
    }
    
    public enum EGameUIMode
    {
        Normal,                //普通
        HideOther,            //隐藏其他
        ReverseChange,         //反向切换
    }
    
    public class GameUIManager
    {
        private static GameUIManager _instance;
        public static GameUIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameUIManager();
                }
                return _instance;
            }
        }
        
        private GameUIManager()
        {
        }

        private readonly Dictionary<int,Transform> _uiLayerDic = new();
        private readonly Dictionary<string, GameUIBase> _allOpenGameUIDic = new();
        private readonly Dictionary<string,GameUIBase> _allCloseGameUIDic = new();
        private readonly Dictionary<string,AssetHandle> _allAssetHandleDic = new();
        private readonly HashSet<string> _loadingUIHash = new();//正在加载中的UI
        private readonly Dictionary<int,Stack<GameUIBase>> _reverseUIStack = new Dictionary<int,Stack<GameUIBase>>();
        private readonly Dictionary<int,string> _finallyOpenUINameByLayer = new Dictionary<int,string>(); // 按Layer记录最后打开的UI
        private readonly HashSet<string> _notCloseUIFilterSet = new()
        {
            "TipsPanel",
        };
        private readonly List<string> _tempUINameList = new(32); // 复用List减少GC

        private ResourcePackage _package;
        private GameUIRoot _uiRoot;
        
        public void Init()
        {
            if (_uiRoot == null)
            {
                var prefab = Resources.Load<GameObject>("GameUIRoot");
                var root = Object.Instantiate(prefab);
                Object.DontDestroyOnLoad(root);
                _uiRoot = root.GetComponent<GameUIRoot>();
            }
        }
          
        /// <summary>
        /// 打开UI
        /// </summary>
        /// <param name="uiName"></param>
        /// <param name="data"></param>
        public async UniTask OpenUI(string uiName,object data)
        {
            if(_uiRoot == null)
            {
                Debug.LogError("GameUIRoot is null");
                return;
            }

            if (string.IsNullOrEmpty(uiName))
            {
                Debug.LogError("uiName is null");
                return;
            }
            
            if (_package == null)
            {
                _package = YooAssets.GetPackage("DefaultPackage");
            }
            
            if(!_loadingUIHash.Add(uiName))
            {
                Debug.LogError($"{uiName} is loading...");
                return;
            }

            if (_allCloseGameUIDic.TryGetValue(uiName, out var uiBase))
            {
                if (uiBase != null)
                {
                    try
                    {
                        uiBase.Data = data;
                        _allCloseGameUIDic.Remove(uiName);
                        SetUIMode(uiBase);
                        uiBase.OnOpenUI();
                        uiBase.gameObject.SetActive(true);
                        uiBase.transform.SetAsLastSibling();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"OpenUI from close cache failed: {uiName}, Error: {ex}");
                        _allOpenGameUIDic.Remove(uiName);
                        int layer = (int)uiBase.GameUILayer;
                        ClearFinallyOpenUI(layer, uiName);
                    }
                    finally
                    {
                        _loadingUIHash.Remove(uiName);
                    }
                }
                return;
            }

            if(_allOpenGameUIDic.TryGetValue(uiName, out uiBase))
            {
                // UI已经打开，更新Data并刷新
                if (uiBase != null && data != null)
                {
                    uiBase.Data = data;
                    uiBase.OnRefreshUI();
                }
                _loadingUIHash.Remove(uiName);
                return;
            }
            await LoadUI(uiName, data);
        }

        public void RefreshUI(GameUIBase uiBase,object data)
        {
            if (uiBase != null)
            {
                uiBase.Data = data;
                uiBase.OnRefreshUI();
            }
        }
        
        public void RefreshUI(string uiName,object data)
        {
            if(_allOpenGameUIDic.TryGetValue(uiName,out var uiBase))
            {
                if(uiBase != null)
                {
                    uiBase.Data = data;
                    uiBase.OnRefreshUI();    
                }
            }
        }
        
        public void CloseUI(string uiName)
        {
            if (_allOpenGameUIDic.TryGetValue(uiName, out var uiBase))
            {
                if (uiBase == null) return;
                
                int layer = (int)uiBase.GameUILayer;
                ReverseStackPop(uiBase);
                uiBase.OnCloseUI();
                uiBase.transform.SetAsFirstSibling();
                uiBase.gameObject.SetActive(false);
                _allCloseGameUIDic.TryAdd(uiName, uiBase);
                _allOpenGameUIDic.Remove(uiName);

                // 如果关闭的是该Layer最后打开的UI，清理标记
                ClearFinallyOpenUI(layer, uiName);

                if (_allOpenGameUIDic.Count == 0)
                {
                    _finallyOpenUINameByLayer.Clear();
                }
            }
        }
        
        public void CloseAllUI()
        {
            _tempUINameList.Clear();
            foreach (var item in _allOpenGameUIDic)
            {
                // 跳过不关闭的UI
                if (!_notCloseUIFilterSet.Contains(item.Key))
                {
                    _tempUINameList.Add(item.Key);
                }
            }
            
            // 先清理反向切换栈，避免触发返回逻辑
            ClearReverseStack();
            
            // 关闭所有UI
            foreach (var uiName in _tempUINameList)
            {
                CloseUI(uiName);
            }
            _tempUINameList.Clear();
            _finallyOpenUINameByLayer.Clear();
        }
        public void CloseAndDestroyUI(string uiName)
        {
            if (_allOpenGameUIDic.TryGetValue(uiName, out var uiBase))
            {
                if (uiBase == null)
                {
                    return;
                }
                ReverseStackPop(uiBase);       
                int layer = (int)uiBase.GameUILayer;
                uiBase.OnDestroyUI();
                Object.Destroy(uiBase.gameObject);
                ReleaseHandle(uiName);
                _allOpenGameUIDic.Remove(uiName);
                
                // 清理该Layer的最后打开UI标记
                ClearFinallyOpenUI(layer, uiName);
            }
        }
        
        public void CloseAllAndDestroyUI()
        {
            _tempUINameList.Clear();
            foreach (var item in _allOpenGameUIDic)
            {
                _tempUINameList.Add(item.Key);
            }
            
            // 先清理反向切换栈，避免触发返回逻辑
            ClearReverseStack();
            
            // 销毁所有UI，禁用反向切换返回
            foreach (var uiName in _tempUINameList)
            {
                CloseAndDestroyUI(uiName);
            }
            _tempUINameList.Clear();
            _finallyOpenUINameByLayer.Clear();
        }
        
        /// <summary>
        /// 清理已关闭的UI缓存，释放资源句柄
        /// 建议在切换场景或内存紧张时调用
        /// </summary>
        public void ClearClosedUICache()
        {
            _tempUINameList.Clear();
            _tempUINameList.AddRange(_allCloseGameUIDic.Keys);
            foreach (var uiName in _tempUINameList)
            {
                if (_allCloseGameUIDic.TryGetValue(uiName, out var uiBase))
                {
                    if (uiBase != null)
                    {
                        uiBase.OnDestroyUI();
                        Object.Destroy(uiBase.gameObject);
                    }
                    ReleaseHandle(uiName);
                    _allCloseGameUIDic.Remove(uiName);
                }
            }
            _tempUINameList.Clear();
        }
        
        public GameUIBase GetOpenUI(string uiName)
        {
            return _allOpenGameUIDic.GetValueOrDefault(uiName);
        }
        
        public Camera GetUICamera()
        {
            if (_uiRoot != null)
            {
                return _uiRoot.UICamera();
            }

            return null;
        }
        
        public void SetPackage(ResourcePackage package)
        {
            _package = package;
        }

        public void AddNotCloseFilter(string uiName)
        {
            _notCloseUIFilterSet.Add(uiName);
        }
        
        public void RemoveNotCloseFilter(string uiName)
        {
            _notCloseUIFilterSet.Remove(uiName);
        }
        
        public bool IsInNotCloseFilter(string uiName)
        {
            return _notCloseUIFilterSet.Contains(uiName);
        }

        public void SetUICameraToStack()
        {
            //使用URP相机时，需要将UI相机加入到相机堆栈中
            //UniversalAdditionalCameraData additionalCameraData = Camera.main.GetUniversalAdditionalCameraData();
            //if(!additionalCameraData.cameraStack.Contains(_uiRoot.UICamera)) 
            //{
            //    additionalCameraData.cameraStack.Add(_uiRoot.UICamera);
            //}
        }
        public void AddUILayer(int layer,Transform transform)
        {
            _uiLayerDic.TryAdd(layer, transform);
        }
        
        public Transform GetUILayer(EGameUILayer layer)
        {
            return _uiLayerDic.GetValueOrDefault((int)layer);
        }

        private async UniTask LoadUI(string uiName,object data)
        {
            if (_package == null)
            {
                _loadingUIHash.Remove(uiName);
                Debug.LogError("Package is null");
                return;
            }

            AssetHandle handle = null;
            GameObject panel = null;
            GameUIBase uiBase = null;
            bool addedToOpenDic = false;
            
            try
            {
                handle = _package.LoadAssetAsync(uiName);
                await handle;
                
                // 检查加载状态
                if (handle == null || handle.Status != EOperationStatus.Succeed)
                {
                    Debug.LogError($"LoadUI failed: {uiName}, Status: {handle.Status}");
                    return;
                }
                
                panel = handle.InstantiateSync();
                if (panel == null)
                {
                    Debug.LogError($"InstantiateSync failed: {uiName}");
                    handle.Release();
                    return;
                }
                
                panel.name = uiName;
                panel.SetActive(false);
                uiBase = panel.GetComponent<GameUIBase>();
                if (uiBase == null)
                {
                    Debug.LogError($"GameUIBase component not found: {uiName}");
                    Object.Destroy(panel);
                    handle.Release();
                    return;
                }
                
                // 初始化UI
                uiBase.Data = data;
                uiBase.UIName = uiName;
                uiBase.OnInitUI();
                SetUILayer(uiBase);
                SetUIMode(uiBase);
                addedToOpenDic = true;
                uiBase.OnOpenUI();
                panel.transform.SetAsLastSibling();
                panel.SetActive(true);
                _allAssetHandleDic.TryAdd(uiName, handle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoadUI exception: {uiName}, Error: {ex}");
                
                // 从打开列表移除（如果已添加）
                if (addedToOpenDic && uiBase != null)
                {
                    _allOpenGameUIDic.Remove(uiName);
                    // 清理该Layer的最后打开UI标记
                    int layer = (int)uiBase.GameUILayer;
                    ClearFinallyOpenUI(layer, uiName);
                }
                
                // 清理 GameObject
                if (panel != null)
                {
                    Object.Destroy(panel);
                }
                
                // 清理资源句柄
                if (handle != null)
                {
                    handle.Release();
                }
            }
            finally
            {
                _loadingUIHash.Remove(uiName);
            }
        }
        private void SetUILayer(GameUIBase uiBase)
        {
            var layer = GetUILayer(uiBase.GameUILayer);
            if (layer != null)
            {
                uiBase.transform.SetParent(layer, false);
                uiBase.transform.localPosition = Vector3.zero;
                uiBase.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogError("GameUIManager SetUILayer error");
            }
        }

        private void SetUIMode(GameUIBase uiBase)
        {
            switch (uiBase.GameUIMode)
            {
                case EGameUIMode.Normal:
                    break;
                case EGameUIMode.HideOther:
                    CloseAllUI();
                    break;
                case EGameUIMode.ReverseChange:
                    ReverseStackPush(uiBase);
                    break;
            }
            
            // 记录该Layer最后打开的UI
            int layer = (int)uiBase.GameUILayer;
            _finallyOpenUINameByLayer[layer] = uiBase.UIName;
            
            if(!_allOpenGameUIDic.TryAdd(uiBase.UIName, uiBase))
            {
                Debug.LogError($"repeatedly open ui in a frame time: {uiBase.UIName}");
            }
        }

        private void ReverseStackPush(GameUIBase uiBase)
        {
            // 获取该Layer最后打开的UI
            int layer = (int)uiBase.GameUILayer;
            if (_finallyOpenUINameByLayer.TryGetValue(layer, out var lastUIName) && !string.IsNullOrEmpty(lastUIName))
            {
                if (_allOpenGameUIDic.TryGetValue(lastUIName, out var finallyUiBase))
                {
                    // 只有相同Layer且相同模式的UI才压栈
                    if (finallyUiBase.GameUIMode == uiBase.GameUIMode && (int)finallyUiBase.GameUILayer == layer)
                    {
                        finallyUiBase.OnCloseUI();
                        finallyUiBase.transform.SetAsFirstSibling();
                        finallyUiBase.gameObject.SetActive(false);
                        _allCloseGameUIDic.TryAdd(lastUIName, finallyUiBase);
                        _allOpenGameUIDic.Remove(lastUIName);
                        
                        // 压入该Layer的栈
                        if(_reverseUIStack.TryGetValue(layer, out var stack))
                        {
                            stack.Push(finallyUiBase);
                        }
                        else
                        {
                            stack = new Stack<GameUIBase>();
                            stack.Push(finallyUiBase);
                            _reverseUIStack.Add(layer, stack);
                        }
                    }
                }
            }
        }
        
        private void ReverseStackPop(GameUIBase uiBase)
        {
            if(_reverseUIStack.Count == 0)
            { 
                return;
            }
            if (uiBase.GameUIMode == EGameUIMode.ReverseChange)
            {
                int layer = (int)uiBase.GameUILayer;
                if(_reverseUIStack.TryGetValue(layer, out var stack))
                {
                    if (stack.Count > 0)
                    {
                        var prev = stack.Pop();
                        if (prev != null && prev.gameObject != null)
                        {
                            OpenUIWithRollback(prev, stack, layer).Forget();
                        }
                    }
                }
            }
        }
        
        private async UniTaskVoid OpenUIWithRollback(GameUIBase prev, Stack<GameUIBase> stack, int layer)
        {
            try
            {
                await OpenUI(prev.UIName, prev.Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"反向切换打开UI失败: {prev.UIName}, Error: {ex}");
                // 失败时重新压回栈（检查栈是否仍有效）
                if (_reverseUIStack.TryGetValue(layer, out var currentStack) && currentStack == stack && prev != null)
                {
                    stack.Push(prev);
                }
            }
        }
        
        private void ClearReverseStack()
        {
            foreach (var stack in _reverseUIStack.Values)
            {
                stack.Clear();
            }
            _reverseUIStack.Clear();
        }
        
        /// <summary>
        /// 清理指定Layer和UI名称的最后打开记录
        /// </summary>
        /// <param name="layer">UI层级</param>
        /// <param name="uiName">UI名称</param>
        private void ClearFinallyOpenUI(int layer, string uiName)
        {
            if (_finallyOpenUINameByLayer.TryGetValue(layer, out var lastUIName) && lastUIName == uiName)
            {
                _finallyOpenUINameByLayer.Remove(layer);
            }
        }
        
        /// <summary>
        /// 资源句柄释放
        /// </summary>
        /// <param name="uiName"></param>
        private void ReleaseHandle(string uiName)
        {
            if (_allAssetHandleDic.TryGetValue(uiName, out var handle))
            {
                handle.Release();
                _allAssetHandleDic.Remove(uiName);
            }
        }
    }
    
}