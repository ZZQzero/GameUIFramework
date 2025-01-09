using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public abstract class GameUIBase: MonoBehaviour,IGameUI
    {
        public EGameUILayer GameUILayer = EGameUILayer.Normal;
        public EGameUIMode GameUIMode = EGameUIMode.Normal;
        public string UIName { get; set; }
        public object Data { get; set; }

        public virtual void OnInitUI()
        {
            
        }

        public virtual void OnOpenUI()
        {
        
        }
        
        public virtual void OnRefreshUI()
        {
        
        }

        public virtual void OnCloseUI()
        {
        
        }

        public virtual void OnDestroyUI()
        {
        
        }

        /// <summary>
        /// 关闭自己
        /// </summary>
        public void CloseSelf()
        {
            GameUIManager.Instance.CloseUI(UIName);
        }

        /// <summary>
        /// 关闭并且销毁自己
        /// </summary>
        public void CloseAndDestroySelf()
        {
            GameUIManager.Instance.CloseAndDestroyUI(UIName);
        }
    }
}

