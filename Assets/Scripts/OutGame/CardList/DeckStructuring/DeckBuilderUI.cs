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
        DisplayCardList();
        DisplayDeck();
        RefreshAllCardUI();
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
    public void AddDeckCard(OutGameCardList card, int cardNo)
    {
        deckListItem.AddDeckCard(cardNo);
        cardListItem.ReturnCardList(cardNo);
        foreach (OutGameCardList outGameCardList in cardListContent.GetComponentsInChildren<OutGameCardList>())
        {
            if(cardNo == outGameCardList.No)
            {
                //デッキから減らす
                return;
            }
        }

        foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
        {
            if(cardNo == outGameCardList.No)
            {
                //一覧から増やす
                return;
            }
        }
        //指定されたカードの番号からデッキと一覧から増減情報を更新
        card.transform.SetParent(deckContent.transform);

    }

    /// <summary>
    /// デッキからドラッグ＆ドロップでカード一覧に戻す
    /// </summary>
    /// <param name="card"></param>
    public void ReturnCardList(OutGameCardList card)
    {

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
