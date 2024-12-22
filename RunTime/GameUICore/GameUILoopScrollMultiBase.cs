using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    [RequireComponent(typeof(UnityEngine.UI.LoopScrollRectMulti))]
    [DisallowMultipleComponent]
    public abstract class GameUILoopScrollMultiBase : GameUIBase,LoopScrollPrefabSource, LoopScrollMultiDataSource
    {
        public List<GameObject> ItemList;
        protected LoopScrollRectMulti ScrollRect;

        public override void OnInitUI()
        {
            ScrollRect.prefabSource = this;
            ScrollRect.dataSource = this;
            base.OnInitUI();
        }

        public virtual GameObject GetObject(int index)
        {
            return null;
        }

        public virtual void ReturnObject(Transform trans)
        {
            
        }

        public virtual void ProvideData(Transform trans, int index)
        {
            
        }
    }
}