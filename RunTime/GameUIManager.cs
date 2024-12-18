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
    
        public async UniTask<GameUIBase> OpenUI(string uiName,object data)
        {
            if(_uiRoot == null)
            {
                Debug.LogError("GameUIRoot is null");
                return null;
            }

            if (string.IsNullOrEmpty(uiName))
            {
                Debug.LogError("uiName is null");
                return null;
            }
            
            if (_package == null)
            {
                _package = YooAssets.GetPackage("DefaultPackage");
            }
            
            if(!_loadingUIDic.TryAdd(uiName, uiName))
            {
                Debug.LogError($"{uiName} is loading...");
                return null;
            }

            GameUIBase uiBase = null;
            if (_allCloseGameUIDic.TryGetValue(uiName, out uiBase))
            {
                uiBase.OnOpenUI();
                uiBase.gameObject.SetActive(true);
                uiBase.transform.SetAsLastSibling();
                _allCloseGameUIDic.Remove(uiName);
                _loadingUIDic.Remove(uiName);
                SetUIMode(uiBase);
                Debug.LogError($"打开已关闭的UI {uiName}");
                return uiBase;
            }

            if(_allOpenGameUIDic.TryGetValue(uiName, out uiBase))
            {
                Debug.LogError("重复打开UI");
                if (_loadingUIDic.Remove(uiName))
                {
                    Debug.LogError("移除loadingUI");
                }
                return uiBase;
            }
            
            if (_package != null)
            {
                var handle = _package.LoadAssetAsync(uiName);
                await handle;
                var panel = handle.InstantiateSync();
                panel.name = uiName;
                panel.SetActive(true);
                uiBase = panel.GetComponent<GameUIBase>();
                if (uiBase != null)
                {
                    Debug.LogError($"新建ui {uiName}");
                    uiBase.Data = data;
                    uiBase.UIName = uiName;
                    uiBase.OnInitUI();
                    uiBase.OnOpenUI();
                    SetUILayer(uiBase);
                    SetUIMode(uiBase);
                }
                panel.transform.SetAsLastSibling();
                _loadingUIDic.Remove(uiName);
            }
            return uiBase;
        }
        
        public void CloseUI(string uiName)
        {
            if (_allOpenGameUIDic.TryGetValue(uiName, out var uiBase))
            {
                if (uiBase.GameUIMode == EGameUIMode.ReverseChange)
                {
                    if (_openUIStack.Count > 0)
                    {
                        Debug.LogError($"栈长度 {_openUIStack.Count}");
                        _finallyOpenUIName = null;
                        var prev = _openUIStack.Pop();
                        if (prev != null)
                        {
                            Debug.LogError($"从栈中打开 {prev.UIName}");
                            OpenUI(prev.UIName, prev.Data).Forget();
                        }
                    }
                }
            
                uiBase.OnCloseUI();
                uiBase.transform.SetAsFirstSibling();
                uiBase.gameObject.SetActive(false);
                _allCloseGameUIDic.TryAdd(uiName, uiBase);
                Debug.LogError($"关闭UI {uiName}");
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
                if (uiBase.GameUIMode == EGameUIMode.ReverseChange)
                {
                    if (_openUIStack.Count > 0)
                    {
                        Debug.LogError($"栈长度 {_openUIStack.Count}");
                        _finallyOpenUIName = null;
                        var prev = _openUIStack.Pop();
                        if (prev != null)
                        {
                            Debug.LogError($"从栈中打开 {prev.UIName}");
                            OpenUI(prev.UIName, prev.Data).Forget();
                        }
                    }
                }
                
                uiBase.OnDestroyUI();
                Object.Destroy(uiBase.gameObject);
                _allOpenGameUIDic.Remove(uiName);
                if (uiName == _finallyOpenUIName)
                {
                    _finallyOpenUIName = null;
                }
                Debug.LogError($"删除UI: {uiName} , {_finallyOpenUIName}");
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
        
        public Transform GetUILayer(int layer)
        {
            return _uiLayerDic.GetValueOrDefault(layer);
        }
        
        private void SetUILayer(GameUIBase uiBase)
        {
            var layer = uiBase.GameUILayer;
            if (_uiLayerDic.TryGetValue((int)layer, out var transform))
            {
                uiBase.transform.SetParent(transform);
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
                    
                    if (!string.IsNullOrEmpty(_finallyOpenUIName))
                    {
                        Debug.LogError($"最后打开的UI {_finallyOpenUIName}");
                        if (_allOpenGameUIDic.TryGetValue(_finallyOpenUIName, out var finallyUiBase))
                        {
                            finallyUiBase.OnCloseUI();
                            finallyUiBase.transform.SetAsFirstSibling();
                            finallyUiBase.gameObject.SetActive(false);
                            _allCloseGameUIDic.TryAdd(_finallyOpenUIName, finallyUiBase);
                            Debug.LogError($"关闭并入栈的UI ,{_finallyOpenUIName}");
                            _allOpenGameUIDic.Remove(_finallyOpenUIName);
                            _openUIStack.Push(finallyUiBase);
                        }
                    }
                    /*_openUIStack.Push(uiBase);
                    Debug.LogError($"入栈的UI {uiBase.UIName}");*/
                    break;
            }

            Debug.LogError($"@栈长度 {_openUIStack.Count} {uiBase.UIName}");
            _finallyOpenUIName = uiBase.UIName;
            if(!_allOpenGameUIDic.TryAdd(uiBase.UIName, uiBase))
            {
                Debug.LogError($"repeatedly open ui in a frame time: {uiBase.UIName}");
            }
        }
    }
    
}
