using System.Collections.Generic;
using UnityEngine;

public class DeckBuilderUI : MonoBehaviour
{

    [SerializeField] GameObject cardPanel;
    [SerializeField] GameObject cardListContent;
    [SerializeField] GameObject deckContent;
    [SerializeField] CardListItem cardListItem;
    [SerializeField] DeckCardItem deckListItem;

    void Start()
    {
        DisplayCardList();
        DisplayDeck();
    }

    void Update()
    {

    }

    /// <summary>
    /// カード一覧表示
    /// </summary>
    public void DisplayCardList()
    {
        cardListItem.CardSetUp(cardPanel, cardListContent.transform);
        //カードの一覧と所持カードのIDを比較して、所持しているカードを表示する
        //パネルと一緒に表示させる
    }

    /// <summary>
    /// デッキ表示表示
    /// </summary>
    public void DisplayDeck()
    {
        deckListItem.CardSetUp(cardPanel, deckContent.transform, 0);
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
}
