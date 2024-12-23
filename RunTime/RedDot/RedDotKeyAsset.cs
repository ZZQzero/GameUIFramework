using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    [CreateAssetMenu(fileName = "RedDotKeyAsset", menuName = "GameUI/RedDotKeyAsset", order = 1)]
    public class RedDotKeyAsset : ScriptableObject
    {
        public List<RedDotKeyData> AllRedDotList = new List<RedDotKeyData>();
    }
}