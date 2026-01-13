using System.Collections.Generic;
using UnityEngine;

public class TwoPickUI : MonoBehaviour
{
    [SerializeField] private List<Transform> leftCardParent;   // 左カードの親
    [SerializeField] private List<Transform> rightCardParent;  // 右カードの親
    [SerializeField] private CardController cardPrefab;      // カードプレハブ

    public void ShowPickCard()
    {
        CreateCard();
    }

    private void CreateCard()
    {
        for (int i = 0; i < leftCardParent.Count; i++)
        {
            // カード生成
            CardController leftCard = Instantiate(cardPrefab, leftCardParent[i]);
            CardController rightCard = Instantiate(cardPrefab, rightCardParent[i]);

            // カード情報設定（仮のデータを使用）
            leftCard.Init(1, false);
            rightCard.Init(1, false);

        }
    }

    private void ClearCard()
    {

    }

    public void OnLeftButtonClick()
    {

    }

    public void OnRightButtonClick()
    {

    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
