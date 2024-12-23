using System;
using UnityEngine;

namespace GameUI
{
    [Serializable]
    public class RedDotConfigData
    {
        [Header("当前红点")]
        public ERedDotFuncType RedDotType;
        [Header("父级红点")]
        public ERedDotFuncType ParentDotType;
        [Header("描述")]
        public string Des;
    }
}