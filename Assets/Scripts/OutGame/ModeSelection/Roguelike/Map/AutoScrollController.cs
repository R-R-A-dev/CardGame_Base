using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AutoScrollController : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;

    [Header("スクロール設定")]
    [SerializeField] private float scrollDuration = 0.5f; // スクロールにかかる時間
    [SerializeField] private AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine scrollCoroutine;

    void Start()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
    }

    /// <summary>
    /// 正規化された位置までスクロール (0=一番上, 1=一番下)
    /// </summary>
    public void ScrollToNormalizedPosition(float normalizedPosition)
    {
        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);

        scrollCoroutine = StartCoroutine(ScrollToPositionCoroutine(normalizedPosition));
    }

    /// <summary>
    /// 一番上までスクロール
    /// </summary>
    public void ScrollToTop()
    {
        ScrollToNormalizedPosition(1f);
    }

    /// <summary>
    /// 一番下までスクロール
    /// </summary>
    public void ScrollToBottom()
    {
        ScrollToNormalizedPosition(0f);
    }

    /// <summary>
    /// 中央までスクロール
    /// </summary>
    public void ScrollToCenter()
    {
        ScrollToNormalizedPosition(0.5f);
    }

    /// <summary>
    /// 指定した子オブジェクトまでスクロール
    /// </summary>
    public void ScrollToChild(RectTransform target)
    {
        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);

        scrollCoroutine = StartCoroutine(ScrollToChildCoroutine(target));
    }

    private IEnumerator ScrollToPositionCoroutine(float targetPosition)
    {
        float startPosition = scrollRect.verticalNormalizedPosition;
        float elapsedTime = 0f;

        while (elapsedTime < scrollDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / scrollDuration);
            float curveValue = scrollCurve.Evaluate(t);

            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, curveValue);

            yield return null;
        }

        scrollRect.verticalNormalizedPosition = targetPosition;
        scrollCoroutine = null;
    }

    private IEnumerator ScrollToChildCoroutine(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        // ターゲットの位置を計算
        Vector2 targetPos = (Vector2)scrollRect.transform.InverseTransformPoint(content.position)
                          - (Vector2)scrollRect.transform.InverseTransformPoint(target.position);

        // コンテンツの高さとビューポートの高さ
        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        // スクロール可能な範囲
        float scrollableHeight = contentHeight - viewportHeight;

        if (scrollableHeight <= 0)
        {
            yield break;
        }

        // 正規化された位置を計算
        float normalizedPosition = Mathf.Clamp01(targetPos.y / scrollableHeight);

        // スクロール実行
        float startPosition = scrollRect.verticalNormalizedPosition;
        float elapsedTime = 0f;

        while (elapsedTime < scrollDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / scrollDuration);
            float curveValue = scrollCurve.Evaluate(t);

            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPosition, normalizedPosition, curveValue);

            yield return null;
        }

        scrollRect.verticalNormalizedPosition = normalizedPosition;
        scrollCoroutine = null;
    }

    /// <summary>
    /// 即座に位置を変更（アニメーションなし）
    /// </summary>
    public void SetPositionImmediate(float normalizedPosition)
    {
        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);

        scrollRect.verticalNormalizedPosition = normalizedPosition;
    }
}