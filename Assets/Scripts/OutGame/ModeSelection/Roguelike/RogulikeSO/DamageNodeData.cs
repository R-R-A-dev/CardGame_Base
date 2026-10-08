using UnityEngine;

[CreateAssetMenu(fileName = "DamageNodeData", menuName = "Roguelike/DamageNodeData")]
public class DamageNodeData : ScriptableObject
{
    public int damageAmount;
    public bool isPercentageDamage;
    public int damagePercentage;
    [TextArea(1, 2)]
    public string description;
}