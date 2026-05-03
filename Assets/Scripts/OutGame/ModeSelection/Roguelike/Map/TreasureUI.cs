using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TreasureUI : MonoBehaviour
{
    [SerializeField] private GameObject cardRewardPanel;
    [SerializeField] private List<CardController> cardList;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TreasureCardSelectPanel selectPanel;

    private RoguelikeGameState gameState;

    public void Open(TreasureData data, RoguelikeGameState state)
    {
        gameState = state;
        gameObject.SetActive(true);

        // フラグON・選択枚数をSOから取得
        selectPanel.SetTreasureMode(true, data.cardCount);

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

    // 複数枚対応に変更
    public void OnCardSelected(List<int> cardIds)
    {
        foreach (int cardId in cardIds)
            gameState.CurrentDeck.Add(cardId);

        Close();
    }

    private void Close()
    {
        foreach (var card in cardList)
            card.gameObject.SetActive(false);

        // フラグOFF
        selectPanel.SetTreasureMode(false, 0);

        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }

    private List<int> GetRandomCards(int count)
    {
        return new List<int>();
    }

    private void CardDisplay(List<int> id)
    {
        foreach (var card in cardList)
            card.gameObject.SetActive(false);

        for (int i = 0; i < id.Count; i++)
        {
            if (i >= cardList.Count) break;
            cardList[i].gameObject.SetActive(true);
            cardList[i].Init(id[i], false);
        }
    }
}

//TODO:他のマスを押した結果実装