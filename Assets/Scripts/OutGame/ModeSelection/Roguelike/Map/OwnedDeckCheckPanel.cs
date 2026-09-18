using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class OwnedDeckCheckPanel : MonoBehaviour, IPointerDownHandler
{
    [Header("カード一覧")]
    [SerializeField] private List<CardController> cardList;
    [SerializeField] private Button closeButton; // 一覧パネルを閉じるボタンのみ保持
    [SerializeField] private List<TextMeshProUGUI> cardNum;

    [Header("詳細パネル")]
    [SerializeField] private GameObject cardListPanel;
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private CardController cardController;
    [SerializeField] private Text cardNameText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHealthText;
    [SerializeField] private Text cardDescriptionText;
    [SerializeField] private Button closeInfoPanelButton;

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    // 開閉時に呼び出し元へ通知（モードの一時停止・再開用）
    public System.Action OnOpened;
    public System.Action OnClosed;

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeInfoPanelButton.onClick.RemoveAllListeners();

        closeButton.onClick.AddListener(OnCloseButtonClick);
        closeInfoPanelButton.onClick.AddListener(CloseInfoPanel);

        cardInfoPanel.SetActive(false);
        cardListPanel.SetActive(false);
    }

    // 各PanelのボタンのonClickから直接呼ぶ公開メソッド
    public void Open()
    {
        isOpen = true;
        cardListPanel.SetActive(true);
        cardInfoPanel.SetActive(false);

        RoguelikeGameState gameState = RoguelikeManager.Instance != null ? RoguelikeManager.Instance.GameState : null;
        CardDisplay(gameState != null ? gameState.CurrentDeck : new List<int>());

        // 呼び出し元（Shop/Treasure等）のモードを止める
        OnOpened?.Invoke();
    }

    private void CardDisplay(List<int> deckCardIds)
    {
        foreach (CardController card in cardList)
            card.gameObject.SetActive(false);

        // 同じIDが何枚あるかを集計
        Dictionary<int, int> idCountMap = new Dictionary<int, int>();
        foreach (int id in deckCardIds)
        {
            if (idCountMap.ContainsKey(id))
                idCountMap[id]++;
            else
                idCountMap[id] = 1;
        }

        // 重複を除いたID一覧（表示するカードの種類）
        List<int> uniqueIds = new List<int>(idCountMap.Keys);

        for (int i = 0; i < uniqueIds.Count; i++)
        {
            if (i >= cardList.Count) break;

            int cardId = uniqueIds[i];
            cardList[i].gameObject.SetActive(true);
            cardList[i].Init(cardId, false);

            // 対応するcardNumに枚数を表示
            if (i < cardNum.Count)
                cardNum[i].text = idCountMap[cardId].ToString();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isOpen) return;

        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        CardController card = clickedObject.GetComponentInParent<CardController>();

        if (card == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OpenInfoPanel(card);
        }
    }

    private void OpenInfoPanel(CardController card)
    {
        cardInfoPanel.SetActive(true);
        cardController.Init(card.model.no, false);
        cardNameText.text = card.model.name;
        cardAttackText.text = card.model.at.ToString();
        cardHealthText.text = card.model.hp.ToString();
        cardDescriptionText.text = card.model.description;
    }

    public void CloseInfoPanel()
    {
        cardInfoPanel.SetActive(false);
    }

    private void OnCloseButtonClick()
    {
        Close();
    }

    private void Close()
    {
        isOpen = false;
        cardInfoPanel.SetActive(false);
        cardListPanel.SetActive(false);

        OnClosed?.Invoke();
    }
}