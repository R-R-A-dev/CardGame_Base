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

    //selectedCardsèâä˙âª
    public void InitializeSelectedCards()
    {
        selectedCards = new List<int>();
    }



    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
