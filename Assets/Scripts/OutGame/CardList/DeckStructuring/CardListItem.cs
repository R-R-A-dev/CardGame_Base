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
    public GameObject[] CardSetUp(GameObject cardPanel,Transform cardListContent)
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
                GameObject panel = Instantiate(cardPanel, cardListContent.transform);
                cardObject.GetComponent<OutGameCardList>().SetData(entitys[i]);
                cardObjects[i] = Instantiate(cardObject,panel.transform);
            }
        }
        return cardObjects;
    }
}
