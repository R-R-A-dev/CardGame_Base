using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TreasureUI : MonoBehaviour
{
    [SerializeField] private Transform cardListParent;
    [SerializeField] private TextMeshProUGUI goldText;

    private RoguelikeGameState gameState;

    public void Open(TreasureData data, RoguelikeGameState state)
    {
        gameState = state;
        gameObject.SetActive(true);

        // ゴールド獲得
        if (data.goldAmount > 0)
        {
            gameState.Gold += data.goldAmount;
            goldText.text = $"G +{data.goldAmount}";
        }

        // カード報酬表示
        List<int> cards = data.treasureCardPool.Count > 0
            ? data.treasureCardPool
            : GetRandomCards(data.cardCount);

        foreach (int cardId in cards)
        {
            // カードUIを生成して表示
        }
    }

    public void OnCardSelected(int cardId)
    {
        gameState.CurrentDeck.Add(cardId);
        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }

    private List<int> GetRandomCards(int count)
    {
        return new List<int>();
    }
}