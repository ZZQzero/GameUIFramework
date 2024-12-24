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
            _handle = _package.LoadAssetAsync<RedDotConfigAsset>("RedDotKeyAsset");
            await _handle;
            var config = _handle.GetAssetObject<RedDotConfigAsset>();
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
                        childData = new RedDotData(item.RedDotType);
                        _allRedDataDic.Add((int)item.RedDotType,childData);
                        AddRedDotParent(item.ParentDotType, childData);
                    }
                }
#if UNITY_EDITOR
                CheckRedDotRepeatedConfig(config);
                CheckRedDotCircle();
#endif
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
                parentData = new RedDotData(type);
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

        /// <summary>
        /// 检查所有节点中是否存在环
        /// </summary>
        public void CheckRedDotCircle()
        {
            foreach (var item in _allRedDataDic)
            {
                HashSet<RedDotData> visited = new HashSet<RedDotData>();
                CheckRedDotCircle(item.Value,visited);
            }
        }
        

        /// <summary>
        /// 检查该红点中是否存在环
        /// </summary>
        /// <param name="data"></param>
        /// <param name="visited"></param>
        public void CheckRedDotCircle(RedDotData data,HashSet<RedDotData> visited)
        {
            if (data.ChildList.Count == 0)
            {
                return;
            }
            foreach (var item in data.ChildList)
            {
                if(!visited.Add(item))
                {
                    Debug.LogError($"红点配置中存在环!!!子节点：{item.DotType}  父节点：{data.DotType}");
                    return;
                }
                CheckRedDotCircle(item,visited);
            }
        }
        
        /// <summary>
        /// 检查红点系统中是否存在相同的配置
        /// </summary>
        public void CheckRedDotRepeatedConfig(RedDotConfigAsset config)
        {
            HashSet<string> visited = new HashSet<string>();
            foreach (var item in config.AllRedDotList)
            {
                var str = item.RedDotType + item.ParentDotType.ToString();
                if (!visited.Add(str))
                {
                    Debug.LogError($"红点系统中存在重复的配置：子节点 ：{item.RedDotType}  父节点：{item.ParentDotType}");
                }
            }
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

