using System.Collections.Generic;
using UnityEngine;

public class DeckBuilderManager : MonoBehaviour
{
    CardDatabase cardDatabase;
    CardListData cardListData;

    [SerializeField] GameObject cardPanel;
    [SerializeField] GameObject cardListContent;
    [SerializeField] GameObject deckContent;

    void Start()
    {
        DisplayCardList();
    }

    void Update()
    {

    }
    //DeckBuilderUIへ移動
    /// <summary>
    /// カード一覧表示
    /// </summary>
    void DisplayCardList()
    {
        //全カードデータと所持カードデータを比較して
        //所持しているカードとその枚数を取得する
        CardEntity[] entitys = CardDatabase.LoadAllCards();
        CardListData.PossessionCard = new List<int>() {1,1,1,1};
        for (int i = 0; i < CardListData.PossessionCard.Count; i++)
        {
            if (entitys[i].no== i + 1)
            {
                //カード番号：entitys[i].no
                //カードの枚数：CardListData.PossessionCard[i]
                //カードパネルを生成して、カード情報をセットしカードも生成する
                GameObject panel = Instantiate(cardPanel, cardListContent.transform);
            }
        }
        //カードの一覧と所持カードのIDを比較して、所持しているカードを表示する
        //パネルと一緒に表示させる
    }

    /// <summary>
    /// デッキ表示表示
    /// </summary>
    void DisplayDeck()
    {


    }

    /// <summary>
    /// フィルター変更時
    /// </summary>
    /// <param name="filterIndex"></param>
    void OnFilterChanged(int filterIndex)
    {

    }

    /// <summary>
    /// 検索入力変更時
    /// </summary>
    /// <param name="keyword"></param>
    void OnSearchInputChanged(string keyword)
    {

    }
}
