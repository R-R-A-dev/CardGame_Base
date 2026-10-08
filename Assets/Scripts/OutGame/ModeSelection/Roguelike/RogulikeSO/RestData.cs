using UnityEngine;

[CreateAssetMenu(fileName = "RestData", menuName = "Roguelike/RestData")]
public class RestData : ScriptableObject
{
    public int healAmount;
    public bool isPercentageHeal;
    public int healPercentage;
    public bool canUpgradeCard;
    public bool canRemoveCard;
}