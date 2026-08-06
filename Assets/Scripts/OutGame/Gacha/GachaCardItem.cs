using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GachaCardItem : MonoBehaviour
{
    [Header("表示オブジェクト")]
    [SerializeField] private GameObject backImage;
    [SerializeField] private CardController cardController;
    [SerializeField] private Button cardButton;

    [Header("回転対象")]
    [SerializeField] private RectTransform cardRoot;

    [Header("通常時の回転演出設定")]
    [SerializeField] private float flipDuration = 0.1f;
    [SerializeField] private float switchAngle = -90f;

    [Header("通常時のスケール演出設定")]
    [SerializeField] private float scaleUpDuration = 0.8f;
    [SerializeField] private float scaleDownDuration = 0.8f;
    [SerializeField] private float maxScale = 2f;

    [Header("SSR演出設定")]
    [SerializeField] private float ssrFlipDuration = 0.1f;  // -180度→ssrSwitchAngleにかかる時間
    [SerializeField] private float ssrSwitchAngle = -90f;  // 裏表を切り替える角度
    [SerializeField] private float ssrPunchScaleTime = 0.2f;  // maxScale→punchScaleにかかる時間
    [SerializeField] private float ssrScaleDownTime = 0.5f;  // punchScale→1に戻る時間（回転も同時に0度へ戻る）
    [SerializeField] private float ssrMaxScale = 2f;    // 一段階目の拡大スケール
    [SerializeField] private float ssrPunchScale = 2.3f;  // 一瞬だけ膨らむ最大スケール

    [Header("共通詳細パネル")]
    [SerializeField] private GachaCardDetailPanel detailPanel;

    private bool isRevealed = false;
    private bool isClickable = false;
    private int cardId;
    private CardRarity rarity;
    private System.Action<GachaCardItem> onClicked;

    private const float START_ANGLE = -180f;
    private const float END_ANGLE = 0f;

    public CardRarity Rarity => rarity;
    public bool IsRevealed => isRevealed;

    public void Setup(int id, CardRarity cardRarity, bool revealed, System.Action<GachaCardItem> onClickedCallback)
    {
        cardId = id;
        rarity = cardRarity;
        isRevealed = revealed;
        onClicked = onClickedCallback;

        cardRoot.localRotation = Quaternion.Euler(0f, revealed ? END_ANGLE : START_ANGLE, 0f);
        cardRoot.localScale = Vector3.one;
        backImage.SetActive(!revealed);

        cardController.Init(cardId, false);

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnCardButtonClick);
        cardButton.interactable = false;
    }

    public void SetClickable(bool clickable)
    {
        isClickable = clickable;
        cardButton.interactable = clickable && !isRevealed;
    }

    private void OnCardButtonClick()
    {
        // めくり済みなら詳細パネルを開く
        if (isRevealed)
        {
            detailPanel.Open(cardController); // GachaCardItemが持つcardControllerを渡す
            return;
        }

        if (!isClickable) return;
        onClicked?.Invoke(this);
    }

    public void PlayRevealAnimation()
    {
        cardButton.interactable = false;

        if (rarity == CardRarity.SSR)
            StartCoroutine(SSRFlipAnimation());
        else
            StartCoroutine(FlipAnimation());
    }

    // ===== 通常のめくり演出 =====
    private IEnumerator FlipAnimation()
    {
        float totalRange = END_ANGLE - START_ANGLE;
        float switchRatio = (switchAngle - START_ANGLE) / totalRange;

        float switchDuration = flipDuration * switchRatio;
        float remainDuration = flipDuration - switchDuration;

        yield return StartCoroutine(RotateY(START_ANGLE, switchAngle, switchDuration));

        backImage.SetActive(false);

        yield return StartCoroutine(RotateY(switchAngle, END_ANGLE, remainDuration));

        // スケール演出（1→2→1）を削除。通常カードは回転のみで完結する

        isRevealed = true;
        cardButton.interactable = true;
    }

    // ===== SSR専用めくり演出 =====
    private IEnumerator SSRFlipAnimation()
    {
        int originalSiblingIndex = cardRoot.GetSiblingIndex();
        cardRoot.SetAsLastSibling();

        yield return StartCoroutine(SSRFlipAndScaleUp());

        backImage.SetActive(false);

        yield return StartCoroutine(ScaleOnly(ssrMaxScale, ssrPunchScale, ssrPunchScaleTime));
        yield return StartCoroutine(ScaleAndRotateBack(ssrPunchScale, 1f, ssrScaleDownTime));

        cardRoot.SetSiblingIndex(originalSiblingIndex);

        isRevealed = true;
        cardButton.interactable = true; // ← 追加：めくり終わったら詳細クリック用に再度trueにする
    }

    private IEnumerator SSRFlipAndScaleUp()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * ssrMaxScale;

        while (elapsed < ssrFlipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ssrFlipDuration);

            float angle = Mathf.Lerp(START_ANGLE, ssrSwitchAngle, t);
            cardRoot.localRotation = Quaternion.Euler(0f, angle, 0f);
            cardRoot.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        cardRoot.localRotation = Quaternion.Euler(0f, ssrSwitchAngle, 0f);
        cardRoot.localScale = endScale;
    }

    // スケールのみを指定時間かけて変化させる（回転はそのまま）
    private IEnumerator ScaleOnly(float fromScale, float toScale, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float scale = Mathf.Lerp(fromScale, toScale, t);
            cardRoot.localScale = Vector3.one * scale;

            yield return null;
        }

        cardRoot.localScale = Vector3.one * toScale;
    }

    // スケールと回転(ssrSwitchAngle→0度)を同時に変化させる
    private IEnumerator ScaleAndRotateBack(float fromScale, float toScale, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float scale = Mathf.Lerp(fromScale, toScale, t);
            cardRoot.localScale = Vector3.one * scale;
            cardRoot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(ssrSwitchAngle, END_ANGLE, t), 0f);

            yield return null;
        }

        cardRoot.localScale = Vector3.one * toScale;
        cardRoot.localRotation = Quaternion.Euler(0f, END_ANGLE, 0f);
    }

    private IEnumerator RotateY(float fromAngle, float toAngle, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float angle = Mathf.Lerp(fromAngle, toAngle, t);

            cardRoot.localRotation = Quaternion.Euler(0f, angle, 0f);

            yield return null;
        }

        cardRoot.localRotation = Quaternion.Euler(0f, toAngle, 0f);
    }

    public void ForceReveal()
    {
        if (isRevealed) return;

        StopAllCoroutines();

        cardRoot.localRotation = Quaternion.Euler(0f, END_ANGLE, 0f);
        cardRoot.localScale = Vector3.one;
        backImage.SetActive(false);

        isRevealed = true;
        cardButton.interactable = true; // ← false から true に変更
    }
}