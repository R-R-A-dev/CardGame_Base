using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapRowUI
{
    public string rowName;
    public List<MapNodeUI> nodeUIs; // Inspectorで既存ボタンを登録
}