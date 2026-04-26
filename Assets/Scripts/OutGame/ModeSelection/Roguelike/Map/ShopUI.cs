using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private Transform cardListParent;
    [SerializeField] private ShopCardItem cardItemPrefab;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button closeButton;

    private RoguelikeGameState gameState;
    private List<ShopCardItem> spawnedItems = new List<ShopCardItem>();


    [SerializeField] private ShopCardDetailPanel shopDetailPanel; // 新規パネルに変更

    private void OnDetailRequested(int cardId)
    {
        shopDetailPanel.Open(cardId);
    }

    public void Open(ShopData data, RoguelikeGameState state)
    {
        gameState = state;
        gameObject.SetActive(true);
        goldText.text = $"G: {state.Gold}";

        foreach (Transform child in cardListParent)
            Destroy(child.gameObject);
        spawnedItems.Clear();

        List<int> cards = data.shopCardList.Count > 0
            ? data.shopCardList
            : GetRandomCards(data.shopCardCount);

        foreach (int cardId in cards)
        {
            ShopCardItem item = Instantiate(cardItemPrefab, cardListParent);
            int price = Random.Range(data.cardPriceMin, data.cardPriceMax);
            item.Setup(cardId, price, OnCardBought, OnDetailRequested);
            item.RefreshBuyable(state.Gold);
            spawnedItems.Add(item);
        }
    }

    private void OnCardBought(int cardId, int price)
    {
        if (gameState.Gold < price) return;

        gameState.Gold -= price;
        gameState.CurrentDeck.Add(cardId);
        goldText.text = $"G: {gameState.Gold}";

        // 全アイテムのボタン状態を更新
        foreach (ShopCardItem item in spawnedItems)
            item.RefreshBuyable(gameState.Gold);
    }

    

    public void OnCloseButtonClick()
    {
        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }

    private List<int> GetRandomCards(int count)
    {
        return new List<int>();
    }
}