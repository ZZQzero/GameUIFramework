using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace GameUI
{
    [CreateAssetMenu(fileName = "RedDotKeyAsset", menuName = "GameUI/RedDotKeyAsset", order = 1)]
    [LabelText("红点枚举配置资源")]
    public class RedDotKeyAsset : SerializedScriptableObject
    {
        [ShowInInspector]
        public List<RedDotKeyData> AllRedDotList = new List<RedDotKeyData>();
    }
}