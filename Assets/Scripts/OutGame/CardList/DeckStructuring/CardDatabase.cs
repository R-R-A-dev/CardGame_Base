using UnityEngine;

public class CardDatabase 
{
    public static CardEntity[] LoadAllCards()
    {
        return Resources.LoadAll<CardEntity>("CardEntityList");
    }

    public static CardEntity LoadCardByID(int cardID)
    {
        return Resources.Load<CardEntity>($"CardEntityList/Card{cardID}");
    }
}
