using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YooAsset;

namespace GameUI
{
    public enum ERedDotFuncType
    {
        [LabelText("无")]
        None = 0,
        主界面,
        变强,
        商店,
        英雄,
        背包,
        提升,
        宝箱,
        金币,
        宝石,
        天赋,
        升级,
        进阶,
        新增,
        合成,
        强化,
        淬炼,
    }
    
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

        public Dictionary<ERedDotFuncType, RedDotData> AllRedDataDic = new();

        private ResourcePackage _package;

        public async UniTask Init()
        {
            _package = YooAssets.GetPackage("DefaultPackage");
            var handle = _package.LoadAssetAsync<RedDotKeyAsset>("RedDotKeyAsset");
            await handle;
            var config = handle.GetAssetObject<RedDotKeyAsset>();
            if (config != null)
            {
                foreach (var item in config.AllRedDotList)
                {
                    if (AllRedDataDic.TryGetValue(item.RedDotType, out RedDotData childData))
                    {
                        AddRedDotParent(item.ParentDotType,childData,config);
                    }
                    else
                    {
                        childData = new RedDotData(config);
                        childData.DotType = item.RedDotType;
                        AllRedDataDic.Add(item.RedDotType,childData);
                        AddRedDotParent(item.ParentDotType, childData, config);
                    }
                    
                }

                Debug.LogError("初始化红点系统成功");
            }
        }

        public void AddRedDotParent(ERedDotFuncType type,RedDotData childData,RedDotKeyAsset config)
        {
            if (AllRedDataDic.TryGetValue(type, out var parentData))
            {
                childData.AddParent(parentData);
            }
            else
            {
                parentData = new RedDotData(config);
                parentData.DotType = type;
                childData.AddParent(parentData);
                AllRedDataDic.Add(type,parentData);
            }
        }

        public RedDotData GetRedDotData(ERedDotFuncType type)
        {
            return AllRedDataDic.GetValueOrDefault(type);
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

    }

}

