using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TwoPickUI : MonoBehaviour
{
    [SerializeField] private List<Transform> leftCardParent;   // 左カードの親
    [SerializeField] private List<Transform> rightCardParent;  // 右カードの親
    [SerializeField] private CardController cardPrefab;      // カードプレハブ

    [SerializeField] List<GameObject> leftZone;
    [SerializeField] List<GameObject> rightZone;

    List<int> leftCards = new List<int>();
    List<int> rightCards = new List<int>();
    public void ShowPickCard()
    {

    }

    public void CreateCard(List<CardGroup> cards, int pickCount)
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

            //ボタンを押した時の渡すカードを追加
            leftCards.Add(leftCardId);
            rightCards.Add(rightCardId);


            CardController leftCard = null;
            CardController rightCard = null;

            //既にカードがあれば生成しない
            //登場アニメーション
            if (leftZone[0].transform.childCount == 0 || leftZone[1].transform.childCount == 0)
            {


                // カード生成
                leftCard = Instantiate(cardPrefab, leftCardParent[i]);
                rightCard = Instantiate(cardPrefab, rightCardParent[i]);

                // カード情報設定（ランダムに選んだIDを使用）
                leftCard.Init(leftCardId, false);
                rightCard.Init(rightCardId, false);
            }
            else
            {
                CardController leftChild = leftZone[i].transform.GetChild(0).GetComponent<CardController>();
                CardController rightChild = rightZone[i].transform.GetChild(0).GetComponent<CardController>();

                leftChild.Init(leftCardId, false);
                rightChild.Init(rightCardId, false);

                Debug.Log("既にカード生成");
            }
        }


    }
    public void OnLeftButtonClick()
    {
        DisableButtons();

        TwoPickModeManager.Instance.pickProgress.SelectedCards.AddRange(leftCards);
        TwoPickModeManager.Instance.PickCountUp();
        //選択されたカードを進捗に追加
        List<int> selectedCards = TwoPickModeManager.Instance.pickProgress.GetSelectedCardsSortedById(TwoPickModeManager.Instance.pickProgress.SelectedCards);

        leftCards.Clear();
        TwoPickModeManager.Instance.OnLeftButtonClick(selectedCards);

        //UI更新
        //既定回数に達したら終了
    }

    public void OnRightButtonClick()
    {
        DisableButtons();

        TwoPickModeManager.Instance.pickProgress.SelectedCards.AddRange(rightCards);
        leftCards.Clear();

        //選択されたカードを進捗に追加
        TwoPickModeManager.Instance.PickCountUp();
        TwoPickModeManager.Instance.OnRightButtonClick();
        //UI更新
        //既定回数に達したら終了
    }

    void DisableButtons()
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
/*左右ボタンを押す（エフェクト）
 * 追加値を増やす
 * カードが更新
 * UI更新
 * 選択されたカードを足す
 * 選択後UIの増加
 * カードクリック時に効果が表示
 * 
 * 
 * 
*/