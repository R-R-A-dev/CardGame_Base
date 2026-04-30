using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TreasureUI : MonoBehaviour
{
    [SerializeField] private GameObject cardRewardPanel;
    [SerializeField] private List<CardController> cardList;
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

        CardDisplay(cards);
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

    private void CardDisplay(List<int> id)
    {
        //init関数でidごとに設定
        for (int i = 0; i < id.Count; i++)
        {
            cardList[i].gameObject.SetActive(true);
            cardList[i].Init(id[i], false);
        }
    }
}