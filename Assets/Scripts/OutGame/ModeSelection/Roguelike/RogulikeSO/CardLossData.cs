using UnityEngine;

[CreateAssetMenu(fileName = "CardLossData", menuName = "Roguelike/CardLossData")]
public class CardLossData : ScriptableObject
{
    public int cardLossCount;
    public bool isPlayerChoice;     // trueならプレイヤーが選んで捨てる
    [TextArea(1, 2)]
    public string description;
}