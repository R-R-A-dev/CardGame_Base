using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BattleRewardUI : MonoBehaviour
{
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private List<CardController> cardList;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private BattleRewardCardSelectPanel selectPanel;

    private RoguelikeGameState gameState;

    private void Start()
    {
        // ActionにOnCardSelectedを登録
        selectPanel.OnCardsConfirmed = OnCardSelected;
    }

    public void Open(RewardData data, RoguelikeGameState state)
    {
        gameState = state;
        gameObject.SetActive(true);

        // ゴールド自動入手
        if (data.hasGoldReward)
        {
            int gold = data.isRandomGold
                ? Random.Range(data.goldMin, data.goldMax)
                : data.goldFixed;

            gameState.Gold += gold;
            goldText.text = $"G +{gold} 獲得！";
            goldText.gameObject.SetActive(true);
        }
        else
        {
            goldText.gameObject.SetActive(false);
        }

        // 選択枚数をSOから取得してパネル初期化
        selectPanel.SetRewardMode(true, data.cardSelectCount);

        // 提示カードを抽選して表示
        List<int> cards = GetRewardCards(data);
        CardDisplay(cards);
    }

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

        selectPanel.SetRewardMode(false, 0);
        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }

    private List<int> GetRewardCards(RewardData data)
    {
        List<int> pool = data.rewardCardPool.Count > 0
            ? new List<int>(data.rewardCardPool)
            : new List<int>();

        List<int> result = new List<int>();
        int count = Mathf.Min(data.cardChoiceCount, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return result;
    }

    private void CardDisplay(List<int> ids)
    {
        foreach (var card in cardList)
            card.gameObject.SetActive(false);

        for (int i = 0; i < ids.Count; i++)
        {
            if (i >= cardList.Count) break;
            cardList[i].gameObject.SetActive(true);
            cardList[i].Init(ids[i], false);
        }
    }

    public void CloseInfoPanel()
    {
        cardInfoPanel.SetActive(false);
    }
}