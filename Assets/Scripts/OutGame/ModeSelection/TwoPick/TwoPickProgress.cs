using System.Collections.Generic;
using UnityEngine;

public class TwoPickProgress
{
    private List<int> selectedCards = new List<int>();

    public TwoPickProgress()
    {

    }

    public TwoPickProgress(int total)
    {
        //totalPicks = total;
        //selectedCards = new List<int>();
        //currentPick = 0;
    }

    public List<int> SelectedCards { get => selectedCards; set => selectedCards = value; }

    //selectedCards初期化
    public void InitializeSelectedCards()
    {
        selectedCards = new List<int>();
    }

    public List<int> GetSelectedCardsSortedById(List<int> addCards)
    {
        int maxCardId = 120;
        List<int> sortedCards = new List<int>(new int[maxCardId]);
        // addCardsのIDをカウント
        foreach (int cardId in addCards)
        {
            // cardId = 1 → sortedCards[0]
            // cardId = 2 → sortedCards[1]
            int index = cardId - 1;

            // 範囲チェック
            if (index >= 0 && index < maxCardId)
            {
                sortedCards[index]++;
            }
            else
            {
                Debug.LogWarning($"Invalid card ID: {cardId}");
            }
        }
        return sortedCards;
    }


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}