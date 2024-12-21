using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{

    public class ScrollMultiData
    {
        public int TypeIndex;
        public string name;
        public Color color;
    }

    public class GameLoopScrollManager
    {
        private GameLoopScrollManager()
        {
        }

        private static GameLoopScrollManager _instance;

        public static GameLoopScrollManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameLoopScrollManager();
                }

                return _instance;
            }
        }

        public List<int> ScrollDataList = new List<int>();

        public List<ScrollMultiData> ScrollMultiDataList = new();
    }

}
