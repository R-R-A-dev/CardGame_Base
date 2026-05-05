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
    [SerializeField] private Button closeInfoPanelButton; // 詳細パネルを閉じるボタン

    private bool isInfoPanelOpen = false;
    public bool IsInfoPanelOpen => cardInfoPanel.activeSelf;

    // ShopUIへの通知
    public System.Action<int, int> OnCardBuyConfirmed;

    private List<int> selectedCardIds = new List<int>();
    private List<CardController> selectedCards = new List<CardController>();

    // フラグ
    private bool isShopMode = false;

    private int selectedCardId = -1;
    private int selectedCardPrice = 0;

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

    // ShopUIから呼ぶ・フラグON
    public void SetShopMode(bool isActive, Dictionary<CardController, int> priceMap)
    {
        isShopMode = isActive;
        cardPriceMap = priceMap ?? new Dictionary<CardController, int>();
        selectedCardId = -1;
        selectedCardPrice = 0;

        cardBuyButton.interactable = false;
        cardInfoPanel.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {

        // フラグがOFFなら処理しない
        if (!isShopMode) return;
        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        CardController card = clickedObject.GetComponentInParent<CardController>();

        // カード以外をクリックした場合
        if (card == null) return;


        // 右クリック：詳細パネル表示
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OpenInfoPanel(card);
            return;
        }

        // 左クリック：カード選択
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            selectedCardId = card.model.no;
            selectedCardPrice = cardPriceMap.ContainsKey(card)
                ? cardPriceMap[card] : 0;

            cardBuyButton.interactable = true;
            if (selectedCards.Contains(card))
            {
                // 選択解除
                Transform[] allChildren = card.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    if (child.name == "SelectedPanel")
                    {
                        child.gameObject.SetActive(false);
                        break;
                    }
                }
                selectedCardIds.Remove(card.model.no);
                selectedCards.Remove(card);
            }
            else if (selectedCardIds.Count < 100)
            {
                // 選択追加
                Transform[] allChildren = card.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    if (child.name == "SelectedPanel")
                    {
                        child.gameObject.SetActive(true);
                        break;
                    }
                }
                selectedCardIds.Add(card.model.no);
                selectedCards.Add(card);
            }

        }
    }

    private void OpenInfoPanel(CardController card)
    {
        cardInfoPanel.SetActive(true);
        isInfoPanelOpen = true;
        cardController.Init(card.model.no, false);
        cardNameText.text = card.model.name;
        cardAttackText.text = card.model.at.ToString();
        cardHealthText.text = card.model.hp.ToString();
        cardDescriptionText.text = card.model.description;
        priceText.text = $"G {selectedCardPrice}";
    }

    private void OnCardBuyButtonClick()
    {
        if (selectedCardId == -1) return;

        OnCardBuyConfirmed?.Invoke(selectedCardId, selectedCardPrice);

        // 選択状態をリセット
        selectedCardId = -1;
        selectedCardPrice = 0;
        cardBuyButton.interactable = false;
        cardInfoPanel.SetActive(false);
    }

    public void CloseInfoPanel()
    {
        // 選択状態もリセット
        selectedCardId = -1;
        selectedCardPrice = 0;
        isInfoPanelOpen = false;
        cardBuyButton.interactable = false;
        cardInfoPanel.SetActive(false);
    }
}

//所持金以上は変えないこと　選択額を超えないこと　購入処理