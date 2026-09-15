using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// モード選択ボタンの押下演出。
/// 「縮小 → 拡大 → 所属パネルのフェードアウト」を再生し、完了後に画面切り替えを呼び出す。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ModeButtonPressEffect : MonoBehaviour
{
    [Header("押し込み")]
    [SerializeField] private float shrinkScale = 0.92f;
    [SerializeField] private float shrinkDuration = 0.08f;

    [Header("拡大")]
    [SerializeField] private float expandScale = 1.18f;
    [SerializeField] private float expandDuration = 0.22f;

    [Header("フェードアウト")]
    [Tooltip("押下後に消えるパネル。未指定の場合は親オブジェクトを対象にする")]
    [SerializeField] private CanvasGroup fadeOutTarget;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [Tooltip("拡大のどの時点からフェードアウトを重ねるか（拡大時間に対する割合）")]
    [SerializeField, Range(0f, 1f)] private float fadeOutStartRatio = 0.5f;

    private RectTransform rectTransform;
    private Vector3 defaultScale = Vector3.one;
    private Sequence sequence;
    private bool initialized;
    private bool isPlaying;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // 拡大したままの状態でパネルに戻ってこないよう、見た目を初期化する
        Initialize();
        rectTransform.localScale = defaultScale;
        isPlaying = false;

        // パネル側に登場演出(PanelEntranceAnimator)がある場合、alphaはそちらが管理するため触らない
        if (fadeOutTarget != null && fadeOutTarget.GetComponent<PanelEntranceAnimator>() == null)
        {
            fadeOutTarget.alpha = 1f;
            fadeOutTarget.blocksRaycasts = true;
        }
    }

    private void OnDisable()
    {
        KillSequence();
        isPlaying = false;
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    /// <summary>
    /// 押下演出を再生し、完了後に onComplete を呼ぶ。
    /// 演出中に再度呼ばれた場合は無視する（二重遷移の防止）。
    /// </summary>
    public void Play(Action onComplete)
    {
        Initialize();

        if (isPlaying)
            return;

        // 非アクティブ中はトゥイーンが進まないため、演出を挟まず即座に完了させる
        if (!gameObject.activeInHierarchy)
        {
            onComplete?.Invoke();
            return;
        }

        isPlaying = true;
        KillSequence();

        rectTransform.localScale = defaultScale;

        sequence = DOTween.Sequence().SetTarget(this);
        sequence.Append(rectTransform.DOScale(defaultScale * shrinkScale, shrinkDuration).SetEase(Ease.OutQuad));
        sequence.Append(rectTransform.DOScale(defaultScale * expandScale, expandDuration).SetEase(Ease.OutCubic));

        if (fadeOutTarget != null)
        {
            // 演出中の再入力を止めたうえで、拡大の途中からパネル全体をフェードアウトさせる
            fadeOutTarget.blocksRaycasts = false;
            sequence.Insert(shrinkDuration + expandDuration * fadeOutStartRatio,
                            fadeOutTarget.DOFade(0f, fadeOutDuration));
        }

        sequence.OnComplete(() =>
        {
            sequence = null;
            isPlaying = false;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// ボタンの拡大とパネルのフェードアウトを初期状態へ戻す。
    /// パネルを非アクティブにせずに元の画面へ戻る場合（ガチャを閉じた時など）に呼ぶ。
    /// </summary>
    public void ResetState()
    {
        Initialize();
        KillSequence();

        isPlaying = false;
        rectTransform.localScale = defaultScale;

        if (fadeOutTarget != null)
        {
            fadeOutTarget.alpha = 1f;
            fadeOutTarget.blocksRaycasts = true;
        }
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        rectTransform = (RectTransform)transform;
        defaultScale = rectTransform.localScale;

        if (fadeOutTarget == null && transform.parent != null)
            fadeOutTarget = GetOrAddCanvasGroup(transform.parent.gameObject);
    }

    private void KillSequence()
    {
        if (sequence == null)
            return;

        sequence.Kill();
        sequence = null;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = target.AddComponent<CanvasGroup>();

        return canvasGroup;
    }
}
