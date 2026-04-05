using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TreasureData", menuName = "Roguelike/TreasureData")]
public class TreasureData : ScriptableObject
{
    public int cardCount;
    public List<int> treasureCardPool;
    public int goldAmount;
}