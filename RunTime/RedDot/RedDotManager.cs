using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace GameUI
{
    public class RedDotManager
    {
        private RedDotManager()
        {
        }

        private static RedDotManager _instance;

        public static RedDotManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RedDotManager();
                }

                return _instance;
            }
        }

        private Dictionary<int, RedDotData> _allRedDataDic = new();

        private ResourcePackage _package;
        private AssetHandle _handle;

        public void Init()
        {
            InitRedDot().Forget();
        }

        private async UniTask InitRedDot()
        {
            _package = YooAssets.GetPackage("DefaultPackage");
            _handle = _package.LoadAssetAsync<RedDotKeyAsset>("RedDotKeyAsset");
            await _handle;
            var config = _handle.GetAssetObject<RedDotKeyAsset>();
            if (config != null)
            {
                foreach (var item in config.AllRedDotList)
                {
                    if (_allRedDataDic.TryGetValue((int)item.RedDotType, out RedDotData childData))
                    {
                        AddRedDotParent(item.ParentDotType,childData);
                    }
                    else
                    {
                        childData = new RedDotData();
                        childData.DotType = item.RedDotType;
                        _allRedDataDic.Add((int)item.RedDotType,childData);
                        AddRedDotParent(item.ParentDotType, childData);
                    }
                    
                }
            }
        }

        private void AddRedDotParent(ERedDotFuncType type,RedDotData childData)
        {
            if (_allRedDataDic.TryGetValue((int)type, out var parentData))
            {
                childData.AddParentNode(parentData);
            }
            else
            {
                parentData = new RedDotData();
                parentData.DotType = type;
                childData.AddParentNode(parentData);
                _allRedDataDic.Add((int)type,parentData);
            }
        }

        public RedDotData GetRedDotData(ERedDotFuncType type)
        {
            return _allRedDataDic.GetValueOrDefault((int)type);
        }

        public void RedDotAddChanged(ERedDotFuncType type)
        {
            var data = GetRedDotData(type);
            if (data == null)
            {
                return;
            }
            if (data.ParentList.Count == 0 || data.ChildList.Count != 0)
            {
                Debug.LogError($"该节点不是最底层叶子节点： {type}，红点不允许在设置");
                return;
            }
            if(data.Count != 0)
            {
                Debug.LogError($"已经有红点了： {type}");
                return;
            }
            data.OnRedDotAddChanged();
        }
        
        public void RedDotRemoveChanged(ERedDotFuncType type)
        {
            var data = GetRedDotData(type);
            if (data == null)
            {
                return;
            }
            if (data.ParentList.Count == 0 || data.ChildList.Count != 0)
            {
                Debug.LogError($"该节点不是最底层叶子节点： {type}，红点不允许在设置");
                return;
            }

            if (data.Count == 0)
            {
                Debug.LogError($"没有可移除的红点： {type}");
                return;
            }
            data.OnRedDotRemoveChanged();
        }
        
        public void ClearAllRedDot()
        {
            foreach (var item in _allRedDataDic)
            {
                item.Value.Dispose();
            }
            _allRedDataDic.Clear();
            _handle.Release();
        }

    }

}

