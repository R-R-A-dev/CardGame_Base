using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardRewardItem : MonoBehaviour
{
    [Header("カード表示")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI cardCostText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHPText;
    [SerializeField] private Image cardIllust;

    [Header("ボタン")]
    [SerializeField] private Button selectButton;   // カード選択ボタン
    [SerializeField] private Button detailButton;   // 詳細表示ボタン

    [Header("選択状態")]
    [SerializeField] private GameObject selectedOverlay; // 選択中のオーバーレイ

    [Header("クリック判定")]
    [SerializeField] private Button cardButton; // カード全体のButton

    private int cardId;
    private System.Action<int> onClicked;
    private System.Action<int, CardRewardItem> onSelected;
    private System.Action<int> onDetailClicked;

    public void Setup(int id,
        System.Action<int, CardRewardItem> onSelectedCallback,
        System.Action<int> onDetailCallback)
    {
        cardId = id;
        onSelected = onSelectedCallback;
        onDetailClicked = onDetailCallback;

        CardEntity card = CardDatabase.LoadCardByID(cardId);
        cardNameText.text = card.name;
        cardCostText.text = $"コスト: {card.cost}";
        cardAttackText.text = $"攻撃: {card.at}";
        cardHPText.text = $"HP: {card.hp}";
        cardIllust.sprite = card.icon;


        selectedOverlay.SetActive(false);

        selectButton.onClick.RemoveAllListeners();
        detailButton.onClick.RemoveAllListeners();

        selectButton.onClick.AddListener(() => onSelected?.Invoke(cardId, this));
        detailButton.onClick.AddListener(() => onDetailClicked?.Invoke(cardId));
    }

    public void SetSelected(bool isSelected)
    {
        selectedOverlay.SetActive(isSelected);
    }
}