using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreasureUI : MonoBehaviour
{
    [SerializeField] private List<CardController> cardList;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI cardCountText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TreasureCardSelectPanel selectPanel;
    [SerializeField] private OwnedDeckCheckPanel ownedDeckCheckPanel;
    private RoguelikeGameState gameState;

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);

        ownedDeckCheckPanel.OnOpened = selectPanel.PauseTreasureMode;
        ownedDeckCheckPanel.OnClosed = selectPanel.ResumeTreasureMode;
    }

    public void Open(TreasureData data, RoguelikeGameState state)
    {
        gameState = state;
        gameObject.SetActive(true);

        selectPanel.SetTreasureMode(true);

        // ゴールド自動取得
        if (data.goldAmount > 0)
        {
            gameState.Gold += data.goldAmount;
            goldText.text = $"G +{data.goldAmount} 獲得！";
            goldText.gameObject.SetActive(true);
        }
        else
        {
            goldText.gameObject.SetActive(false);
        }

        // カードをランダムで選出して全て自動取得
        List<int> selectedCards = GetRandomCards(data);
        foreach (int cardId in selectedCards)
            gameState.CurrentDeck.Add(cardId);

        cardCountText.text = $"{selectedCards.Count}枚のカードを獲得！";

        CardDisplay(selectedCards);
    }

    private List<int> GetRandomCards(TreasureData data)
    {
        if (data.treasureCardPool.Count == 0)
        {
            Debug.LogWarning("treasureCardPoolが空です");
            return new List<int>();
        }

        // cardCountが0以下の場合は全て取得
        if (data.cardCount <= 0)
            return new List<int>(data.treasureCardPool);

        List<int> pool = new List<int>(data.treasureCardPool);
        List<int> result = new List<int>();

        // cardCountとpool数の小さい方を上限にする
        int count = Mathf.Min(data.cardCount, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index); // 選んだカードを除外して重複を防ぐ
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

    private void OnCloseButtonClick()
    {
        if (selectPanel.IsInfoPanelOpen)
        {
            selectPanel.CloseInfoPanel();
            return;
        }
        Close();
    }

    private void Close()
    {
        foreach (var card in cardList)
            card.gameObject.SetActive(false);

        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }
}