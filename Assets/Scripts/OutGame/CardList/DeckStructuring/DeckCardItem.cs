using System.Collections.Generic;
using UnityEngine;

public class DeckCardItem : MonoBehaviour
{
    [SerializeField] GameObject cardObject;


    /// <summary>
    /// 表示するデッキのカード情報をセットする
    /// </summary>
    public GameObject[] CardSetUp(GameObject cardPanel, Transform cardListContent, int deckNum)
    {
        //全カードデータと所持カードデータを比較して
        //デッキにあるカードとその枚数を取得する
        CardEntity[] entitys = CardDatabase.LoadAllCards();
        CardListData.Decks = new List<List<int>> { new List<int> { 1, 1, 1, 1 } };
        GameObject[] cardObjects = new GameObject[CardListData.Decks[0].Count];
        for (int i = 0; i < CardListData.Decks[0].Count; i++)
        {
            if (CardListData.Decks[0][i] > 0)
            {
                //カード番号：entitys[i].no
                //カードの枚数：CardListData.PossessionCard[i]
                //カードパネルを生成して、カード情報をセットしカードも生成する
                GameObject panel = Instantiate(cardPanel, cardListContent.transform);
                cardObject.GetComponent<OutGameCardList>().SetData(entitys[i]);
                cardObjects[i] = Instantiate(cardObject, panel.transform);
            }
        }
        return cardObjects;
    }
}
