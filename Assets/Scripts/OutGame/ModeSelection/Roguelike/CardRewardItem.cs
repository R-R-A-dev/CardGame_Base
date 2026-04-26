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

    [Header("クリック判定")]
    [SerializeField] private Button cardButton; // カード全体のButton

    private int cardId;
    private System.Action<int> onClicked;

    public void Setup(int id, System.Action<int> onClickedCallback)
    {
        cardId = id;
        onClicked = onClickedCallback;

        CardEntity card = CardDatabase.LoadCardByID(cardId);
        cardNameText.text = card.name;
        cardCostText.text = $"コスト: {card.cost}";
        cardAttackText.text = $"攻撃: {card.at}";
        cardHPText.text = $"HP: {card.hp}";
        cardIllust.sprite = card.icon;

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => onClicked?.Invoke(cardId));
    }
}