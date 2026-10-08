using System.Collections.Generic;
using UnityEngine;

public class CardListData
{
    //デッキの中身とそれぞれの枚数
    static List<List<int>> decks = new List<List<int>>();

    //所持カード状況
    //添え字+1をカードIDにして、値を所持枚数にする
    static List<int> possessionCard = new List<int>();

    public static List<List<int>> Decks { get { return decks; } set { decks = value; } }
    public static List<int> PossessionCard { get { return possessionCard; } set { possessionCard = value; } }

    // カードデータ本体はCardDatabase（カードNoで引ける）に一本化した。
    // ここに配列順のまま溜め込むと、Resources.LoadAllの名前順とカードNoがずれる上に
    // staticのままシーンを跨いで重複蓄積していたため廃止
}
