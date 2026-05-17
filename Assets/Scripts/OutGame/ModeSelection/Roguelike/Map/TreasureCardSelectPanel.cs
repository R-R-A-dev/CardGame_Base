using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class TreasureCardSelectPanel : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private CardController cardController;
    [SerializeField] private Button closeButton; // 取得ボタンを閉じるボタンに変更

    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHealthText;
    [SerializeField] private TextMeshProUGUI cardDescriptionText;

    private bool isTreasureMode = false;

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);
        cardInfoPanel.SetActive(false);
    }

    public void SetTreasureMode(bool isActive)
    {
        isTreasureMode = isActive;
        cardInfoPanel.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isTreasureMode) return;

        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        CardController card = clickedObject.GetComponent<CardController>();

        if (card == null) return;

        // 右クリック：詳細パネル表示
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OpenInfoPanel(card);
            return;
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

    private void OnCloseButtonClick()
    {
        cardInfoPanel.SetActive(false);
        SetTreasureMode(false);
        // TreasureUIに閉じるを通知
        OnClosed?.Invoke();
    }

    // TreasureUIへの通知
    public System.Action OnClosed;

    public void CloseInfoPanel()
    {
        cardInfoPanel.SetActive(false);
    }
}