using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    [Header("ピック演出")]
    [Tooltip("選んだカードが吸い込まれる先（中央のデッキ枚数パネル）")]
    [SerializeField] RectTransform pickedCardDestination;
    [SerializeField] float selectedPunchScale = 1.15f;
    [SerializeField] float selectedPunchDuration = 0.15f;
    [SerializeField] float flyDuration = 0.4f;
    [SerializeField] float flyEndScale = 0.3f;
    [SerializeField] float unselectedFadeDuration = 0.25f;
    [SerializeField] float entranceDuration = 0.35f;
    [Tooltip("次の4枚を1枚ずつずらして登場させる間隔")]
    [SerializeField] float entranceStagger = 0.05f;

    // 演出中はSelectボタンを受け付けない
    bool isAnimating;

    // 手前に描画するため一時的に親を付け替えたカードと、戻し先
    readonly List<(Transform card, Transform parent, Vector3 localPosition)> flyingCards = new List<(Transform, Transform, Vector3)>();

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
            // ピック演出から呼ばれた場合にボタンがロックされたままにならないようにする
            SetAnimating(false);
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

        // ピック開始時・ピック後どちらも新しい4枚を弾むように登場させる
        PlayEntranceAnimation();
    }

    public void OnLeftButtonClick()
    {
        SelectPair(true);
    }

    public void OnRightButtonClick()
    {
        SelectPair(false);
    }

    void SelectPair(bool isLeft)
    {
        if (isAnimating) return;
        SetAnimating(true);

        TwoPickModeManager manager = TwoPickModeManager.Instance;

        //選択されたカードを進捗に追加
        manager.pickProgress.SelectedCards.AddRange(isLeft ? leftCards : rightCards);
        manager.PickCountUp();
        List<int> selectedCards = manager.pickProgress.GetSelectedCardsSortedById(manager.pickProgress.SelectedCards);

        leftCards.Clear();
        rightCards.Clear();

        PlayPickAnimation(
            isLeft ? leftCardParent : rightCardParent,
            isLeft ? rightCardParent : leftCardParent,
            onArrive: () =>
            {
                // カードがパネルに届いたタイミングで表示を更新し、コストの棒を伸ばす
                pickCount.text = manager.GetPickCount().ToString();
                manager.RefreshStatistics(selectedCards);
            },
            onComplete: () =>
            {
                RestoreFlyingCards();

                //既定回数に達したら、棒が伸びきってから終了（最後は生成をしない）
                if (manager.GetPickCount() > 19)
                    DOVirtual.DelayedCall(manager.StatisticsAnimationDuration, PickEnd).SetLink(gameObject);
                else
                    manager.ShowNextCards();
            });
    }

    void PickEnd()
    {
        cardParent.SetActive(false);
        leftButton.SetActive(false);
        rightButton.SetActive(false);
        ModeConfigManager.Instance.ChangeMode(GameMode.TWO_PICK);
        GameSession.SelectedDeck = TwoPickModeManager.Instance.pickProgress.SelectedCards;
        SceneTransition.Load("Game");
    }

    /// <summary>
    /// 選んだペアは少し拡大してから中央のパネルへ縮小・フェードしながら移動し、
    /// 選ばなかったペアはその場でフェードアウトする
    /// </summary>
    void PlayPickAnimation(List<Transform> pickedSlots, List<Transform> unpickedSlots, TweenCallback onArrive, TweenCallback onComplete)
    {
        Sequence sequence = DOTween.Sequence().SetLink(gameObject);

        foreach (Transform slot in pickedSlots)
        {
            CardController card = GetCard(slot);
            if (card == null) continue;

            Transform cardTransform = card.transform;
            CanvasGroup canvasGroup = GetCanvasGroup(card);

            // 移動中に他のUIの裏へ隠れないよう、見た目の位置を保ったまま最前面へ移す
            flyingCards.Add((cardTransform, slot, cardTransform.localPosition));
            cardTransform.SetParent(transform, true);
            cardTransform.SetAsLastSibling();

            Vector3 destination = pickedCardDestination != null ? pickedCardDestination.position : cardTransform.position;

            sequence.Insert(0f, cardTransform.DOScale(selectedPunchScale, selectedPunchDuration).SetEase(Ease.OutQuad));
            sequence.Insert(selectedPunchDuration, cardTransform.DOMove(destination, flyDuration).SetEase(Ease.InOutCubic));
            sequence.Insert(selectedPunchDuration, cardTransform.DOScale(flyEndScale, flyDuration).SetEase(Ease.InCubic));
            // 飛び始めは見えたまま、パネルに近づくにつれて消える
            sequence.Insert(selectedPunchDuration + flyDuration * 0.4f, canvasGroup.DOFade(0f, flyDuration * 0.6f));
        }

        foreach (Transform slot in unpickedSlots)
        {
            CardController card = GetCard(slot);
            if (card == null) continue;

            sequence.Insert(0f, GetCanvasGroup(card).DOFade(0f, unselectedFadeDuration));
        }

        sequence.InsertCallback(selectedPunchDuration + flyDuration, onArrive);
        sequence.OnComplete(onComplete);
    }

    /// <summary>
    /// 最前面へ移したカードを元の枠に戻す。CreateCardは枠の子の有無で再利用を判断するため、次の生成前に戻す必要がある
    /// </summary>
    void RestoreFlyingCards()
    {
        foreach (var (card, parent, localPosition) in flyingCards)
        {
            card.SetParent(parent, false);
            card.localPosition = localPosition;
        }
        flyingCards.Clear();
    }

    /// <summary>
    /// 4枚をスケール0から少し弾むように登場させる
    /// </summary>
    void PlayEntranceAnimation()
    {
        SetAnimating(true);

        Sequence sequence = DOTween.Sequence().SetLink(gameObject);
        int index = 0;

        foreach (Transform slot in GetSlotsInDisplayOrder())
        {
            CardController card = GetCard(slot);
            if (card == null) continue;

            CanvasGroup canvasGroup = GetCanvasGroup(card);
            card.transform.DOKill();
            canvasGroup.DOKill();

            canvasGroup.alpha = 1f;
            card.transform.localScale = Vector3.zero;
            sequence.Insert(entranceStagger * index, card.transform.DOScale(1f, entranceDuration).SetEase(Ease.OutBack));
            index++;
        }

        sequence.OnComplete(() => SetAnimating(false));
    }

    // 画面左から順に登場させるための並び（左ペア→右ペア）
    IEnumerable<Transform> GetSlotsInDisplayOrder()
    {
        return leftCardParent.Concat(rightCardParent);
    }

    CardController GetCard(Transform slot)
    {
        return slot.childCount > 0 ? slot.GetChild(0).GetComponent<CardController>() : null;
    }

    CanvasGroup GetCanvasGroup(CardController card)
    {
        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : card.gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// 演出中はSelectボタンを押せなくし、カードのクリック（詳細表示）も止める
    /// </summary>
    void SetAnimating(bool animating)
    {
        isAnimating = animating;

        SetButtonInteractable(leftButton, !animating);
        SetButtonInteractable(rightButton, !animating);

        foreach (Transform slot in GetSlotsInDisplayOrder())
        {
            CardController card = GetCard(slot);
            if (card != null)
                GetCanvasGroup(card).blocksRaycasts = !animating;
        }
    }

    void SetButtonInteractable(GameObject buttonObject, bool interactable)
    {
        if (buttonObject != null && buttonObject.TryGetComponent(out Button button))
            button.interactable = interactable;
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