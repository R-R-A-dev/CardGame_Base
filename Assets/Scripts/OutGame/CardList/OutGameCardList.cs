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

    public Image image;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardAttackText;
    public TextMeshProUGUI cardHpText;
    public TextMeshProUGUI cardCostText;


    [SerializeField] private TextMeshProUGUI selectedText;
    [SerializeField] private GameObject selectedPanel;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private GameObject countPanel;



    public int No { get { return no; } set { no = value; } }
    public string CardName { get { return cardName; } set { cardName = value; } }
    public Sprite CardSprite { get { return cardSprite; } set { cardSprite = value; } }
    public int Hp { get { return hp; } set { hp = value; } }
    public int At { get { return at; } set { at = value; } }
    public int EffectDmg { get { return effectDmg; } set { effectDmg = value; } }
    public int EffectHeal { get { return effectHeal; } set { effectHeal = value; } }
    public int Cost { get { return cost; } set { cost = value; } }
    public string Description { get { return description; } set { description = value; } }

    /// <summary>
    /// 一覧表示時のデータセット
    /// </summary>
    /// <param name="entity"></param>
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

    /// <summary>
    /// ドラッグ時の生成されたカードのデータセット
    /// </summary>
    /// <param name="card"></param>
    public void DragCardGen(OutGameCardList card)
    {
        No = card.No;
        CardName = card.CardName;
        Hp = card.Hp;
        At = card.At;
        CardSprite = card.CardSprite;
        Cost = card.Cost;
        Description = card.Description;

        image.sprite = card.image.sprite;
        cardNameText.text = card.cardNameText.text;
        cardAttackText.text = card.cardAttackText.text;
        cardHpText.text = card.cardHpText.text;
        cardCostText.text = card.cardCostText.text;
    }

    /// <summary>
    /// デッキ編成画面のカードの表示更新
    /// </summary>
    public void RefreshView(bool isDeck, int deckNum)
    {
        //所持カードデータとデッキデータを取得して比較
        //一覧に複数ある場合は枚数を表示、デッキにある場合は編成中表示
        //デッキにある場合は枚数を表示のみ
        //自分の番号とデッキと一覧の比較からパネルの表示をする

        int deckCardNum = CardListData.Decks[deckNum][No - 1];
        int cardListNum = CardListData.PossessionCard[No - 1];

        if (isDeck && deckCardNum > 0)
        {
            countText.gameObject.SetActive(true);
            countPanel.SetActive(true);
            countText.text = $"×{deckCardNum}";
        }
        else if (cardListNum > 0)
        {
            countText.gameObject.SetActive(true);
            countPanel.SetActive(true);
            countText.text = $"×{cardListNum - deckCardNum}";
            if (deckCardNum > 0)
            {
                selectedText.gameObject.SetActive(true);
                selectedPanel.SetActive(true);
                selectedText.text = $"編成中\n×{deckCardNum}";
            }
        }

        //if (isDeck && deckCardNum > 0)
        //{
        //    countText.gameObject.SetActive(true);
        //    countPanel.SetActive(true);
        //    countText.text = $"×{deckCardNum}";
        //}
        //else if (cardListNum > 0)
        //{
        //    countText.gameObject.SetActive(true);
        //    countPanel.SetActive(true);
        //    countText.text = $"×{cardListNum - deckCardNum}";
        //    if (deckCardNum > 0)
        //    {
        //        selectedText.gameObject.SetActive(true);
        //        selectedPanel.SetActive(true);
        //        selectedText.text = $"編成中\n×{deckCardNum}";
        //    }
        //    if (deckCardNum > 0 && cardListNum > 0)
        //    {
        //        CardListData.PossessionCard[No - 1] = cardListNum - deckCardNum;
        //    }
        //}

    }

    /// <summary>
    /// デッキ編成画面の追加、減少時のカードの表示更新
    /// </summary>
    /// <param name="isDeck"></param>
    public void RefreshCardView(bool isDeck, int cardNo, int deckNum, OutGameCardList outGameCardList)
    {
        int deckCardNum = CardListData.Decks[deckNum][No - 1];
        int cardListNum = CardListData.PossessionCard[No - 1];


        if (isDeck)
        {
            outGameCardList.countText.text = $"×{deckCardNum}";
            outGameCardList.selectedText.text = $"編成中\n×{deckCardNum}";
        }
        else
        {
            outGameCardList.countText.text = $"×{cardListNum}";
        }
    }


    public void DeckAddPanelOn()
    {
        countText.gameObject.SetActive(true);
        countPanel.SetActive(true);
    }

    /// <summary>
    /// パネルオフ
    /// </summary>
    public void PanelOff()
    {
        selectedText.gameObject.SetActive(false);
        selectedPanel.SetActive(false);
        countText.gameObject.SetActive(false);
        countPanel.SetActive(false);
    }

}
