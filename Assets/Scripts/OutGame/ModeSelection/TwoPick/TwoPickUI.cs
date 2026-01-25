using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TwoPickUI : MonoBehaviour
{
    [SerializeField] private List<Transform> leftCardParent;   // 左カードの親
    [SerializeField] private List<Transform> rightCardParent;  // 右カードの親
    [SerializeField] private CardController cardPrefab;      // カードプレハブ

    List<int> leftCards = new List<int>();
    List<int> rightCards = new List<int>();
    public void ShowPickCard()
    {
        
    }

    public void CreateCard(List<CardGroup> cards,int pickCount)
    {
        // cards[pickCount].cards のコピーを作成
        List<int> availableCards = new List<int>(cards[pickCount].cards);

        // カードが足りない場合のチェック（2回ループで4枚必要）
        int requiredCards = leftCardParent.Count * 2; // 2 * 2 = 4枚
        if (availableCards.Count < requiredCards)
        {
            Debug.LogError($"Not enough cards. Need {requiredCards}, but only {availableCards.Count} available.");
            return;
        }

        // ランダムの組み合わせ
        for (int i = 0; i < leftCardParent.Count; i++)
        {
            // 左カード用にランダムで1枚選択
            int leftRandomIndex = Random.Range(0, availableCards.Count);
            int leftCardId = availableCards[leftRandomIndex];
            availableCards.RemoveAt(leftRandomIndex); // 選んだカードを除外

            // 右カード用にランダムで1枚選択
            int rightRandomIndex = Random.Range(0, availableCards.Count);
            int rightCardId = availableCards[rightRandomIndex];
            availableCards.RemoveAt(rightRandomIndex); // 選んだカードを除外

            // カード生成
            CardController leftCard = Instantiate(cardPrefab, leftCardParent[i]);
            CardController rightCard = Instantiate(cardPrefab, rightCardParent[i]);

            // カード情報設定（ランダムに選んだIDを使用）
            leftCard.Init(leftCardId, false);
            rightCard.Init(rightCardId, false);
        }
    }

    public void OnLeftButtonClick()
    {
        TwoPickModeManager.Instance.OnLeftButtonClick();
    }

    public void OnRightButtonClick()
    {

        TwoPickModeManager.Instance.OnRightButtonClick();
    }

    void DisableButton()
    {

    }

    private void ClearCard()
    {

    }



    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
