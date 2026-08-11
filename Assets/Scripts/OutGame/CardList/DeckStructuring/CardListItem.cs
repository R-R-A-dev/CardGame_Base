using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Splines.ExtrusionShapes;
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
        // 再生成前に既存のカードを削除（StartDeckEditが複数回呼ばれても重複生成しないようにする）
        foreach (Transform child in cardListContent)
            Destroy(child.gameObject);

        // 全カードデータと所持カードデータを比較して
        CardEntity[] entitys = CardDatabase.LoadAllCards();

        List<int> displayPossessionCard = GameDataHolder.Instance.DisplayPossessionCard;
        GameObject[] cardObjects = new GameObject[displayPossessionCard.Count];
        for (int i = 0; i < displayPossessionCard.Count; i++)
        {
            if (displayPossessionCard[i] > 0)
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
        GameDataHolder.Instance.DisplayPossessionCard[cardNo - 1]++;
    }

    /// <summary>
    /// 指定されたカードの番号を一覧から減少情報を更新
    /// </summary>
    /// <param name="cardNo"></param>
    public void ReturnCardList(int cardNo)
    {
        GameDataHolder.Instance.DisplayPossessionCard[cardNo - 1]--;
    }

    /// <summary>
    /// 所持カードからデッキ分を減算して準備
    /// </summary>
    public void PrepareDeck()
    {
        // デッキに入っている分だけ減算
        List<int> deck = GameDataHolder.Instance.EditingDeckCounts;
        List<int> displayPossessionCard = GameDataHolder.Instance.DisplayPossessionCard;
        for (int i = 0; i < displayPossessionCard.Count; i++)
        {
            displayPossessionCard[i] -= deck[i];
            if (displayPossessionCard[i] < 0)
                displayPossessionCard[i] = 0;
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
