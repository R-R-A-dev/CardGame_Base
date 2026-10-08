using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PackData", menuName = "Gacha/PackData")]
public class PackData : ScriptableObject
{
    [Header("基本情報")]
    public string packName;
    public Sprite packImage;
    public int price;
    [TextArea(2, 4)]
    public string description;

    [Header("アンロック条件")]
    public bool isUnlockedByDefault;
    public int unlockConditionType;
    public string unlockConditionDescription;

    [Header("レアリティごとの排出率（合計100%）")]
    public List<RarityDropRate> rarityDropRates;

    [Header("排出カード一覧（カードIDとレアリティの対応）")]
    public List<GachaCardEntry> cardPool;

    public int drawCount = 5;

    // レアリティ排出率の合計を取得
    public float GetTotalRarityRate()
    {
        float total = 0f;
        foreach (RarityDropRate rate in rarityDropRates)
            total += rate.dropRatePercent;
        return total;
    }

    private void OnValidate()
    {
        if (rarityDropRates == null || rarityDropRates.Count == 0) return;

        float total = GetTotalRarityRate();
        if (Mathf.Abs(total - 100f) > 0.01f)
        {
            Debug.LogWarning($"[{packName}] レアリティ排出率の合計が100%ではありません（現在: {total}%）");
        }
    }
}