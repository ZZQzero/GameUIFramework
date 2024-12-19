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
        Static = 0,
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

        private Dictionary<int,Transform> _uiLayerDic = new();
        private Dictionary<string, GameUIBase> _allOpenGameUIDic = new();
        private Dictionary<string,GameUIBase> _allCloseGameUIDic = new();
        private Dictionary<string,AssetHandle> _allAssetHandleDic = new();
        private Dictionary<string,string> _loadingUIDic = new();//正在加载中的UI
        private Stack<GameUIBase> _openUIStack = new Stack<GameUIBase>();
        private List<string> _notCloseUIFilterList = new(5)
        {
            "TipsPanel",
        };
        private string _finallyOpenUIName;

        private ResourcePackage _package;
        private GameUIRoot _uiRoot;
        
        public void Init()
        {
            var prefab = Resources.Load<GameObject>("GameUIRoot");
            var root = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(root);
            _uiRoot = root.GetComponent<GameUIRoot>();
        }
    
        public void SetPackage(ResourcePackage package)
        {
            _package = package;
        }
          
        /// <summary>
        /// 打开UI,没有数据时可以这么用GameUIManager.Instance.OpenUI<GameUIBase<object>>("name", null);
        /// </summary>
        /// <param name="uiName"></param>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        public void OpenUI(string uiName,object data)
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
            
            if(!_loadingUIDic.TryAdd(uiName, uiName))
            {
                Debug.LogError($"{uiName} is loading...");
                return;
            }

            GameUIBase uiBase = null;
            if (_allCloseGameUIDic.TryGetValue(uiName, out uiBase))
            {
                if (uiBase != null)
                {
                    uiBase.OnOpenUI();
                    uiBase.gameObject.SetActive(true);
                    uiBase.transform.SetAsLastSibling();
                    _allCloseGameUIDic.Remove(uiName);
                    _loadingUIDic.Remove(uiName);
                    SetUIMode(uiBase);
                }
                return;
            }

            if(_allOpenGameUIDic.TryGetValue(uiName, out uiBase))
            {
                _loadingUIDic.Remove(uiName);
                return;
            }
            LoadUI(uiName, data).Forget();
        }
        
        public void CloseUI(string uiName)
        {
            if (_allOpenGameUIDic.TryGetValue(uiName, out var uiBase))
            {
                ReverseStackPop(uiBase.GameUIMode);
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
            _openUIStack.Clear();

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
                ReverseStackPop(uiBase.GameUIMode);
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
            _openUIStack.Clear();
            foreach (var uiName in hideList)
            {
                CloseAndDestroyUI(uiName);
            }
            hideList.Clear();
            _finallyOpenUIName = null;
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
                _allAssetHandleDic.TryAdd(uiName, handle);
                var panel = handle.InstantiateSync();
                if (panel != null)
                {
                    panel.name = uiName;
                    panel.SetActive(true);
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
                    _loadingUIDic.Remove(uiName);
                }
                else
                {
                    Debug.LogError($"panel is null: {uiName}");
                }
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
                    ReverseStackPush();
                    break;
            }
            
            _finallyOpenUIName = uiBase.UIName;
            if(!_allOpenGameUIDic.TryAdd(uiBase.UIName, uiBase))
            {
                Debug.LogError($"repeatedly open ui in a frame time: {uiBase.UIName}");
            }
        }

        private void ReverseStackPush()
        {
            if (!string.IsNullOrEmpty(_finallyOpenUIName))
            {
                if (_allOpenGameUIDic.TryGetValue(_finallyOpenUIName, out var finallyUiBase))
                {
                    finallyUiBase.OnCloseUI();
                    finallyUiBase.transform.SetAsFirstSibling();
                    finallyUiBase.gameObject.SetActive(false);
                    _allCloseGameUIDic.TryAdd(_finallyOpenUIName, finallyUiBase);
                    _allOpenGameUIDic.Remove(_finallyOpenUIName);
                    _openUIStack.Push(finallyUiBase);
                }
            }
        }
        
        private void ReverseStackPop(EGameUIMode mode)
        {
            if (mode == EGameUIMode.ReverseChange)
            {
                if (_openUIStack.Count > 0)
                {
                    _finallyOpenUIName = null;
                    var prev = _openUIStack.Pop();
                    if (prev != null)
                    {
                        OpenUI(prev.UIName, prev.Data);
                    }
                }
            }
        }
        
        /// <summary>
        /// 资源句柄释放，释放之后如果调用了卸载资源，那么这个资源就不能使用了
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
