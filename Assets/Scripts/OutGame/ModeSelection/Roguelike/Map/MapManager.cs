using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("固定ノードUI")]
    [SerializeField] private MapNodeUI startNodeUI;
    [SerializeField] private MapNodeUI bossNodeUI;

    [Header("層ごとのノードUI")]
    [SerializeField] private List<MapRowUI> rowUIs;

    [SerializeField] private MapStatusUI statusUI;

    private Dictionary<NodeData, MapNodeUI> nodeUIMap
        = new Dictionary<NodeData, MapNodeUI>();
    private List<NodeData> selectableNodes = new List<NodeData>();
    private MapData currentMapData;
    private RoguelikeGameState gameState;

    public void Initialize(MapData mapData, RoguelikeGameState state)
    {
        currentMapData = mapData;
        gameState = state;

        SetupMap();
        UpdateSelectableNodes();
        statusUI.Refresh(gameState);
    }

    private void SetupMap()
    {
        nodeUIMap.Clear();

        // 全ノードUIを一旦非表示
        startNodeUI.gameObject.SetActive(false);
        bossNodeUI.gameObject.SetActive(false);
        foreach (var row in rowUIs)
            foreach (var nodeUI in row.nodeUIs)
                nodeUI.gameObject.SetActive(false);

        // startNode設定
        SetupNodeUI(startNodeUI, currentMapData.startNode);

        // 各層のノードをランダムで選んで割り当て
        for (int i = 0; i < currentMapData.rows.Count; i++)
        {
            if (i >= rowUIs.Count) break;

            MapRowData rowData = currentMapData.rows[i];
            MapRowUI rowUI = rowUIs[i];
            List<NodeData> selected = SelectRandomNodes(rowData);

            for (int j = 0; j < selected.Count; j++)
            {
                if (j >= rowUI.nodeUIs.Count) break;
                SetupNodeUI(rowUI.nodeUIs[j], selected[j]);
            }
        }

        // bossNode設定
        SetupNodeUI(bossNodeUI, currentMapData.bossNode);
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
        {
            selectableNodes.Add(currentMapData.startNode);
        }
        else
        {
            selectableNodes = GetNextSelectableNodes(gameState.CurrentNode);
        }

        foreach (NodeData node in selectableNodes)
        {
            if (nodeUIMap.ContainsKey(node))
                nodeUIMap[node].SetSelectable(true);
        }
    }

    private List<NodeData> GetNextSelectableNodes(NodeData currentNode)
    {
        foreach (MapRowData row in currentMapData.rows)
        {
            foreach (NodeConnection connection in row.connections)
            {
                if (connection.fromNode == currentNode)
                    return new List<NodeData>(connection.toNodes);
            }
        }
        return new List<NodeData> { currentMapData.bossNode };
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