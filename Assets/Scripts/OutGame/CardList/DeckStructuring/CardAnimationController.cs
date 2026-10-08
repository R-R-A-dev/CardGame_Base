using DG.Tweening;
using TMPro;
using UnityEngine;

public class CardAnimationController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;


    /// <summary>
    /// カードがだんだん透明になって広がる演出
    /// （消えた後に元のサイズ・透明度に戻す）
    /// </summary>
    /// <param name="duration">アニメーション時間</param>
    /// <param name="scaleFactor">どれくらい拡大するか</param>
    public void PlayFadeOutAndExpand(float duration = 0.5f, float scaleFactor = 1.5f)
    {
        gameObject.SetActive(true);
        // 元の状態を保存
        Vector3 originalScale = rectTransform.localScale;
        float originalAlpha = canvasGroup.alpha;

        // アニメーションの設定
        Sequence seq = DOTween.Sequence();

        seq.Join(rectTransform.DOScale(originalScale * scaleFactor, duration))  // 拡大
           .Join(canvasGroup.DOFade(0f, duration))                              // フェードアウト
           .OnComplete(() =>
           {
               // 元の状態に戻す
               rectTransform.localScale = originalScale;
               canvasGroup.alpha = originalAlpha;
           });
    }

    /// <summary>
    /// カードが指定位置まで移動する演出
    /// </summary>
    /// <param name="target">移動先 Transform</param>
    /// <param name="duration">移動時間</param>
    /// <param name="ease">補間カーブ</param>
    public void PlayMoveTo(Vector3 target, float duration = 0.5f, Ease ease = Ease.InOutQuad)
    {
        if (target == null)
        {
            Debug.LogWarning("PlayMoveTo: target が指定されていません");
            return;
        }

        rectTransform.DOMove(target, duration).SetEase(ease);
    }

    /// <summary>
    /// 現在の位置から任意の位置まで移動（Transformを使わずに座標指定）
    /// </summary>
    public void PlayMoveToPosition(Vector3 position, float duration = 0.5f, Ease ease = Ease.InOutQuad)
    {
        rectTransform.DOMove(position, duration).SetEase(ease);
    }
}
