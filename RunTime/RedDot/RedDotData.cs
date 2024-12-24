using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public class RedDotData : IRedDot,IDisposable
    {
        internal ERedDotFuncType DotType = ERedDotFuncType.None;
        
        internal string LocalSaveKey;
        public int Count { get; set; }
        
        /// <summary>
        /// 我的所有父级
        /// </summary>
        internal HashSet<RedDotData> ParentList { get; private set; }

        /// <summary>
        /// 我的所有子级
        /// </summary>
        internal HashSet<RedDotData> ChildList { get; private set; }

        /// <summary>
        /// 红点改变通知
        /// </summary>
        public Action<ERedDotFuncType,int> OnRedDotChangedAction;

        internal RedDotData(ERedDotFuncType type)
        {
            DotType = type;
            LocalSaveKey = $"RedDotKey_{(int)type}";
            ParentList = new HashSet<RedDotData>();
            ChildList = new HashSet<RedDotData>();
            Count = PlayerPrefs.GetInt(LocalSaveKey);
        }

        /// <summary>
        /// 添加父级对象
        /// </summary>
        internal bool AddParentNode(RedDotData data)
        {
            var addPrent = ParentList.Add(data);       //目标设定为我的父级
            var addChild = data.AddChildNode(this); //因为他是我的父级所以我为他的子级
            return addPrent && addChild;
        }
        
        /// <summary>
        /// 添加子对象
        /// </summary>
        private bool AddChildNode(RedDotData data)
        {
            return ChildList.Add(data);
        }
        
        public void OnRedDotAddChanged()
        {
            Count++;
            OnRedDotChangedAction?.Invoke(DotType,Count);
            PlayerPrefs.SetInt(LocalSaveKey,Count);
            AddParentRedDot(ParentList);
        }

        public void OnRedDotRemoveChanged()
        {
            Count--;
            if (Count < 0)
            {
                Debug.LogError($"你是不是在哪多调用了一次删除红点！{DotType}");
                Count = 0;
                return;
            }
            OnRedDotChangedAction?.Invoke(DotType,Count);
            PlayerPrefs.SetInt(LocalSaveKey,Count);
            RemoveParentRedDot(ParentList);
        }

        private void AddParentRedDot(HashSet<RedDotData> parentDataList)
        {
            foreach (var parent in parentDataList)
            {
                parent.Count++;
                parent.OnRedDotChangedAction?.Invoke(parent.DotType,parent.Count);
                PlayerPrefs.SetInt(parent.LocalSaveKey,parent.Count);
                if (parent.ParentList is {Count: > 0})
                {
                    AddParentRedDot(parent.ParentList);
                }
            }
        }
        
        private void RemoveParentRedDot(HashSet<RedDotData> parentDataList)
        {
            foreach (var parent in parentDataList)
            {
                parent.Count--;
                if (parent.Count < 0)
                {
                    Debug.LogError($"你是不是在哪多调用了一次删除红点！{parent.DotType}");
                    parent.Count = 0;
                }
                parent.OnRedDotChangedAction?.Invoke(parent.DotType,parent.Count);
                PlayerPrefs.SetInt(parent.LocalSaveKey,parent.Count);
                if (parent.ParentList is {Count: > 0})
                {
                    RemoveParentRedDot(parent.ParentList);
                }
            }
        }

        public void Dispose()
        {
            ParentList.Clear();
            ChildList.Clear();
            Count = 0;
            ParentList = null;
            ChildList = null;
            OnRedDotChangedAction = null;
        }
    }
}