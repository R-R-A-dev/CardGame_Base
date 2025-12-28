using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardListItem : MonoBehaviour
{
    [SerializeField] GameObject cardObject;
    [SerializeField] private Transform cardListContent;


    /// <summary>
    /// 表示するカードの情報をセットする
    /// </summary>
    public GameObject[] CardSetUp(Transform cardListContent)
    {
        // 全カードデータと所持カードデータを比較して
        CardEntity[] entitys = CardDatabase.LoadAllCards();

        // 例: 総所持数で初期化
        CardListData.PossessionCard = new List<int>() { 3, 1, 1, 1 };



        GameObject[] cardObjects = new GameObject[CardListData.PossessionCard.Count];
        for (int i = 0; i < CardListData.PossessionCard.Count; i++)
        {
            if (CardListData.PossessionCard[i] > 0)
            {
                //カード番号：entitys[i].no
                //カードの枚数：CardListData.PossessionCard[i]
                //カードパネルを生成して、カード情報をセットしカードも生成する
                GameObject obj = Instantiate(cardObject, cardListContent.transform);
                OutGameCardList outGameCard = obj.GetComponent<OutGameCardList>();
                outGameCard.SetData(entitys[i]);
                cardObjects[i] = obj;
            }
        }
        return cardObjects;
    }

    /// <summary>
    /// 指定されたカードの番号から一覧の増加情報を更新
    /// </summary>
    /// <param name="cardNo"></param>
    public void AddCardList(int cardNo)
    {
        CardListData.PossessionCard[cardNo - 1]++;
    }

    /// <summary>
    /// 指定されたカードの番号を一覧から減少情報を更新
    /// </summary>
    /// <param name="cardNo"></param>
    public void ReturnCardList(int cardNo)
    {
        CardListData.PossessionCard[cardNo - 1]--;
    }

    /// <summary>
    /// 所持カードからデッキ分を減算して準備
    /// </summary>
    public void PrepareDeck()
    {
        // デッキに入っている分だけ減算
        List<int> deck = CardListData.Decks[DeckBuilderManager.Instance.deckNum];
        for (int i = 0; i < CardListData.PossessionCard.Count; i++)
        {
            CardListData.PossessionCard[i] -= deck[i];
            if (CardListData.PossessionCard[i] < 0)
                CardListData.PossessionCard[i] = 0;
        }
    }

    /// <summary>
    /// デッキフィルター表示非表示処理
    /// </summary>
    /// <param name="filter"></param>
    public void RefreshFiltered(CardFilterSettings filter)
    {
        foreach (Transform child in cardListContent.transform)
        {
            OutGameCardList card = child.GetComponent<OutGameCardList>();
            if (card == null) continue;

            CardEntity entity = CardListData.Entities[card.No - 1];
            child.gameObject.SetActive(filter.Matches(entity));
        }
    }
}
