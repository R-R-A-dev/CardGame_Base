using System.Collections.Generic;
using UnityEngine;

public class GamePlayerManager : MonoBehaviour
{
    public List<int> deck = new List<int>();

    public int heroHp;
    public int manaCost;
    public int defaultManaCost;

    public void Init(List<int> deck)
    {
        this.deck = deck;
        heroHp = 10;
        manaCost = 10;
        defaultManaCost = 10;
    }

    public void IncreaseManaCost()
    {
        defaultManaCost++;
        if(defaultManaCost > 10)
        {
            defaultManaCost = 10;
        }
        manaCost = defaultManaCost;
    }
}
