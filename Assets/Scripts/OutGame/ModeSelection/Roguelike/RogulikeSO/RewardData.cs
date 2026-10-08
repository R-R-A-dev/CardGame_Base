using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardData", menuName = "Roguelike/RewardData")]
public class RewardData : ScriptableObject
{
    [Header("カード報酬")]
    public bool hasCardReward = true;
    public int cardChoiceCount = 3;     // 提示するカード数
    public int cardSelectCount = 1;     // 選べる枚数
    public List<int> rewardCardPool;    // 空なら共通プールから

    [Header("ゴールド報酬")]
    public bool hasGoldReward = true;
    public bool isRandomGold = true;
    public int goldMin = 10;
    public int goldMax = 30;
    public int goldFixed = 0;           // isRandomGold=falseの時に使用
}