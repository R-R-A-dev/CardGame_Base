using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private List<CardController> cardList;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button closeButton;
    [SerializeField] private ShopCardSelectPanel selectPanel;

    private RoguelikeGameState gameState;

    // カードと価格の対応表
    private Dictionary<CardController, int> cardPriceMap
        = new Dictionary<CardController, int>();

    // 購入済みカードのID一覧
    private List<int> purchasedCardIds = new List<int>();

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);

        // Actionを登録
        selectPanel.OnCardBuyConfirmed = OnCardBuyConfirmed;
    }

    public void Open(ShopData data, RoguelikeGameState state)
    {
        gameState = state;
        gameObject.SetActive(true);

        purchasedCardIds.Clear();
        cardPriceMap.Clear();

        goldText.text = $"G: {gameState.Gold}";

        // カードを非表示にリセット
        foreach (var card in cardList)
            card.gameObject.SetActive(false);

        // 販売カードリストを取得
        List<int> cards = data.shopCardList.Count > 0
            ? data.shopCardList
            : GetRandomCards(data.shopCardCount);

        CardDisplay(cards);
        selectPanel.SetShopMode(true, cardPriceMap);
        //// カードを表示して価格を設定
        //for (int i = 0; i < cards.Count; i++)
        //{
        //    if (i >= cardList.Count) break;

        //    int price = Random.Range(data.cardPriceMin, data.cardPriceMax);
        //    cardList[i].gameObject.SetActive(true);
        //    cardList[i].Init(cards[i], false);
        //    cardPriceMap[cardList[i]] = price;
        //}

        // ShopModeをONに
        //selectPanel.SetShopMode(true, cardPriceMap);
    }

    private void OnCardBuyConfirmed(int cardId, int price)
    {
        // ゴールドが足りない場合は処理しない
        if (gameState.Gold < price)
        {
            Debug.Log("ゴールドが足りません");
            return;
        }

        // 既に購入済みの場合は処理しない
        if (purchasedCardIds.Contains(cardId))
        {
            Debug.Log("既に購入済みです");
            return;
        }

        gameState.Gold -= price;
        gameState.CurrentDeck.Add(cardId);
        purchasedCardIds.Add(cardId);

        goldText.text = $"G: {gameState.Gold}";

        // 購入済みカードを非表示
        foreach (var kvp in cardPriceMap)
        {
            if (kvp.Key.model.no == cardId)
            {
                kvp.Key.gameObject.SetActive(false);
                break;
            }
        }
    }

private void OnCloseButtonClick()
{
    if (selectPanel.IsInfoPanelOpen)
    {
        // 詳細パネルが開いていれば閉じるだけ
        selectPanel.CloseInfoPanel();
        return;
    }

    // 詳細パネルが閉じていればShopUIを閉じてマップへ
    Close();
}

    private void Close()
    {
        foreach (var card in cardList)
            card.gameObject.SetActive(false);

        selectPanel.SetShopMode(false, null);
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