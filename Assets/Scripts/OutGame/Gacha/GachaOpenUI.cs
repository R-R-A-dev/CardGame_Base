using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaOpenUI : MonoBehaviour
{
    [Header("パック進行表示")]
    [SerializeField] private TextMeshProUGUI packCountText;

    [Header("パック")]
    [SerializeField] private GameObject packObject;
    [SerializeField] private RectTransform packTransform;
    [SerializeField] private Button packButton;
    [SerializeField] private Transform packPosition;
    // anchoredPosition基準の移動量。zは使用しない
    [SerializeField] private Vector3 packMoveDownOffset = new Vector3(0, -300f, 0);

    [Header("パック破れ演出")]
    [SerializeField] private GameObject packTopPart;
    // anchoredPosition基準の移動量。zは使用しない
    [SerializeField] private Vector3 packTopMoveOffset = new Vector3(0, 200f, 0);

    [Header("アニメーション時間")]
    [SerializeField] private float tearDuration = 0.3f;
    [SerializeField] private float packMoveDuration = 0.4f;
    [SerializeField] private float cardMoveDuration = 0.2f;
    [SerializeField] private float cardMoveInterval = 0.15f;

    [Header("カード一覧")]
    [SerializeField] private List<GachaCardItem> cardList;
    [SerializeField] private List<Transform> cardTargetPositions;

    [Header("演出参照")]
    [SerializeField] private CameraShake cameraShake; // SSR演出時のカメラシェイク

    [Header("ボタン")]
    [SerializeField] private Button skipButton;
    [SerializeField] private Button nextButton;

    private PackData currentPack;
    private List<List<GachaCardEntry>> packResults = new List<List<GachaCardEntry>>();
    private List<GachaCardEntry> allDrawnCards = new List<GachaCardEntry>();
    private int currentPackIndex = 0;
    private int revealedCount = 0;

    private RectTransform packTopRect;
    private Vector2 packOriginalAnchoredPos;
    private Vector2 packTopOriginalAnchoredPos;

    // Canvasのスケール・サイズはCanvas更新時（Start以降）まで確定しないため、
    // world座標ではなくシリアライズ済みのanchoredPositionを初期位置として保持する。
    // 取得はAwakeで行い、Startより前にOpenされても正しい値が入るようにする。
    private void Awake()
    {
        packTopRect = (RectTransform)packTopPart.transform;

        packOriginalAnchoredPos = packTransform.anchoredPosition;
        packTopOriginalAnchoredPos = packTopRect.anchoredPosition;

        packButton.onClick.RemoveAllListeners();
        skipButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();

        packButton.onClick.AddListener(OnPackClicked);
        skipButton.onClick.AddListener(OnSkipButtonClick);
        nextButton.onClick.AddListener(OnNextButtonClick);
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Open(PackData pack, List<List<GachaCardEntry>> results)
    {
        currentPack = pack;
        packResults = results;
        allDrawnCards.Clear();
        currentPackIndex = 0;

        gameObject.SetActive(true);
        OpenCurrentPack();
    }

    private void OpenCurrentPack()
    {
        revealedCount = 0;

        packCountText.text = $"{currentPackIndex + 1} / {packResults.Count} パック目";

        packObject.SetActive(true);
        packTransform.anchoredPosition = packOriginalAnchoredPos;

        packTopPart.SetActive(true);
        packTopRect.anchoredPosition = packTopOriginalAnchoredPos;

        packButton.interactable = true;
        skipButton.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(false);

        List<GachaCardEntry> drawnCards = packResults[currentPackIndex];

        for (int i = 0; i < cardList.Count; i++)
        {
            bool isValid = i < drawnCards.Count;

            // 抽選枚数を超える枠は前のパックの状態が残らないよう非表示にする
            cardList[i].gameObject.SetActive(isValid);

            if (isValid)
            {
                // packTransformをリセットした後に読むこと。Canvas更新後なのでworld座標で問題ない
                cardList[i].transform.position = packPosition.position;
                cardList[i].Setup(drawnCards[i].cardId, drawnCards[i].rarity, false, OnCardClicked);
            }
        }
    }

    private void OnPackClicked()
    {
        packButton.interactable = false;
        StartCoroutine(PlayOpenSequence());
    }

    private IEnumerator PlayOpenSequence()
    {
        yield return StartCoroutine(TearPackAnimation());
        yield return StartCoroutine(MovePackDownAnimation());
        yield return StartCoroutine(DealCardsAnimation());

        OnCardDealAnimationComplete();
    }

    private IEnumerator TearPackAnimation()
    {
        Vector2 startPos = packTopOriginalAnchoredPos;
        Vector2 endPos = packTopOriginalAnchoredPos + (Vector2)packTopMoveOffset;

        float elapsed = 0f;
        while (elapsed < tearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / tearDuration);
            packTopRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        packTopPart.SetActive(false);
    }

    private IEnumerator MovePackDownAnimation()
    {
        Vector2 startPos = packOriginalAnchoredPos;
        Vector2 endPos = packOriginalAnchoredPos + (Vector2)packMoveDownOffset;

        float elapsed = 0f;
        while (elapsed < packMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / packMoveDuration);
            packTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        packObject.SetActive(false);
    }

    private IEnumerator DealCardsAnimation()
    {
        List<GachaCardEntry> drawnCards = packResults[currentPackIndex];

        for (int i = 0; i < drawnCards.Count; i++)
        {
            if (i >= cardList.Count || i >= cardTargetPositions.Count) break;

            cardList[i].gameObject.SetActive(true);
            yield return StartCoroutine(MoveCardToPosition(
                cardList[i].transform, cardTargetPositions[i].position));

            yield return new WaitForSeconds(cardMoveInterval);
        }
    }

    private IEnumerator MoveCardToPosition(Transform card, Vector3 targetPos)
    {
        Vector3 startPos = card.position;
        float elapsed = 0f;

        while (elapsed < cardMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cardMoveDuration);
            card.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        card.position = targetPos;
    }

    public void OnCardDealAnimationComplete()
    {
        foreach (GachaCardItem card in cardList)
            card.SetClickable(true);
    }

    private void OnCardClicked(GachaCardItem card)
    {
        // SSRの場合はカメラシェイクを鳴らす
        if (card.Rarity == CardRarity.SSR)
            cameraShake.StartShake(0.8f, 20f, 5, 0, false);
        
        card.PlayRevealAnimation();
        revealedCount++;

        List<GachaCardEntry> drawnCards = packResults[currentPackIndex];
        if (revealedCount >= drawnCards.Count)
            ShowNextButton();
    }

    private void OnSkipButtonClick()
    {
        StopAllCoroutines();

        packObject.SetActive(false);
        packTopPart.SetActive(false);

        List<GachaCardEntry> drawnCards = packResults[currentPackIndex];
        for (int i = 0; i < drawnCards.Count; i++)
        {
            if (i >= cardList.Count || i >= cardTargetPositions.Count) break;
            cardList[i].gameObject.SetActive(true);
            cardList[i].transform.position = cardTargetPositions[i].position;
            cardList[i].ForceReveal();
        }

        ShowNextButton();
    }

    private void ShowNextButton()
    {
        skipButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(true);
    }

    private void OnNextButtonClick()
    {
        allDrawnCards.AddRange(packResults[currentPackIndex]);
        currentPackIndex++;

        if (currentPackIndex < packResults.Count)
        {
            OpenCurrentPack();
        }
        else
        {
            GachaManager.Instance.OnAllPacksOpened(allDrawnCards);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}