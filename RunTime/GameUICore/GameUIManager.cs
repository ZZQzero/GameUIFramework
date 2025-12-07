using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameUI;
using UnityEngine;
//using UnityEngine.Rendering.Universal;
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
        private readonly List<string> _notCloseUIFilterList = new(5)
        {
            "TipsPanel",
        };
        private string _finallyOpenUIName;

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
                    uiBase.Data = data;
                    uiBase.OnOpenUI();
                    uiBase.gameObject.SetActive(true);
                    uiBase.transform.SetAsLastSibling();
                    _allCloseGameUIDic.Remove(uiName);
                    _allOpenGameUIDic.TryAdd(uiName, uiBase);
                    _loadingUIHash.Remove(uiName);
                    SetUIMode(uiBase);
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
                uiBase.Data = data;
                uiBase.OnRefreshUI();
            }
        }
        
        public void CloseUI(string uiName)
        {
            if (_allOpenGameUIDic.TryGetValue(uiName, out var uiBase))
            {
                ReverseStackPop(uiBase);
                uiBase.OnCloseUI();
                uiBase.transform.SetAsFirstSibling();
                uiBase.gameObject.SetActive(false);
                _allCloseGameUIDic.TryAdd(uiName, uiBase);
                _allOpenGameUIDic.Remove(uiName);
            }

            if (_allOpenGameUIDic.Count == 0)
            {
                _finallyOpenUIName = null;
            }
        }
        
        public void CloseAllUI()
        {
            Dictionary<string,string> hideList = new Dictionary<string,string>();
            foreach (var item in _allOpenGameUIDic)
            {
                hideList.Add(item.Key,item.Key);
            }
            ClearReverseStack();
            foreach (var item in _notCloseUIFilterList)
            {
                hideList.Remove(item);
            }
            
            foreach (var uiName in hideList)
            {
                CloseUI(uiName.Key);
            }
            hideList.Clear();
            _finallyOpenUIName = null;
        }
        public void CloseAndDestroyUI(string uiName)
        {
            if (_allOpenGameUIDic.TryGetValue(uiName, out var uiBase))
            {
                ReverseStackPop(uiBase);
                uiBase.OnDestroyUI();
                Object.Destroy(uiBase.gameObject);
                ReleaseHandle(uiName);
                _allOpenGameUIDic.Remove(uiName);
                if (uiName == _finallyOpenUIName)
                {
                    _finallyOpenUIName = null;
                }
            }
        }
        
        public void CloseAllAndDestroyUI()
        {
            List<string> hideList = new List<string>();
            foreach (var item in _allOpenGameUIDic)
            {
                hideList.Add(item.Key);
            }
            ClearReverseStack();
            foreach (var uiName in hideList)
            {
                CloseAndDestroyUI(uiName);
            }
            hideList.Clear();
            _finallyOpenUIName = null;
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
            if (_package != null)
            {
                var handle = _package.LoadAssetAsync(uiName);
                await handle;
                if (handle == null)
                {
                    _loadingUIHash.Remove(uiName);
                    Debug.LogError($"OpenUI: asset handle null for {uiName}");
                    return;
                }
                _allAssetHandleDic.TryAdd(uiName, handle);
                var panel = handle.InstantiateSync();
                if (panel != null)
                {
                    panel.name = uiName;
                    panel.SetActive(false);
                    var uiBase = panel.GetComponent<GameUIBase>();
                    if (uiBase != null)
                    {
                        uiBase.Data = data;
                        uiBase.UIName = uiName;
                        uiBase.OnInitUI();
                        SetUILayer(uiBase);
                        SetUIMode(uiBase);
                        uiBase.OnOpenUI();
                    }
                    panel.transform.SetAsLastSibling();
                    panel.SetActive(true);
                }
                else
                {
                    Debug.LogError($"panel is null: {uiName}");
                }
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
            
            _finallyOpenUIName = uiBase.UIName;
            if(!_allOpenGameUIDic.TryAdd(uiBase.UIName, uiBase))
            {
                Debug.LogError($"repeatedly open ui in a frame time: {uiBase.UIName}");
            }
        }

        private void ReverseStackPush(GameUIBase uiBase)
        {
            if (!string.IsNullOrEmpty(_finallyOpenUIName))
            {
                if (_allOpenGameUIDic.TryGetValue(_finallyOpenUIName, out var finallyUiBase))
                {
                    if (finallyUiBase.GameUIMode == uiBase.GameUIMode)
                    {
                        finallyUiBase.OnCloseUI();
                        finallyUiBase.transform.SetAsFirstSibling();
                        finallyUiBase.gameObject.SetActive(false);
                        _allCloseGameUIDic.TryAdd(_finallyOpenUIName, finallyUiBase);
                        _allOpenGameUIDic.Remove(_finallyOpenUIName);
                        if(_reverseUIStack.TryGetValue((int)finallyUiBase.GameUILayer,out var stack))
                        {
                            stack.Push(finallyUiBase);
                        }
                        else
                        {
                            stack = new Stack<GameUIBase>();
                            stack.Push(finallyUiBase);
                            _reverseUIStack.Add((int)finallyUiBase.GameUILayer, stack);
                        }
                    }
                }
            }
        }
        
        private void ReverseStackPop(GameUIBase uiBase)
        {
            if (uiBase.GameUIMode == EGameUIMode.ReverseChange)
            {
                if(_reverseUIStack.TryGetValue((int)uiBase.GameUILayer,out var stack))
                {
                    if (stack.Count > 0)
                    {
                        _finallyOpenUIName = null;
                        var prev = stack.Pop();
                        if (prev != null)
                        {
                            OpenUI(prev.UIName, prev.Data).Forget();
                        }
                    }
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
