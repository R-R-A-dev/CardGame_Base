using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EventChoice
{
    [Header("選択肢テキスト")]
    public string choiceText;

    [Header("選択時の効果")]
    public List<ParameterModifier> modifiers;   // バフ・デバフ

    [Header("選択時の報酬")]
    public RewardData rewardData;               // カード・ゴールド報酬

    [Header("選択時のペナルティ")]
    public DamageNodeData damageData;           // ダメージ効果
    public CardLossData cardLossData;           // カード減少効果
}