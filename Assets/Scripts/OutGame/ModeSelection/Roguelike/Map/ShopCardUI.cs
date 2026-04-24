using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopCardUI : MonoBehaviour
{
    [Header("カード表示")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI cardCostText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHPText;
    [SerializeField] private Image cardIllust;

    private int cardId;

    public void Setup(int id)
    {
        cardId = id;
        CardEntity card = CardDatabase.LoadCardByID(cardId);

        cardNameText.text = card.name;
        cardCostText.text = card.cost.ToString();
        cardAttackText.text = card.at.ToString();
        cardHPText.text = card.hp.ToString();
        cardIllust.sprite = card.icon;
    }

    public int GetCardId() => cardId;
}