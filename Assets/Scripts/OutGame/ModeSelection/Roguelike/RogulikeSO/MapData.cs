using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "Roguelike/MapData")]
public class MapData : ScriptableObject
{
    [Header("マップ情報")]
    public string mapName;

    [Header("固定ノード")]
    public NodeData startNode;
    public NodeData bossNode;

    [Header("層の一覧（上から順番）")]
    public List<MapRowData> rows;
}