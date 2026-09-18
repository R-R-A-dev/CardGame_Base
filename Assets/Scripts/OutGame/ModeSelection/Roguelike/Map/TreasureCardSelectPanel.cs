using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class TreasureCardSelectPanel : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private CardController cardController;
    [SerializeField] private Button closeInfoPanelButton;

    [SerializeField] private Text cardNameText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHealthText;
    [SerializeField] private Text cardDescriptionText;

    public bool IsInfoPanelOpen => cardInfoPanel.activeSelf;

    private bool isTreasureMode = false;

    private void Start()
    {
        closeInfoPanelButton.onClick.RemoveAllListeners();
        closeInfoPanelButton.onClick.AddListener(CloseInfoPanel);
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

    public void CloseInfoPanel()
    {
        cardInfoPanel.SetActive(false);
    }


    public void PauseTreasureMode()
    {
        isTreasureMode = false;
    }

    public void ResumeTreasureMode()
    {
        isTreasureMode = true;
    }
}