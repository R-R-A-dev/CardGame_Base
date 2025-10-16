using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DeckBuilderUI : MonoBehaviour
{
    [Header("ドロップゾーン")]
    [SerializeField] private DeckDropZone deckDropZone;
    [SerializeField] private CardListDropZone cardListDropZone;

    [Header("カード詳細")]
    [SerializeField] private CardDetailPanel cardDetailPanel;

    [Header("カード一覧・デッキ表示")]
    [SerializeField] GameObject cardPanel;
    [SerializeField] GameObject cardListContent;
    [SerializeField] GameObject deckContent;
    [SerializeField] CardListItem cardListItem;
    [SerializeField] DeckCardItem deckListItem;
    [SerializeField] GameObject cardPool;



    void Start()
    {
        DisplayDeck();
        DisplayCardList();
        RefreshAllCardUI();
        PrepareDeck();
        DeckBuilderManager.Instance.deckStatisticsUI.
            RefreshStatistics(DeckBuilderManager.Instance.deckNum);
        SortCard();
    }

    void Update()
    {

    }

    /// <summary>
    /// カード一覧表示
    /// </summary>
    public void DisplayCardList()
    {
        cardListItem.CardSetUp(cardListContent.transform);
        //カードの一覧と所持カードのIDを比較して、所持しているカードを表示する
        //パネルと一緒に表示させる
    }

    /// <summary>
    /// デッキ表示表示
    /// </summary>
    public void DisplayDeck()
    {
        deckListItem.CardSetUp(deckContent.transform, DeckBuilderManager.Instance.deckNum);
    }

    /// <summary>
    /// フィルター変更時
    /// </summary>
    /// <param name="filterIndex"></param>
    public void OnFilterChanged(int filterIndex)
    {

    }

    /// <summary>
    /// 検索入力変更時
    /// </summary>
    /// <param name="keyword"></param>
    public void OnSearchInputChanged(string keyword)
    {

    }

    /// <summary>
    /// カード一覧からドラッグ＆ドロップでデッキに追加
    /// </summary>
    /// <param name="card"></param>
    public void AddDeckCard(OutGameCardList card, int cardNo, CardDragHandler cardDragHandler)
    {
        //デッキと所持一覧のリストに追加・削除
        deckListItem.AddDeckCard(cardNo);
        cardListItem.ReturnCardList(cardNo);


        //デッキと所持一覧のカードUIを更新
        foreach (OutGameCardList outGameCardList in cardListContent.GetComponentsInChildren<OutGameCardList>())
        {
            if (cardNo == outGameCardList.No)
            {
                card.RefreshCardView(false, cardNo,
                DeckBuilderManager.Instance.deckNum, outGameCardList);
                break;
            }
        }

        foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
        {
            if (cardNo == outGameCardList.No)
            {
                card.RefreshCardView(true, cardNo,
                DeckBuilderManager.Instance.deckNum, outGameCardList);
                break;
            }
        }
        //デッキに二枚目であればオブジェクトプールから取得して追加
        //一枚目であればそのまま移動
        if (CardListData.Decks[DeckBuilderManager.Instance.deckNum][cardNo - 1] >= 2)
        {
            DeckBuilderManager.Instance.deckBuilderUI.PoolCard(card.gameObject);
        }
        else if (CardListData.Decks[DeckBuilderManager.Instance.deckNum][cardNo - 1] == 1)
        {
            int newCardCost = card.Cost;

            // デッキ内のすべてのカードを取得
            OutGameCardList[] deckCards = deckContent.GetComponentsInChildren<OutGameCardList>();

            int insertIndex = deckCards.Length; // デフォルトは末尾

            // コスト順に挿入位置を探す
            for (int i = 0; i < deckCards.Length; i++)
            {
                if (newCardCost < deckCards[i].Cost)
                {
                    insertIndex = i;
                    break;
                }
            }
            // 指定した位置に Transform を挿入
            card.transform.SetParent(deckContent.transform);
            card.transform.SetSiblingIndex(insertIndex);

            foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
            {
                if (cardNo == outGameCardList.No)
                {
                    outGameCardList.DeckAddPanelOn();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// デッキからドラッグ＆ドロップでカード一覧に戻す
    /// </summary>
    /// <param name="card"></param>
    public void ReturnCardList(OutGameCardList card, int cardNo)
    {
        cardListItem.AddCardList(cardNo);
        deckListItem.ReturnListCard(cardNo);


        //デッキと所持一覧のカードUIを更新
        foreach (OutGameCardList outGameCardList in cardListContent.GetComponentsInChildren<OutGameCardList>())
        {
            if (cardNo == outGameCardList.No)
            {
                card.RefreshCardView(false, cardNo,
                DeckBuilderManager.Instance.deckNum, outGameCardList);
                break;
            }
        }

        foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
        {
            if (cardNo == outGameCardList.No)
            {
                card.RefreshCardView(true, cardNo,
                DeckBuilderManager.Instance.deckNum, outGameCardList);
                break;
            }
        }


        if (CardListData.Decks[DeckBuilderManager.Instance.deckNum][cardNo - 1] == 0)
        {
            foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
            {
                if (cardNo == outGameCardList.No)
                {
                    DeckBuilderManager.Instance.deckBuilderUI.PoolCard(outGameCardList.gameObject);
                    break;
                }
            }
        }
        DeckBuilderManager.Instance.deckBuilderUI.PoolCard(card.gameObject);
    }

    /// <summary>
    /// デッキと所持カードの枚数を更新して、カードUIを更新
    /// </summary>
    public void RefreshAllCardUI()
    {
        foreach (OutGameCardList outGameCardList in cardListContent.GetComponentsInChildren<OutGameCardList>())
            outGameCardList.RefreshView(false, DeckBuilderManager.Instance.deckNum);
        foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
            outGameCardList.RefreshView(true, DeckBuilderManager.Instance.deckNum);
    }

    /// <summary>
    /// 一覧からデッキ分を減算して準備
    /// </summary>
    void PrepareDeck()
    {
        cardListItem.PrepareDeck();
    }

    void SortCard()
    {
        // 所持カードリストをソート
        SortByCost(cardListContent.transform);

        // デッキリストをソート
        SortByCost(deckContent.transform);
    }

    void SortByCost(Transform parent)
    {
        // 子オブジェクトからOutGameCardListを全部取得
        List<OutGameCardList> cards = new List<OutGameCardList>(parent.GetComponentsInChildren<OutGameCardList>());

        // Costで昇順ソート
        cards.Sort((a, b) => a.Cost.CompareTo(b.Cost));

        // 並び替えた順にHierarchy上の位置を更新
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.SetSiblingIndex(i);
        }
    }

    /// <summary>
    /// カードのオブジェクトプールから非アクティブなカードを取得
    /// </summary>
    /// <returns></returns>
    public GameObject GetCardPool()
    {
        if (cardPool.transform.childCount == 0)
            return null;

        // 子オブジェクトを順にチェック
        for (int i = 0; i < cardPool.transform.childCount; i++)
        {
            GameObject child = cardPool.transform.GetChild(i).gameObject;
            // 非アクティブならそれを返す
            if (!child.activeSelf)
            {
                return child;
            }
        }
        return null;
    }

    /// <summary>
    /// オブジェクトプールにカードを戻す
    /// </summary>
    /// <param name="card"></param>
    public void PoolCard(GameObject card)
    {
        card.SetActive(false);
        card.transform.SetParent(cardPool.transform);
    }

    public Vector3 GetCardPosToDeck(int cardNo, int cost)
    {
        //既にデッキにあるカードならその位置を返す 
        Vector3 movePos = Vector3.zero;
        foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
        {
            if (cardNo == outGameCardList.No)
            {
                movePos = outGameCardList.transform.position;
                return movePos;
            }
        }

        float x = 110;
        float y = 673.5f;
        float interval = 220;
        //0番目の位置 x:110 y:673.5
        //間の距離 220

        //デッキ内にないカードならコスト順に挿入位置を探す
        OutGameCardList[] deckCards = deckContent.GetComponentsInChildren<OutGameCardList>();

        int insertIndex = deckCards.Length; // デフォルトは末尾
        if (deckCards.Length != 0)
        {
            // コスト順に挿入位置を探す
            for (int i = 0; i < deckCards.Length; i++)
            {
                if (cost < deckCards[i].Cost)
                {
                    insertIndex = i;
                    return movePos = deckCards[i].transform.position;
                }
            }
        }

        //デッキにカードが一枚もない場合、またはコストが一番高い場合は最後尾に追加
        if (movePos == Vector3.zero && deckCards.Length != 0)
        {
            movePos = deckCards[deckCards.Length - 1].transform.position;
            movePos = new Vector2(movePos.x + interval, movePos.y);
            return movePos;
        }

        // 指定した位置に Transform を挿入
        //card.transform.SetParent(deckContent.transform);
        //card.transform.SetSiblingIndex(insertIndex);
        //インデックスから場所を取得して目的地を出す
        if (movePos == Vector3.zero && deckCards.Length == 0)
        {
            movePos = new Vector2(x, y);
        }
        return movePos;
    }

    public Vector3 GetCardPosToList(int cardNo)
    {
        Vector3 movePos = Vector3.zero;
        foreach (OutGameCardList outGameCardList in cardListContent.GetComponentsInChildren<OutGameCardList>())
        {
            if (cardNo == outGameCardList.No)
            {
                movePos = outGameCardList.transform.position;
                return movePos;
            }
        }
        return movePos;
    }
}
