using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PackData", menuName = "Gacha/PackData")]
public class PackData : ScriptableObject
{
    [Header("基本情報")]
    public string packName;
    public Sprite packImage;
    public int price;               // 購入に必要な所持金
    [TextArea(2, 4)]
    public string description;

    [Header("アンロック条件")]
    public bool isUnlockedByDefault; // 最初からアンロック済みか
    public int unlockConditionType;  // 0:デフォルト 1:ステージクリア 2:カード枚数等
    public string unlockConditionDescription;

    [Header("排出カード設定")]
    public List<int> cardPool;       // 排出カードIDのリスト
    public int drawCount = 5;        // 1回で排出する枚数
}