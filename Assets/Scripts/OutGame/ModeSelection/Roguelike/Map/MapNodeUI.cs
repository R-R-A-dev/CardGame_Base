using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapNodeUI : MonoBehaviour
{
    [SerializeField] private Button nodeButton;
    [SerializeField] private TextMeshProUGUI stageTypeText;

    private NodeData nodeData;
    private System.Action<NodeData> onNodeClicked;

    public void Setup(NodeData data, bool isSelectable, System.Action<NodeData> onClicked)
    {
        nodeData = data;
        onNodeClicked = onClicked;

        stageTypeText.text = GetStageTypeText(data.stageType);
        nodeButton.interactable = isSelectable;

        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(() => onNodeClicked?.Invoke(nodeData));
    }

    public void SetSelectable(bool isSelectable)
    {
        nodeButton.interactable = isSelectable;
    }

    private string GetStageTypeText(StageType type)
    {
        return type switch
        {
            StageType.NORMAL_BATTLE => "戦闘",
            StageType.ELITE_BATTLE => "強敵",
            StageType.BOSS_BATTLE => "ボス",
            StageType.REST => "休憩",
            StageType.SHOP => "ショップ",
            StageType.TREASURE => "宝箱",
            StageType.EVENT => "？",
            StageType.DAMAGE => "ダメージ",
            StageType.CARD_LOSS => "カード消失",
            _ => "？"
        };
    }
}