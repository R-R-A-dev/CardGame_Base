using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaOpenUI : MonoBehaviour
{
    [Header("パック")]
    [SerializeField] private GameObject packObject;     // パック画像オブジェクト
    [SerializeField] private Button packButton;         // パッククリック用ボタン

    [Header("カード一覧")]
    [SerializeField] private GameObject cardArea;       // カードエリア全体
    [SerializeField] private List<GachaCardItem> cardList; // 5枚分のカード

    [Header("ボタン")]
    [SerializeField] private Button skipButton;         // 結果画面へスキップ
    [SerializeField] private Button nextButton;         // 全公開後に結果画面へ

    private List<int> currentDrawnCards = new List<int>();
    private int revealedCount = 0;                      // 表向きにしたカード枚数

    private void Start()
    {
        packButton.onClick.RemoveAllListeners();
        skipButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();

        packButton.onClick.AddListener(OnPackClicked);
        skipButton.onClick.AddListener(OnSkipButtonClick);
        nextButton.onClick.AddListener(OnNextButtonClick);

        gameObject.SetActive(false);
    }

    public void Open(List<int> drawnCards)
    {
        currentDrawnCards = drawnCards;
        revealedCount = 0;
        gameObject.SetActive(true);

        // パックを表示・カードエリアを非表示
        packObject.SetActive(true);
        cardArea.SetActive(false);
        packButton.interactable = true;

        skipButton.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(false);

        // カードを初期化（裏向き・非表示）
        for (int i = 0; i < cardList.Count; i++)
        {
            bool isValid = i < drawnCards.Count;
            cardList[i].gameObject.SetActive(isValid);

            if (isValid)
                cardList[i].Setup(drawnCards[i], false, OnCardClicked);
        }
    }

    // パッククリック時
    private void OnPackClicked()
    {
        packButton.interactable = false;

        // パックを開くアニメーション（こちらで実装）
        PlayPackOpenAnimation();
    }

    // パックを開くアニメーション枠
    private void PlayPackOpenAnimation()
    {
        // アニメーション後にOnPackOpenAnimationCompleteを呼ぶ
        OnPackOpenAnimationComplete();
    }

    // パックを開くアニメーション完了後
    public void OnPackOpenAnimationComplete()
    {
        // パックを非表示・カードエリアを表示
        packObject.SetActive(false);
        cardArea.SetActive(true);

        // カードを裏向きで並べるアニメーション（こちらで実装）
        PlayCardDealAnimation();
    }

    // カードを裏向きで並べるアニメーション枠
    private void PlayCardDealAnimation()
    {
        // アニメーション後にOnCardDealAnimationCompleteを呼ぶ
        OnCardDealAnimationComplete();
    }

    // カードを裏向きで並べるアニメーション完了後
    public void OnCardDealAnimationComplete()
    {
        // 全カードをクリック可能にする
        foreach (GachaCardItem card in cardList)
            card.SetClickable(true);
    }

    // カードをクリックして表向きにする
    private void OnCardClicked(GachaCardItem card)
    {
        // カードを表向きにするアニメーション（こちらで実装）
        card.PlayRevealAnimation();

        revealedCount++;

        // 全カード表向きになったら次へボタンを表示
        if (revealedCount >= currentDrawnCards.Count)
        {
            skipButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(true);
        }
    }

    // スキップボタン（全カードを強制的に表向きにして結果へ）
    private void OnSkipButtonClick()
    {
        foreach (GachaCardItem card in cardList)
            card.ForceReveal();

        skipButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(true);
    }

    // 次へボタン（結果画面へ）
    private void OnNextButtonClick()
    {
        GachaManager.Instance.OnOpenAnimationComplete(currentDrawnCards);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}