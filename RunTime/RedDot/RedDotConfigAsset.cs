using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    [CreateAssetMenu(fileName = "RedDotKeyAsset", menuName = "GameUI/RedDotKeyAsset", order = 1)]
    public class RedDotConfigAsset : ScriptableObject
    {
        public List<RedDotConfigData> AllRedDotList = new List<RedDotConfigData>();
    }
}