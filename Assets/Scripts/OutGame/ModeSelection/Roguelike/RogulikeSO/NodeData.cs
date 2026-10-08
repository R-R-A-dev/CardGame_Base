using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeData", menuName = "Roguelike/NodeData")]
public class NodeData : ScriptableObject
{
    [Header("基本情報")]
    public string nodeId;
    public StageType stageType;

    [Header("バフ・デバフ")]
    public List<ParameterModifier> modifiers;

    [Header("各StageTypeの設定")]
    public EnemyData enemyData;
    public RewardData rewardData;
    public RestData restData;
    public ShopData shopData;
    public TreasureData treasureData;
    public DamageNodeData damageData;
    public CardLossData cardLossData;
}