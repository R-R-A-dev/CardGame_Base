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
    [SerializeField] private Transform packTransform;
    [SerializeField] private Button packButton;
    [SerializeField] private Transform packPosition;
    [SerializeField] private Vector3 packMoveDownOffset = new Vector3(0, -300f, 0);

    [Header("パック破れ演出")]
    [SerializeField] private GameObject packTopPart;
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
    [SerializeField] private TextMeshProUGUI nextButtonText;

    private PackData currentPack;
    private List<List<GachaCardEntry>> packResults = new List<List<GachaCardEntry>>();
    private List<GachaCardEntry> allDrawnCards = new List<GachaCardEntry>();
    private int currentPackIndex = 0;
    private int revealedCount = 0;

    private Vector3 packInitialPos;
    private Vector3 packTopInitialPos;

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
        packTransform.position = packPosition.position;
        packInitialPos = packTransform.position;

        packTopPart.SetActive(true);
        packTopInitialPos = packTopPart.transform.localPosition;

        packButton.interactable = true;
        skipButton.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(false);

        List<GachaCardEntry> drawnCards = packResults[currentPackIndex];

        for (int i = 0; i < cardList.Count; i++)
        {
            bool isValid = i < drawnCards.Count;

            if (isValid)
            {
                cardList[i].transform.position = packPosition.position;
                // rarityも一緒に渡す
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
        Vector3 startPos = packTopInitialPos;
        Vector3 endPos = packTopInitialPos + packTopMoveOffset;

        float elapsed = 0f;
        while (elapsed < tearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / tearDuration);
            packTopPart.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        packTopPart.SetActive(false);
    }

    private IEnumerator MovePackDownAnimation()
    {
        Vector3 startPos = packInitialPos;
        Vector3 endPos = packInitialPos + packMoveDownOffset;

        float elapsed = 0f;
        while (elapsed < packMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / packMoveDuration);
            packTransform.position = Vector3.Lerp(startPos, endPos, t);
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
        {
            Debug.Log("SSRカードが引かれたのでカメラシェイクを鳴らす");
            cameraShake.StartShake(0.8f, 20f, 5, 0, false);
        }
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

        bool isLastPack = currentPackIndex >= packResults.Count - 1;
        nextButtonText.text = isLastPack ? "結果を見る" : "次のパックへ";
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