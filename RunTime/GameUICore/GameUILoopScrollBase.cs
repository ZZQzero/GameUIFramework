using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    [DisallowMultipleComponent]
    public abstract class GameUILoopScrollBase : GameUIBase,LoopScrollPrefabSource,LoopScrollDataSource
    {
        protected LoopScrollRect ScrollRect;
        public GameObject item;

        public override void OnInitUI()
        {
            base.OnInitUI();
            ScrollRect.prefabSource = this;
            ScrollRect.dataSource = this;
        }
        

        /// <summary>
        /// 获取GameObject
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual GameObject GetObject(int index)
        {
            return null;
        }

        /// <summary>
        /// 回收GameObject
        /// </summary>
        /// <param name="trans"></param>
        public virtual void ReturnObject(Transform trans)
        {
            
        }

        /// <summary>
        /// 刷新GameObject
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="idx"></param>
        public virtual void ProvideData(Transform trans, int idx)
        {
            
        }
    }

}
