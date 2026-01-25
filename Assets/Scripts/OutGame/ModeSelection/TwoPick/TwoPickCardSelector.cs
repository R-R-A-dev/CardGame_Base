using System.Collections.Generic;
using UnityEngine;

public class TwoPickCardSelector : MonoBehaviour
{
    private List<int> cardPool; // 使用可能なカード全体
    private List<int> usedCards; // 今回のピックで出現したカード（重複制御用）

    public TwoPickCardSelector(TwoPickData data)
    {
        //cardPool = new List<int>(data.availableCards);
        usedCards = new List<int>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
