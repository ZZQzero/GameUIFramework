using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameUI
{
    public class GameUIRoot : MonoBehaviour
    {
        [SerializeField] private Camera uiCamera;
        [SerializeField] private Transform canvasRoot;
        
        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            for (int i = 0; i < canvasRoot.childCount; i++)
            {
                var child = canvasRoot.GetChild(i);
                GameUIManager.Instance.AddUILayer(i,child);
            }
        }

        public Camera UICamera()
        {
            return uiCamera;
        }

        public Transform CanvasRoot()
        {
            return canvasRoot;
        }
    }
}
