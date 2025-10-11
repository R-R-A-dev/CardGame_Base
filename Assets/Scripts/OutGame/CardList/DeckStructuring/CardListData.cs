using System.Collections.Generic;
using UnityEngine;

public class CardListData
{
    //デッキの中身とそれぞれの枚数
    static List<List<int>> decks = new List<List<int>>();

    //所持カード状況
    //添え字+1をカードIDにして、値を所持枚数にする
    static List<int> possessionCard = new List<int>();

    static List<CardEntity> entities = new List<CardEntity>();

    public static List<List<int>> Decks { get { return decks; } set { decks = value; } }
    public static List<int> PossessionCard { get { return possessionCard; } set { possessionCard = value; } }

    //ファイルから手持ちのカードやデッキのカード情報を読み込む
    public static List<CardEntity> Entities { get { return entities; } set { entities = value; } }

}
