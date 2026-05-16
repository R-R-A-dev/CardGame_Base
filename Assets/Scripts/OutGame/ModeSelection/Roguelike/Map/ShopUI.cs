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
    private Dictionary<CardController, int> cardPriceMap
        = new Dictionary<CardController, int>();
    private List<int> purchasedCardIds = new List<int>();

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);

        // Actionを複数カード対応に変更
        selectPanel.OnCardBuyConfirmed = OnCardBuyConfirmed;
    }

    public void Open(ShopData data, RoguelikeGameState state)
    {
        gameState = state;
        gameObject.SetActive(true);

        purchasedCardIds.Clear();
        cardPriceMap.Clear();

        goldText.text = $"G: {gameState.Gold}";

        foreach (var card in cardList)
            card.gameObject.SetActive(false);

        List<int> cards = data.shopCardList.Count > 0
            ? data.shopCardList
            : GetRandomCards(data.shopCardCount);

        // カード表示と価格設定
        for (int i = 0; i < cards.Count; i++)
        {
            if (i >= cardList.Count) break;
            cardList[i].gameObject.SetActive(true);
            cardList[i].Init(cards[i], false);
            int price = cardList[i].model.price;
            cardPriceMap[cardList[i]] = price;
        }

        // 所持金も渡す
        selectPanel.SetShopMode(true, cardPriceMap, gameState.Gold);
    }

    // 複数カード購入処理
    private void OnCardBuyConfirmed(List<int> cardIds, int totalPrice)
    {
        // 所持金チェック
        if (gameState.Gold < totalPrice)
        {
            Debug.Log("所持金が足りません");
            return;
        }

        gameState.Gold -= totalPrice;
        goldText.text = $"G: {gameState.Gold}";

        foreach (int cardId in cardIds)
        {
            if (purchasedCardIds.Contains(cardId)) continue;

            gameState.CurrentDeck.Add(cardId);
            purchasedCardIds.Add(cardId);

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

        // 所持金更新をパネルに通知
        selectPanel.UpdateGold(gameState.Gold);
        goldText.text = $"G: {gameState.Gold}";
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

        selectPanel.SetShopMode(false, null, 0);
        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }

    private List<int> GetRandomCards(int count)
    {
        return new List<int>();
    }
}