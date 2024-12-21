using System.Collections;
using System.Collections.Generic;
using GameUI;
using UnityEngine;
using UnityEngine.UI;

public class ScrollIndexCallback4 : MonoBehaviour
{
    public Image image;
    public Text txt;
    
    public void ScrollCellIndex(int index,ScrollMultiData data)
    {
        image.color = data.color;
        txt.text = data.name;
    }
}
