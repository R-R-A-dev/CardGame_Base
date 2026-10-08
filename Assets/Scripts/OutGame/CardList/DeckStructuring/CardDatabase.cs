using System.Collections.Generic;
using UnityEngine;

public class CardDatabase
{
    // Resources.LoadAllが返す配列の並びは「名前順」（Card1, Card10, Card11, ... Card2）であり、
    // カードNo順ではない。カード9枚までは偶然一致していたが、Card10以降を追加すると崩れるため、
    // 添字ではなくカードNoで引けるようにここでキャッシュを作る
    private static Dictionary<int, CardEntity> cardsByNo;
    private static CardEntity[] allCards;
    private static int maxCardNo;

    public static CardEntity[] LoadAllCards()
    {
        BuildCache();
        return allCards;
    }

    public static CardEntity LoadCardByID(int cardID)
    {
        return Resources.Load<CardEntity>($"CardEntityList/Card{cardID}");
    }

    /// <summary>
    /// カードNoからカードデータを取得する。該当するカードが無ければnull
    /// </summary>
    public static CardEntity GetByNo(int cardNo)
    {
        BuildCache();
        return cardsByNo.TryGetValue(cardNo, out CardEntity entity) ? entity : null;
    }

    /// <summary>
    /// 全カード中で最大のカードNo。添字方式（添字＝カードNo-1）のリスト長にはこの値を使う。
    /// カード枚数（Length）を使うとNoが歯抜けになった時に範囲外参照になる
    /// </summary>
    public static int MaxCardNo
    {
        get
        {
            BuildCache();
            return maxCardNo;
        }
    }

    /// <summary>
    /// カードアセットを追加・削除した後にキャッシュを破棄する
    /// </summary>
    public static void ClearCache()
    {
        cardsByNo = null;
        allCards = null;
        maxCardNo = 0;
    }

    // 「ドメインリロードなしでプレイ開始」設定でもキャッシュが持ち越されないようにする
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayModeStart()
    {
        ClearCache();
    }

    private static void BuildCache()
    {
        if (cardsByNo != null) return;

        allCards = Resources.LoadAll<CardEntity>("CardEntityList");
        cardsByNo = new Dictionary<int, CardEntity>(allCards.Length);
        maxCardNo = 0;

        foreach (CardEntity entity in allCards)
        {
            if (entity == null) continue;

            if (entity.no <= 0)
            {
                Debug.LogWarning($"カードのnoが未設定です（アセット名: {entity.name}）。カード一覧には表示されません");
                continue;
            }

            if (cardsByNo.ContainsKey(entity.no))
            {
                Debug.LogWarning($"カードNo.{entity.no}が重複しています。先に読み込んだ方を使用します");
                continue;
            }

            cardsByNo[entity.no] = entity;
            if (entity.no > maxCardNo) maxCardNo = entity.no;
        }
    }
}
