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
            card.transform.SetParent(deckContent.transform);
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

}
