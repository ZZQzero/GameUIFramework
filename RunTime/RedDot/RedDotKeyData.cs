using System;
using GameUI;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace GameUI
{
    [HideLabel]
    [HideReferenceObjectPicker]
    [Serializable]
    public class RedDotKeyData
    {
        [LabelText("当前红点")]
        [LabelWidth(50)]
        [MinValue(1)]
        [TableColumnWidth(150, resizable: false)]
        [ShowInInspector]
        [OdinSerialize]
        public ERedDotFuncType RedDotType { get; internal set; }
        
        [LabelText("父级红点")]
        [LabelWidth(50)]
        [MinValue(1)]
        [TableColumnWidth(150, resizable: false)]
        [ShowInInspector]
        [OdinSerialize]
        public ERedDotFuncType ParentDotType { get; internal set; }

        [LabelText("描述")]
        [LabelWidth(50)]
        [ShowInInspector]
        [OdinSerialize]
        public string Des { get; internal set; }

        private RedDotKeyData()
        {
        }

        public RedDotKeyData(ERedDotFuncType type, string des)
        {
            RedDotType = type;
            Des = des;
        }
    }
}