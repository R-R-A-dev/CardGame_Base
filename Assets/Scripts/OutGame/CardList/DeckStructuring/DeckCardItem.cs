using System.Collections.Generic;
using UnityEngine;

public class DeckCardItem : MonoBehaviour
{
    [SerializeField] GameObject cardObject;


    /// <summary>
    /// 表示するデッキのカード情報をセットする
    /// </summary>
    public GameObject[] CardSetUp( Transform cardListContent, int deckNum)
    {
        //全カードデータと所持カードデータを比較して
        //デッキにあるカードとその枚数を取得する
        CardEntity[] entitys = CardDatabase.LoadAllCards();
        CardListData.Decks = new List<List<int>> { new List<int> { 1, 1, 1, 1 } };
        GameObject[] cardObjects = new GameObject[CardListData.Decks[deckNum].Count];
        for (int i = 0; i < CardListData.Decks[deckNum].Count; i++)
        {
            if (CardListData.Decks[deckNum][i] > 0)
            {
                //カード番号：entitys[i].no
                //カードの枚数：CardListData.PossessionCard[i]
                //カードパネルを生成して、カード情報をセットしカードも生成する
                GameObject obj = Instantiate(cardObject, cardListContent.transform);
                OutGameCardList outGameCard = obj.GetComponent<OutGameCardList>();
                outGameCard.SetData(entitys[i]);
                obj.GetComponent<CardDragHandler>().isDeck = true;
                cardObjects[i] = obj;
            }
        }
        return cardObjects;
    }

    /// <summary>
    /// 指定されたカードの番号からデッキの増加情報を更新
    /// </summary>
    /// <param name="cardNo"></param>
    public void AddDeckCard(int cardNo)
    {
        CardListData.Decks[DeckBuilderManager.Instance.deckNum][cardNo - 1]++;
    }

    /// <summary>
    /// 指定されたカードの番号をデッキから減少情報を更新
    /// </summary>
    /// <param name="cardNo"></param>
    public void ReturnListCard(int cardNo)
    {
        CardListData.Decks[DeckBuilderManager.Instance.deckNum][cardNo - 1]--;
    }
}
