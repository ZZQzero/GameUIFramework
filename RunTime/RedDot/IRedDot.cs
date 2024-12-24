using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    internal interface IRedDot
    {
        public int Count { get; set; }
        public void OnRedDotAddChanged();
        public void OnRedDotRemoveChanged();
    }
}

