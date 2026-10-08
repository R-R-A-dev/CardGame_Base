using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapRowData", menuName = "Roguelike/MapRowData")]
public class MapRowData : ScriptableObject
{
    [Header("層の情報")]
    public string rowName;

    [Header("候補ノード")]
    public List<NodeData> candidateNodes;

    [Header("この層に並ぶノード数")]
    public int nodeCount = 2;

    [Header("次の層への接続（手動設定）")]
    public List<NodeConnection> connections;
}

[System.Serializable]
public class NodeConnection
{
    public NodeData fromNode;
    public List<NodeData> toNodes;
}