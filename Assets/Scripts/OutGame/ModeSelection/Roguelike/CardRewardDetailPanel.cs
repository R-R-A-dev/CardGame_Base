using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardRewardDetailPanel : MonoBehaviour
{
    [Header("詳細表示")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI cardCostText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHPText;
    [SerializeField] private TextMeshProUGUI cardEffectText;
    [SerializeField] private Image cardIllust;

    [Header("閉じる")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton; // 背景クリックで閉じる

    private void Start()
    {
        closeButton.onClick.AddListener(Close);
        backgroundButton.onClick.AddListener(Close);
        gameObject.SetActive(false);
    }

    public void Open(int cardId)
    {
        CardEntity card = CardDatabase.LoadCardByID(cardId);
        cardNameText.text = card.name;
        cardCostText.text = $"コスト: {card.cost}";
        cardAttackText.text = $"攻撃: {card.at}";
        cardHPText.text = $"HP: {card.hp}";
        cardIllust.sprite = card.icon;
        cardEffectText.text = card.description;

        gameObject.SetActive(true);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}