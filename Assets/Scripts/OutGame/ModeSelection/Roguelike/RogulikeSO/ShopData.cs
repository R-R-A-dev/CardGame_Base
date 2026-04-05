using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopData", menuName = "Roguelike/ShopData")]
public class ShopData : ScriptableObject
{
    public int shopCardCount;
    public List<int> shopCardList;
    public int cardPriceMin;
    public int cardPriceMax;
    public bool canRemoveCard;
    public int removeCardCost;
}