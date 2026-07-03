using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ShopCardSelectPanel : MonoBehaviour, IPointerDownHandler
{
    [Header("詳細パネル")]
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private CardController cardController;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHealthText;
    [SerializeField] private TextMeshProUGUI cardDescriptionText;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("ボタン")]
    [SerializeField] private Button cardBuyButton;
    [SerializeField] private Button closeInfoPanelButton;

    [Header("合計金額表示")]
    [SerializeField] private TextMeshProUGUI totalPriceText;

    public bool IsInfoPanelOpen => cardInfoPanel.activeSelf;

    // ShopUIへの通知
    public System.Action<List<CardController>, int> OnCardBuyConfirmed; // 選択カードID一覧・合計金額

    private bool isShopMode = false;
    private int currentGold = 0;                            // 所持金を保持

    private int selectedCardId = -1;
    private int selectedCardPrice = 0;

    private List<int> selectedCardIds = new List<int>();
    private List<CardController> selectedCards = new List<CardController>();
    private int totalSelectedPrice = 0;                     // 選択中の合計金額

    private Dictionary<CardController, int> cardPriceMap
        = new Dictionary<CardController, int>();

    private void Start()
    {
        cardBuyButton.interactable = false;
        cardBuyButton.onClick.RemoveAllListeners();
        closeInfoPanelButton.onClick.RemoveAllListeners();
        cardBuyButton.onClick.AddListener(OnCardBuyButtonClick);
        closeInfoPanelButton.onClick.AddListener(CloseInfoPanel);
        cardInfoPanel.SetActive(false);
    }

    public void SetShopMode(bool isActive, Dictionary<CardController, int> priceMap, int gold)
    {
        isShopMode = isActive;
        cardPriceMap = priceMap ?? new Dictionary<CardController, int>();
        currentGold = gold;
        selectedCardId = -1;
        selectedCardPrice = 0;
        selectedCardIds.Clear();
        selectedCards.Clear();
        totalSelectedPrice = 0;
        cardBuyButton.interactable = false;
        cardInfoPanel.SetActive(false);
        UpdateTotalPriceText();
    }

    // 所持金更新（購入後にShopUIから呼ぶ）
    public void UpdateGold(int gold)
    {
        currentGold = gold;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isShopMode) return;

        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        CardController card = clickedObject.GetComponentInParent<CardController>();

        if (card == null) return;

        // 右クリック：詳細パネル表示
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OpenInfoPanel(card);
            return;
        }

        // 左クリック：カード選択・選択解除
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            int cardPrice = cardPriceMap.ContainsKey(card) ? cardPriceMap[card] : 0;

            if (selectedCards.Contains(card))
            {
                // 選択解除
                SetCardSelectedPanel(card, false);
                selectedCardIds.Remove(card.model.no);
                selectedCards.Remove(card);
                totalSelectedPrice -= cardPrice;
            }
            else
            {
                // 所持金を超える場合は選択不可
                if (totalSelectedPrice + cardPrice > currentGold)
                {
                    Debug.Log("所持金が足りません");
                    return;
                }

                // 選択追加
                SetCardSelectedPanel(card, true);
                selectedCardIds.Add(card.model.no);
                selectedCards.Add(card);
                totalSelectedPrice += cardPrice;
            }

            UpdateTotalPriceText();

            // 1枚以上選択していれば購入ボタン有効
            cardBuyButton.interactable = selectedCardIds.Count > 0;
        }
    }

    private void SetCardSelectedPanel(CardController card, bool isActive)
    {
        Transform selectedPanel = card.transform.Find("SelectedPanel");
        if (selectedPanel != null)
            selectedPanel.gameObject.SetActive(isActive);
    }

    private void OpenInfoPanel(CardController card)
    {
        selectedCardId = card.model.no;
        selectedCardPrice = cardPriceMap.ContainsKey(card) ? cardPriceMap[card] : 0;

        cardInfoPanel.SetActive(true);
        cardController.Init(card.model.no, false);
        cardNameText.text = card.model.name;
        cardAttackText.text = card.model.at.ToString();
        cardHealthText.text = card.model.hp.ToString();
        cardDescriptionText.text = card.model.description;
        priceText.text = $"G {selectedCardPrice}";
    }

    private void UpdateTotalPriceText()
    {
        totalPriceText.text = $"合計: G {totalSelectedPrice} / 所持: G {currentGold}";
    }

    private void OnCardBuyButtonClick()
    {
        if (selectedCardIds.Count == 0) return;
        if (totalSelectedPrice > currentGold) return;

        OnCardBuyConfirmed?.Invoke(
            new List<CardController>(selectedCards),
            totalSelectedPrice
                );
        // 選択済みカードのSelectedPanelを非表示にしてリセット
        foreach (CardController card in selectedCards)
            SetCardSelectedPanel(card, false);

        // 選択状態をリセット
        selectedCardIds.Clear();
        selectedCards.Clear();
        totalSelectedPrice = 0;
        selectedCardId = -1;
        selectedCardPrice = 0;
        cardBuyButton.interactable = false;
        cardInfoPanel.SetActive(false);
        UpdateTotalPriceText();
    }

    public void CloseInfoPanel()
    {
        selectedCardId = -1;
        selectedCardPrice = 0;
        cardBuyButton.interactable = selectedCardIds.Count > 0;
        cardInfoPanel.SetActive(false);
    }

    public void PauseShopMode()
    {
        isShopMode = false;
    }

    public void ResumeShopMode()
    {
        isShopMode = true;
    }
}
//所持金以上は変えないこと　選択額を超えないこと　購入処理