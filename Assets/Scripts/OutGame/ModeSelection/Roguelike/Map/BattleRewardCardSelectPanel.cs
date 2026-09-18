using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BattleRewardCardSelectPanel : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private CardController cardController;
    [SerializeField] private Button cardGetButton;

    [SerializeField] private Text cardNameText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHealthText;
    [SerializeField] private Text cardDescriptionText;
    [SerializeField] private TextMeshProUGUI selectedCountText;

    // BattleRewardUIへの通知（相互依存を避けるためAction使用）
    public System.Action<List<int>> OnCardsConfirmed;

    public bool IsInfoPanelOpen => cardInfoPanel.activeSelf;

    private bool isRewardMode = false;
    private int maxSelectCount = 1;

    private List<int> selectedCardIds = new List<int>();
    private List<CardController> selectedCards = new List<CardController>();

    private void Start()
    {
        cardGetButton.interactable = false;
        cardGetButton.onClick.RemoveAllListeners();
        cardGetButton.onClick.AddListener(OnCardGetButtonClick);
        cardInfoPanel.SetActive(false);
    }

    public void SetRewardMode(bool isActive, int selectCount)
    {
        isRewardMode = isActive;
        maxSelectCount = selectCount;
        selectedCardIds.Clear();
        selectedCards.Clear();
        cardGetButton.interactable = false;
        cardInfoPanel.SetActive(false);
        UpdateSelectedCountText();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isRewardMode) return;

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
            if (selectedCards.Contains(card))
            {
                // 選択解除
                SetCardSelectedPanel(card, false);
                selectedCardIds.Remove(card.model.no);
                selectedCards.Remove(card);
            }
            else if (selectedCardIds.Count < maxSelectCount)
            {
                // 選択追加
                SetCardSelectedPanel(card, true);
                selectedCardIds.Add(card.model.no);
                selectedCards.Add(card);
            }

            UpdateSelectedCountText();
            cardGetButton.interactable = selectedCardIds.Count == maxSelectCount;
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
        cardInfoPanel.SetActive(true);
        cardController.Init(card.model.no, false);
        cardNameText.text = card.model.name;
        cardAttackText.text = card.model.at.ToString();
        cardHealthText.text = card.model.hp.ToString();
        cardDescriptionText.text = card.model.description;
    }

    private void UpdateSelectedCountText()
    {
        selectedCountText.text = $"{selectedCardIds.Count} / {maxSelectCount}枚選択中";
    }

    private void OnCardGetButtonClick()
    {
        TryConfirmSelection();
    }

    // 選択が規定枚数に達していれば確定する。決定ボタン・閉じるボタン両方から呼ばれる
    public bool TryConfirmSelection()
    {
        if (selectedCardIds.Count != maxSelectCount) return false;

        OnCardsConfirmed?.Invoke(new List<int>(selectedCardIds));
        SetRewardMode(false, maxSelectCount);
        return true;
    }

    public void CloseInfoPanel()
    {
        cardInfoPanel.SetActive(false);
    }
}
