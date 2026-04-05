using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    [SerializeField] private Transform rowParent;
    [SerializeField] private MapNodeUI nodeUIPrefab;
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

        GenerateMap();
        UpdateSelectableNodes();
        statusUI.Refresh(gameState);
    }

    private void GenerateMap()
    {
        foreach (Transform child in rowParent)
            Destroy(child.gameObject);
        nodeUIMap.Clear();

        CreateRowUI("スタート", new List<NodeData> { currentMapData.startNode });

        foreach (MapRowData row in currentMapData.rows)
        {
            List<NodeData> selected = SelectRandomNodes(row);
            CreateRowUI(row.rowName, selected);
        }

        CreateRowUI("ボス", new List<NodeData> { currentMapData.bossNode });
    }

    private void CreateRowUI(string rowName, List<NodeData> nodes)
    {
        GameObject rowObject = new GameObject(rowName);
        rowObject.transform.SetParent(rowParent, false);

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 50f;
        layout.childAlignment = TextAnchor.MiddleCenter;

        foreach (NodeData node in nodes)
        {
            MapNodeUI nodeUI = Instantiate(nodeUIPrefab, rowObject.transform);
            nodeUI.Setup(node, false, OnNodeClicked);
            nodeUIMap[node] = nodeUI;
        }
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