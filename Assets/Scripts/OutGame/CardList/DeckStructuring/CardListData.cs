using System.Collections.Generic;
using UnityEngine;

public class CardListData
{
    //デッキの中身とそれぞれの枚数
    List<List<int>> decks = new List<List<int>>();

    //所持カード状況
    List<int> possessionCard = new List<int>();
}
