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

        int deckCardNum = GameDataHolder.Instance.EditingDeckCounts[No - 1];
        int cardListNum = GameDataHolder.Instance.DisplayPossessionCard[No - 1];

        if (isDeck)
        {
            // デッキ側：枚数表示のみ。「編成中」オーバーレイは常に非表示にする
            bool showCount = deckCardNum > 0;
            countText.gameObject.SetActive(showCount);
            countPanel.SetActive(showCount);
            if (showCount)
                countText.text = $"×{deckCardNum}";

            selectedText.gameObject.SetActive(false);
            selectedPanel.SetActive(false);
        }
        else
        {
            // 所持一覧側：残り枚数を表示し、所持しているカードは「編成中」オーバーレイも合わせて表示する
            bool showCount = cardListNum > 0 || deckCardNum > 0;
            countText.gameObject.SetActive(showCount);
            countPanel.SetActive(showCount);
            if (showCount)
                countText.text = $"×{cardListNum}";

            // デッキに入っているかどうかに関わらず、所持しているカードは開いた直後から表示する
            selectedText.gameObject.SetActive(showCount);
            selectedPanel.SetActive(showCount);
            if (showCount)
                selectedText.text = $"編成中\n×{deckCardNum}";
        }

    }

    /// <summary>
    /// デッキ編成画面の追加、減少時のカードの表示更新
    /// </summary>
    /// <param name="isDeck"></param>
    public void RefreshCardView(bool isDeck, int cardNo, int deckNum, OutGameCardList outGameCardList)
    {
        // テキストの書き換えだけでSetActiveを呼んでいなかったため、
        // 元々非表示だったパネルはテキストが更新されても見た目に反映されなかった。
        // 表示制御込みのRefreshViewを呼ぶようにする。
        RefreshView(isDeck, deckNum);
        outGameCardList.RefreshView(isDeck, deckNum);
    }


    public void DeckAddPanelOn()
    {
        countText.gameObject.SetActive(true);
        countPanel.SetActive(true);

        // このカードオブジェクトはデッキ側の表示として再利用されるため、
        // 一覧側で使われる「編成中」オーバーレイは非表示にする
        selectedText.gameObject.SetActive(false);
        selectedPanel.SetActive(false);
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
