using UnityEngine;

[System.Serializable]
public class RarityDropRate
{
    public CardRarity rarity;

    [Range(0f, 100f)]
    [Tooltip("このレアリティが出る確率（%）。全レアリティの合計が100になるように設定する")]
    public float dropRatePercent;
}