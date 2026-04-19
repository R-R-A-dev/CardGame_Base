using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("このマップのSOデータ")]
    [SerializeField] private MapData mapData;       // 対応するMapData.assetをアサイン

    [Header("固定ノードUI")]
    [SerializeField] private MapNodeUI startNodeUI;
    [SerializeField] private MapNodeUI bossNodeUI;

    [Header("層ごとのノードUI")]
    [SerializeField] private List<MapRowUI> rowUIs;

    [SerializeField] private MapStatusUI statusUI;

    private Dictionary<NodeData, MapNodeUI> nodeUIMap
        = new Dictionary<NodeData, MapNodeUI>();
    private List<NodeData> selectableNodes = new List<NodeData>();
    private RoguelikeGameState gameState;

    public MapData MapData => mapData;              // MapUIから参照用

    public void Initialize(RoguelikeGameState state)
    {
        gameState = state;

        SetupMap();
        UpdateSelectableNodes();
        statusUI.Refresh(gameState);
    }

    private void SetupMap()
    {
        nodeUIMap.Clear();

        startNodeUI.gameObject.SetActive(false);
        bossNodeUI.gameObject.SetActive(false);
        foreach (var row in rowUIs)
            foreach (var nodeUI in row.nodeUIs)
                nodeUI.gameObject.SetActive(false);

        SetupNodeUI(startNodeUI, mapData.startNode);

        for (int i = 0; i < mapData.rows.Count; i++)
        {
            if (i >= rowUIs.Count) break;

            MapRowData rowData = mapData.rows[i];
            MapRowUI rowUI = rowUIs[i];
            List<NodeData> selected = SelectRandomNodes(rowData);

            for (int j = 0; j < selected.Count; j++)
            {
                if (j >= rowUI.nodeUIs.Count) break;
                SetupNodeUI(rowUI.nodeUIs[j], selected[j]);
            }
        }

        SetupNodeUI(bossNodeUI, mapData.bossNode);
    }

    private void SetupNodeUI(MapNodeUI nodeUI, NodeData nodeData)
    {
        nodeUI.gameObject.SetActive(true);
        nodeUI.Setup(nodeData, false, OnNodeClicked);
        nodeUIMap[nodeData] = nodeUI;
    }

    private void UpdateSelectableNodes()
    {
        foreach (var nodeUI in nodeUIMap.Values)
            nodeUI.SetSelectable(false);

        selectableNodes.Clear();

        if (gameState.CurrentNode == null)
            selectableNodes.Add(mapData.startNode);
        else
            selectableNodes = GetNextSelectableNodes(gameState.CurrentNode);

        foreach (NodeData node in selectableNodes)
            if (nodeUIMap.ContainsKey(node))
                nodeUIMap[node].SetSelectable(true);
    }

    private List<NodeData> GetNextSelectableNodes(NodeData currentNode)
    {
        foreach (MapRowData row in mapData.rows)
            foreach (NodeConnection connection in row.connections)
                if (connection.fromNode == currentNode)
                    return new List<NodeData>(connection.toNodes);

        return new List<NodeData> { mapData.bossNode };
    }

    private List<NodeData> SelectRandomNodes(MapRowData row)
    {
        List<NodeData> candidates = new List<NodeData>(row.candidateNodes);
        List<NodeData> selected = new List<NodeData>();

        int count = Mathf.Min(row.nodeCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, candidates.Count);
            selected.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return selected;
    }
    /// <summary>
    /// ノードをクリックしたときの処理。選択可能なノードであれば、ゲーム状態を更新し、UIをリフレッシュする。
    /// </summary>
    /// <param name="nodeData"></param>
    private void OnNodeClicked(NodeData nodeData)
    {
        if (!selectableNodes.Contains(nodeData)) return;

        gameState.CurrentNode = nodeData;
        gameState.ClearedNodes.Add(nodeData);

        UpdateSelectableNodes();
        statusUI.Refresh(gameState);

        RoguelikeManager.Instance.OnNodeSelected(nodeData);
    }


}