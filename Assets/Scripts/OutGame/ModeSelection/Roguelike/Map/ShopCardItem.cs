using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopCardItem : MonoBehaviour
{
    [Header("カードUI")]
    [SerializeField] private ShopCardUI shopCardUI;
    [SerializeField] private Button cardButton;         // カードクリックで詳細を開く

    [Header("ショップ固有UI")]
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject soldOutPanel;   // 購入済み表示

    private int cardId;
    private int price;
    private bool isSoldOut = false;
    private System.Action<int, int> onBought;
    private System.Action<int> onDetailRequested;

    public void Setup(int id, int cardPrice,
        System.Action<int, int> onBoughtCallback,
        System.Action<int> onDetailCallback)
    {
        cardId = id;
        price = cardPrice;
        onBought = onBoughtCallback;
        onDetailRequested = onDetailCallback;

        shopCardUI.Setup(cardId);
        priceText.text = $"G {price}";
        soldOutPanel.SetActive(false);

        cardButton.onClick.RemoveAllListeners();
        buyButton.onClick.RemoveAllListeners();

        cardButton.onClick.AddListener(OnCardButtonClick);
        buyButton.onClick.AddListener(OnBuyButtonClick);
    }

    public void RefreshBuyable(int currentGold)
    {
        buyButton.interactable = !isSoldOut && currentGold >= price;
    }

    private void OnCardButtonClick()
    {
        onDetailRequested?.Invoke(cardId);
    }

    private void OnBuyButtonClick()
    {
        onBought?.Invoke(cardId, price);
        isSoldOut = true;
        soldOutPanel.SetActive(true);
        buyButton.interactable = false;
    }
}