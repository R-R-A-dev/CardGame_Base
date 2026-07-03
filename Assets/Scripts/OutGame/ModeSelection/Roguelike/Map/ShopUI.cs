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
    [SerializeField] private OwnedDeckCheckPanel ownedDeckCheckPanel;

    private RoguelikeGameState gameState;
    private Dictionary<CardController, int> cardPriceMap
        = new Dictionary<CardController, int>();
    private List<int> purchasedCardIds = new List<int>();

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);

        ownedDeckCheckPanel.OnOpened = selectPanel.PauseShopMode;
        ownedDeckCheckPanel.OnClosed = selectPanel.ResumeShopMode;

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
    private void OnCardBuyConfirmed(List<CardController> cards, int price)
    {
        // 所持金チェック
        if (gameState.Gold < price)
        {
            Debug.Log("所持金が足りません");
            return;
        }

        // ゴールド消費
        gameState.Gold -= price;

        // 購入したカードをデッキへ追加＆非表示
        foreach (CardController card in cards)
        {
            gameState.CurrentDeck.Add(card.model.no);
            card.gameObject.SetActive(false);
        }

        // 所持金表示更新
        goldText.text = $"G: {gameState.Gold}";

        // ShopCardSelectPanelへ通知
        selectPanel.UpdateGold(gameState.Gold);
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