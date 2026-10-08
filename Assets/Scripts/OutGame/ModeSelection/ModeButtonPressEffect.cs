using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// モード選択ボタンの押下演出。
///
/// ・押した瞬間            … 縮小する
/// ・ボタン上で離した場合  … 元より大きく拡大し、押した判定として画面遷移へ進む
/// ・ボタン外で離した場合  … 元のサイズまで戻るだけで、遷移しない
///
/// 「ボタン外で離した場合は押した判定にしない」判定自体は Unity の Button が行うため、
/// このクラスは見た目（スケールとフェード）のみを担当する。
/// なお IDragHandler は実装してはいけない。実装すると入力モジュールがドラッグ開始時点で
/// OnPointerUp を発火させてしまい、押下判定と演出の両方が壊れる。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ModeButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("押し込み")]
    [SerializeField] private float shrinkScale = 0.92f;
    [SerializeField] private float shrinkDuration = 0.08f;

    [Header("拡大（押した判定が成立した場合のみ）")]
    [SerializeField] private float expandScale = 1.18f;
    [SerializeField] private float expandDuration = 0.22f;

    [Header("復帰（ボタン外で離した場合）")]
    [Tooltip("元のサイズへ戻るまでの時間。拡大はせず等倍で止まる")]
    [SerializeField] private float revertDuration = 0.15f;

    [Header("フェードアウト")]
    [Tooltip("押下後に消えるパネル。未指定の場合は親オブジェクトを対象にする。" +
             "ボタンがHeaderなどの中間オブジェクトの下にある場合は、パネル本体を明示的に指定すること。" +
             "CanvasGroupは実行時に自動で追加されるため、あらかじめ付けておく必要はない")]
    [SerializeField] private GameObject fadeOutPanel;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [Tooltip("拡大のどの時点からフェードアウトを重ねるか（拡大時間に対する割合）")]
    [SerializeField, Range(0f, 1f)] private float fadeOutStartRatio = 0.5f;

    [Header("演出完了後の処理")]
    [Tooltip("PlayAndInvoke() で再生した場合に、演出の完了後に実行する処理。" +
             "ボタンのOnClickには本来の処理ではなく PlayAndInvoke() を登録すること")]
    [SerializeField] private UnityEvent onEffectComplete;

    private RectTransform rectTransform;
    private Button button;
    // fadeOutPanel から解決した CanvasGroup（無ければ実行時に追加する）
    private CanvasGroup fadeOutTarget;
    private Vector3 defaultScale = Vector3.one;
    private Sequence sequence;
    private bool initialized;

    // 押下中（OnPointerDown を受けてから OnPointerUp を受けるまで）
    private bool isPressed;
    // OnPointerUp による復帰の再生中。この間に Button.onClick が来れば押した判定として扱う
    private bool isReleasing;
    // 押した判定が確定し、フェードアウトと画面遷移を再生中
    private bool isTransitioning;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // 拡大したままの状態でパネルに戻ってこないよう、見た目を初期化する
        Initialize();
        rectTransform.localScale = defaultScale;
        ClearFlags();

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
        ClearFlags();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    // ========================================
    // ポインタ入力
    // ========================================

    public void OnPointerDown(PointerEventData eventData)
    {
        Initialize();

        if (isTransitioning || (button != null && !button.IsInteractable()))
            return;

        isPressed = true;
        isReleasing = false;

        KillSequence();
        sequence = DOTween.Sequence().SetTarget(this);
        sequence.Append(rectTransform.DOScale(defaultScale * shrinkScale, shrinkDuration).SetEase(Ease.OutQuad));
        sequence.OnComplete(() => sequence = null);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // ボタン外で離した場合もこのボタン自身に OnPointerUp が届くため、ここでは内外を区別しない。
        // まずは押した判定が成立しなかった場合の動き（元のサイズへ戻るだけ）を再生し、
        // 同一フレームで Button.onClick が続いた場合に Play() が拡大へ差し替える。
        if (!isPressed || isTransitioning)
            return;

        isPressed = false;
        isReleasing = true;

        KillSequence();
        sequence = DOTween.Sequence().SetTarget(this);
        sequence.Append(rectTransform.DOScale(defaultScale, revertDuration).SetEase(Ease.OutCubic));
        sequence.OnComplete(() =>
        {
            sequence = null;
            isReleasing = false;
        });
    }

    // ========================================
    // 押した判定が成立した時の演出
    // ========================================

    /// <summary>
    /// 押した判定の成立時に、ボタンのOnClickから直接呼ぶためのエントリ。
    /// 演出の完了後に onEffectComplete を実行する。
    /// </summary>
    public void PlayAndInvoke()
    {
        Play(() => onEffectComplete?.Invoke());
    }

    /// <summary>
    /// 押した判定の成立時（Button.onClick 経由）に呼ぶ。
    /// 拡大に続けてパネルをフェードアウトさせ、完了後に onComplete を呼ぶ。
    /// </summary>
    public void Play(Action onComplete)
    {
        Initialize();

        if (isTransitioning)
            return;

        // 非アクティブ中はトゥイーンが進まないため、演出を挟まず即座に完了させる
        if (!gameObject.activeInHierarchy)
        {
            onComplete?.Invoke();
            return;
        }

        // OnPointerUp の直後（同一フレーム）に呼ばれるため、復帰はまだ進行していない。
        // 元のサイズへ戻すだけの動きを破棄し、拡大＋フェードアウトとして組み直す。
        bool needsShrink = !isReleasing;

        isPressed = false;
        isReleasing = false;
        isTransitioning = true;

        KillSequence();

        float expandStartTime = 0f;
        sequence = DOTween.Sequence().SetTarget(this);

        // ポインタを経由しない呼び出し（Submitキーやスクリプトからの実行）では縮小から再生する
        if (needsShrink)
        {
            sequence.Append(rectTransform.DOScale(defaultScale * shrinkScale, shrinkDuration).SetEase(Ease.OutQuad));
            expandStartTime = shrinkDuration;
        }

        sequence.Append(rectTransform.DOScale(defaultScale * expandScale, expandDuration).SetEase(Ease.OutCubic));

        if (fadeOutTarget != null)
        {
            // 演出中の再入力を止めたうえで、拡大の途中からパネル全体をフェードアウトさせる
            fadeOutTarget.blocksRaycasts = false;
            sequence.Insert(expandStartTime + expandDuration * fadeOutStartRatio,
                            fadeOutTarget.DOFade(0f, fadeOutDuration));
        }

        sequence.OnComplete(() =>
        {
            sequence = null;
            isTransitioning = false;
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
        ClearFlags();

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
        button = GetComponent<Button>();

        // 未指定の場合は親オブジェクトをフェード対象とする
        GameObject panel = fadeOutPanel != null ? fadeOutPanel
                         : (transform.parent != null ? transform.parent.gameObject : null);

        if (panel != null)
            fadeOutTarget = GetOrAddCanvasGroup(panel);
    }

    private void ClearFlags()
    {
        isPressed = false;
        isReleasing = false;
        isTransitioning = false;
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
