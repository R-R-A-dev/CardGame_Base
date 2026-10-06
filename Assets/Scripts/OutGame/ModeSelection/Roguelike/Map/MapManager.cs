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
    [SerializeField] private StageInfoPanel stageInfoPanel;

    private Dictionary<NodeData, MapNodeUI> nodeUIMap
        = new Dictionary<NodeData, MapNodeUI>();
    private List<NodeData> selectableNodes = new List<NodeData>();
    private RoguelikeGameState gameState;
    private NodeData selectedNode;                  // クリックで選択中のノード。Startボタンで作動させる

    public MapData MapData => mapData;              // MapUIから参照用

    public void Initialize(RoguelikeGameState state)
    {
        gameState = state;
        selectedNode = null;

        SetupMap();
        UpdateSelectableNodes();
        statusUI.Refresh(gameState);

        stageInfoPanel.SetOnStart(OnStartClicked);
        stageInfoPanel.Hide();
    }

    private void SetupMap()
    {
        nodeUIMap.Clear();

        startNodeUI.gameObject.SetActive(false);
        bossNodeUI.gameObject.SetActive(false);
        foreach (var row in rowUIs)
            foreach (var nodeUI in row.nodeUIs)
                nodeUI.SetSelectable(false);

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
    /// ノードをクリックしたときの処理。クリックしたノードを選択中にして、ステージ情報パネルに表示する。
    /// 今は進めないノードの場合は、情報だけ表示して Start を押せなくする
    /// </summary>
    /// <param name="nodeData"></param>
    private void OnNodeClicked(NodeData nodeData)
    {
        if (nodeData == selectedNode) return;

        if (selectedNode != null && nodeUIMap.ContainsKey(selectedNode))
            nodeUIMap[selectedNode].SetSelected(false);

        selectedNode = nodeData;
        if (nodeUIMap.ContainsKey(nodeData))
            nodeUIMap[nodeData].SetSelected(true);

        stageInfoPanel.Show(nodeData, gameState, selectableNodes.Contains(nodeData));
    }

    /// <summary>
    /// ステージ情報パネルのStartボタンから呼ばれる。選択中のノードへ進み、マスを作動させる。
    /// </summary>
    private void OnStartClicked()
    {
        if (selectedNode == null || !selectableNodes.Contains(selectedNode)) return;

        NodeData nodeData = selectedNode;
        selectedNode = null;
        stageInfoPanel.Hide();

        gameState.CurrentNode = nodeData;
        gameState.ClearedNodes.Add(nodeData);

        UpdateSelectableNodes();
        statusUI.Refresh(gameState);

        RoguelikeManager.Instance.OnNodeSelected(nodeData);
    }

    public void PlayHealEffect(int healAmount, RoguelikeGameState state)
    {
        statusUI.PlayHealEffect(healAmount, state);
    }


}