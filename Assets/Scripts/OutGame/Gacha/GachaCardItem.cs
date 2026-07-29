using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GachaCardItem : MonoBehaviour
{
    [Header("表示オブジェクト")]
    [SerializeField] private GameObject backImage;
    [SerializeField] private CardController cardController;
    [SerializeField] private Button cardButton;

    [Header("回転対象")]
    [SerializeField] private RectTransform cardRoot; // RectTransformに変更（scale制御のため）

    [Header("通常時の回転演出設定")]
    [SerializeField] private float flipDuration = 0.4f;
    [SerializeField] private float switchAngle = -90f;

    [Header("SSR演出設定")]
    [SerializeField] private float ssrFlipDuration = 0.5f;  // -180→-90度にかかる時間
    [SerializeField] private float ssrSwitchAngle = -90f;  // 裏表切り替え角度
    [SerializeField] private float ssrScaleUpTime = 0.5f;  // スケール2になるまでの時間
    [SerializeField] private float ssrScaleDownTime = 0.9f;  // スケール1に戻るまでの時間
    [SerializeField] private float ssrMaxScale = 2f;

    private bool isRevealed = false;
    private bool isClickable = false;
    private int cardId;
    private CardRarity rarity;
    private System.Action<GachaCardItem> onClicked;

    private const float START_ANGLE = -180f;
    private const float END_ANGLE = 0f;

    public CardRarity Rarity => rarity;


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
        if (!isClickable || isRevealed) return;
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

        isRevealed = true;
    }

    // ===== SSR専用めくり演出 =====
    private IEnumerator SSRFlipAnimation()
    {
        int originalSiblingIndex = cardRoot.GetSiblingIndex();

        // めくっている間だけ同階層で最前面に
        cardRoot.SetAsLastSibling();

        // ① 0.5秒かけて -180度→-90度 の回転と同時にスケールを2倍に
        yield return StartCoroutine(SSRFlipAndScaleUp());

        // ② -90度に到達した時点で裏面を非表示にする
        backImage.SetActive(false);

        // ③ 0.9秒かけてスケールを1に戻す
        yield return StartCoroutine(ScaleTo(Vector3.one, ssrScaleDownTime));

        // めくり終わったので元のsiblingIndexに戻す
        cardRoot.SetSiblingIndex(originalSiblingIndex);

        isRevealed = true;
    }

    // -180度→-90度の回転と、スケール1→2を同時に0.5秒かけて行う
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

    // スケールのみを指定時間かけて変化させる（回転は-90→0度へ同時に戻す）
    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = cardRoot.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            cardRoot.localScale = Vector3.Lerp(startScale, targetScale, t);
            cardRoot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(ssrSwitchAngle, END_ANGLE, t), 0f);

            yield return null;
        }

        cardRoot.localScale = targetScale;
        cardRoot.localRotation = Quaternion.Euler(0f, END_ANGLE, 0f);
    }

    // 指定角度から指定角度までY軸回転させる（通常演出用）
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
        cardButton.interactable = false;
    }
}