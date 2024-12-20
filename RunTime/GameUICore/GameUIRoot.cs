using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public class GameUIRoot : MonoBehaviour
    {
        [HideInInspector]public Camera UICamera;
        
        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            UICamera = transform.Find("UICamera").GetComponent<Camera>();
            var canvasRoot = transform.Find("CanvasRoot");
            for (int i = 0; i < canvasRoot.childCount; i++)
            {
                var child = canvasRoot.GetChild(i);
                GameUIManager.Instance.AddUILayer(i,child);
            }
        }
    }
}
