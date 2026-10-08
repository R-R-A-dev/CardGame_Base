using System;
using System.Collections.Generic;
using UnityEngine;

public class DeckCardItem : MonoBehaviour
{
    [SerializeField] GameObject cardObject;
    [SerializeField] private Transform deckContent;

    /// <summary>
    /// 表示するデッキのカード情報をセットする
    /// </summary>
    public GameObject[] CardSetUp( Transform cardListContent, int deckNum)
    {
        // 再生成前に既存のカードを削除（StartDeckEditが複数回呼ばれても重複生成しないようにする）
        foreach (Transform child in cardListContent)
            Destroy(child.gameObject);

        //全カードデータと所持カードデータを比較して
        //デッキにあるカードとその枚数を取得する
        List<int> editingDeckCounts = GameDataHolder.Instance.EditingDeckCounts;
        GameObject[] cardObjects = new GameObject[editingDeckCounts.Count];
        for (int i = 0; i < editingDeckCounts.Count; i++)
        {
            if (editingDeckCounts[i] > 0)
            {
                //カード番号：添字＋1
                //カードの枚数：editingDeckCounts[i]
                //LoadAllCardsの配列順はカードNo順ではないため、必ずNo指定で取得する
                CardEntity entity = CardDatabase.GetByNo(i + 1);
                if (entity == null) continue;

                //カードパネルを生成して、カード情報をセットしカードも生成する
                GameObject obj = Instantiate(cardObject, cardListContent.transform);
                OutGameCardList outGameCard = obj.GetComponent<OutGameCardList>();
                outGameCard.SetData(entity);
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
        GameDataHolder.Instance.EditingDeckCounts[cardNo - 1]++;
    }

    /// <summary>
    /// 指定されたカードの番号をデッキから減少情報を更新
    /// </summary>
    /// <param name="cardNo"></param>
    public void ReturnListCard(int cardNo)
    {
        GameDataHolder.Instance.EditingDeckCounts[cardNo - 1]--;
    }

    /// <summary>
    /// デッキフィルター表示非表示処理
    /// </summary>
    /// <param name="filter"></param>
    public void RefreshFiltered(CardFilterSettings filter)
    {
        foreach (Transform child in deckContent.transform)
        {
            OutGameCardList card = child.GetComponent<OutGameCardList>();
            if (card == null) continue;

            CardEntity entity = CardDatabase.GetByNo(card.No);
            if (entity == null) continue;

            child.gameObject.SetActive(filter.Matches(entity));
        }
    }
}
