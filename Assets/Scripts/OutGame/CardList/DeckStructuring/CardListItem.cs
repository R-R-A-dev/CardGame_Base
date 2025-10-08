using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardListItem : MonoBehaviour
{
    [SerializeField] GameObject cardObject;


    /// <summary>
    /// 表示するカードの情報をセットする
    /// </summary>
    public GameObject[] CardSetUp(Transform cardListContent)
    {
        //全カードデータと所持カードデータを比較して
        //所持しているカードとその枚数を取得する
        CardEntity[] entitys = CardDatabase.LoadAllCards();
        CardListData.PossessionCard = new List<int>() { 1, 1, 1, 1 };
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
}
