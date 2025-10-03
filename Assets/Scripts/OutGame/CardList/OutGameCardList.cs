using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OutGameCardList : MonoBehaviour
{

    private int no;
    private string cardName;
    private Sprite cardSprite;
    private int hp;
    private int at;
    private int effectDmg;
    private int effectHeal;
    private int cost;
    private string description;

    [SerializeField] Image image;
    [SerializeField] TextMeshProUGUI cardNameText;
    [SerializeField] TextMeshProUGUI cardAttackText;
    [SerializeField] TextMeshProUGUI cardHpText;
    [SerializeField] TextMeshProUGUI cardCostText;


    public int No { get { return no; } set { no = value; } }
    public string CardName { get { return cardName; } set { cardName = value; } }
    public Sprite CardSprite { get { return cardSprite; } set { cardSprite = value; } }
    public int Hp { get { return hp; } set { hp = value; } }
    public int At { get { return at; } set { at = value; } }
    public int EffectDmg { get { return effectDmg; } set { effectDmg = value; } }
    public int EffectHeal { get { return effectHeal; } set { effectHeal = value; } }
    public int Cost { get { return cost; } set { cost = value; } }
    public string Description { get { return description; } set { description = value; } }

    public void SetData(CardEntity entity)
    {
        No = entity.no;
        CardName = entity.name;
        Hp = entity.hp;
        At = entity.at;
        CardSprite = entity.icon;
        EffectDmg = entity.effectDmg;
        EffectHeal = entity.effectHeal;
        Cost = entity.cost;
        Description = entity.description;

        image.sprite = entity.icon;
        cardNameText.text = entity.name;
        cardAttackText.text = entity.at.ToString();
        cardHpText.text = entity.hp.ToString();
        cardCostText.text = entity.cost.ToString();
    }
}
