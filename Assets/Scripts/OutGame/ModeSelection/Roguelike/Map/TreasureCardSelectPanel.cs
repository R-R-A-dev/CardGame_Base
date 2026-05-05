using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class TreasureCardSelectPanel : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private CardController cardController;
    [SerializeField] private Button cardGetButton;

    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHealthText;
    [SerializeField] private TextMeshProUGUI cardDescriptionText;
    [SerializeField] private TextMeshProUGUI selectedCountText; // 選択枚数表示

    [SerializeField] private TreasureUI treasureUI;

    // TreasureモードのフラグとSOで設定した選択可能枚数
    private bool isTreasureMode = false;
    private int maxSelectCount = 1;

    private List<int> selectedCardIds = new List<int>();
    private List<CardController> selectedCards = new List<CardController>(); // 選択中カードの参照

    private void Start()
    {
        cardGetButton.interactable = false;
        cardGetButton.onClick.RemoveAllListeners();
        cardGetButton.onClick.AddListener(OnCardGetButtonClick);
    }

    // TreasureUIからフラグをONにして初期化
    public void SetTreasureMode(bool isActive, int selectCount)
    {
        isTreasureMode = isActive;
        maxSelectCount = selectCount;
        selectedCardIds.Clear();
        selectedCards.Clear();
        cardGetButton.interactable = false;
        cardInfoPanel.SetActive(false);
        UpdateSelectedCountText();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        CardController card = clickedObject.GetComponent<CardController>();

        if (card == null) return;
        if (!isTreasureMode) return;
        
        // 右クリック：詳細パネル表示（モード問わず）
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OpenInfoPanel(card);
            return;
        }

        // 左クリック：TreasureModeの時のみカード選択
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 既に選択済みか判定（CardControllerの参照で比較）
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
            else if (selectedCardIds.Count < maxSelectCount)
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

            UpdateSelectedCountText();
            cardGetButton.interactable = selectedCardIds.Count == maxSelectCount;
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

    private void UpdateSelectedCountText()
    {
        selectedCountText.text = $"{selectedCardIds.Count} / {maxSelectCount}枚選択中";
    }

    public void OnCardGetButtonClick()
    {
        if (selectedCardIds.Count != maxSelectCount) return;

        treasureUI.OnCardSelected(selectedCardIds);

        // リセット
        SetTreasureMode(false, maxSelectCount);
    }

    public void CloseInfoPanel()
    {
        cardInfoPanel.SetActive(false);
    }
}