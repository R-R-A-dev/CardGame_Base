using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TwoPickUI : MonoBehaviour
{
    [SerializeField] private List<Transform> leftCardParent;   // 左カードの親
    [SerializeField] private List<Transform> rightCardParent;  // 右カードの親
    [SerializeField] private CardController cardPrefab;      // カードプレハブ

    [SerializeField] List<GameObject> leftZone;
    [SerializeField] List<GameObject> rightZone;

    [SerializeField] TextMeshProUGUI pickCount;


    //カード一覧パネル
    [SerializeField] private GameObject cardsPanel;
    [SerializeField] CardController[] cardControllers;
    [SerializeField] TextMeshProUGUI[] cardsNum;

    List<int> leftCards = new List<int>();
    List<int> rightCards = new List<int>();

    [SerializeField] GameObject cardParent;
    [SerializeField] GameObject leftButton;
    [SerializeField] GameObject rightButton;

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
        rightCards.Clear();
        pickCount.text = TwoPickModeManager.Instance.GetPickCount().ToString();
        //最後は生成をせずにする
        TwoPickModeManager.Instance.OnLeftButtonClick(selectedCards);
        //UI更新
        //既定回数に達したら終了
        PickEnd();
    }



    public void OnRightButtonClick()
    {
        DisableButtons();

        TwoPickModeManager.Instance.pickProgress.SelectedCards.AddRange(rightCards);
        TwoPickModeManager.Instance.PickCountUp();

        List<int> selectedCards = TwoPickModeManager.Instance.pickProgress.GetSelectedCardsSortedById(TwoPickModeManager.Instance.pickProgress.SelectedCards);

        rightCards.Clear();
        leftCards.Clear();
        pickCount.text = TwoPickModeManager.Instance.GetPickCount().ToString();

        //選択されたカードを進捗に追加
        TwoPickModeManager.Instance.OnRightButtonClick(selectedCards);

        PickEnd();
    }

    void PickEnd()
    {
        if (TwoPickModeManager.Instance.GetPickCount() > 19)
        {
            cardParent.SetActive(false);
            leftButton.SetActive(false);
            rightButton.SetActive(false);
            ModeConfigManager.Instance.ChangeMode(GameMode.TWO_PICK);
            GameSession.SelectedDeck = TwoPickModeManager.Instance.pickProgress.SelectedCards;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
    }

    public void ShowCard()
    {
        //現在の取得カードのコスト順の一覧を取得
        //カード情報パネルに表示
        //リスト内のCardControllerをタップしたときにカード情報パネルをセット
        cardsPanel.SetActive(true);
        List<int> selectedCards = TwoPickModeManager.Instance.pickProgress.GetSelectedCardsSortedById(TwoPickModeManager.Instance.pickProgress.SelectedCards);

        for (int i = 0; i < cardControllers.Length; i++)
            cardControllers[i].gameObject.SetActive(false);

        int setCount = 0;
        for (int i = 0; i < selectedCards.Count; i++)
        {

            if (selectedCards[i] > 0)
            {
                // 表示枠より多くの種類を引いた場合はそこで打ち切る
                if (setCount >= cardControllers.Length) break;

                cardControllers[setCount].gameObject.SetActive(true);
                // 枚数表示は枠ごとに用意されているが、未設定でも一覧自体は表示できるようにする
                if (setCount < cardsNum.Length && cardsNum[setCount] != null)
                    cardsNum[setCount].text = selectedCards[i].ToString();
                //カードデータセット
                cardControllers[setCount].Init(i + 1, false);
                setCount++;
            }
        }
    }

    public void CloseCard()
    {
        cardsPanel.SetActive(false);
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
 * 既定の回数で終了
 * 
 * 一覧画面のデータセット
 * 対戦画面へ
*/