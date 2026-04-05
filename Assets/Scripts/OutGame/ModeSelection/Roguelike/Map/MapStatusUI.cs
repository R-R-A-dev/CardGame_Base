using UnityEngine;
using TMPro;

public class MapStatusUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI deckCountText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI currentNodeText;

    public void Refresh(RoguelikeGameState state)
    {
        hpText.text = $"HP: {state.CurrentHP} / {state.MaxHP}";
        deckCountText.text = $"デッキ: {state.CurrentDeck.Count}枚";
        goldText.text = $"G: {state.Gold}";
        currentNodeText.text = state.CurrentNode != null
            ? $"現在地: {state.CurrentNode.nodeId}"
            : "現在地: スタート";
    }
}