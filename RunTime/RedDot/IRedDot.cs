using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public interface IRedDot
    {
        public int Count { get; set; }
        public void OnRedDotAddChanged();
        public void OnRedDotRemoveChanged();
    }
}

