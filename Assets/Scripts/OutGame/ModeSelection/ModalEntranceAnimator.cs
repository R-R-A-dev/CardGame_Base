using DG.Tweening;
using UnityEngine;

/// <summary>
/// モーダル表示時の登場演出。
/// モーダルのルート（暗幕）をフェードインさせながら、ウィンドウ本体を少し小さい状態から拡大させる。
/// ルートが SetActive(true) された時点で自動的に再生される。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ModalEntranceAnimator : MonoBehaviour
{
    [Header("対象")]
    [Tooltip("拡大させるウィンドウ本体。未指定の場合はこのオブジェクト自身を対象にする")]
    [SerializeField] private RectTransform window;

    [Header("フェードイン（ルート）")]
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("拡大（ウィンドウ本体）")]
    [SerializeField] private float startScale = 0.9f;
    [SerializeField] private float scaleDuration = 0.2f;

    private CanvasGroup canvasGroup;
    private RectTransform scaleTarget;
    private Vector3 defaultScale = Vector3.one;
    private Sequence sequence;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        // 演出の途中で閉じられた場合、次に開いた時のために見た目を戻しておく
        KillSequence();
        RestoreImmediate();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    /// <summary>
    /// 登場演出を最初から再生する。
    /// </summary>
    public void Play()
    {
        Initialize();
        KillSequence();

        // 非アクティブ中はトゥイーンが進まないため、演出せずに完成状態にする
        if (!gameObject.activeInHierarchy)
        {
            RestoreImmediate();
            return;
        }

        canvasGroup.alpha = 0f;
        scaleTarget.localScale = defaultScale * startScale;

        sequence = DOTween.Sequence().SetTarget(this);
        sequence.Insert(0f, canvasGroup.DOFade(1f, fadeDuration));
        sequence.Insert(0f, scaleTarget.DOScale(defaultScale, scaleDuration).SetEase(Ease.OutBack));
        sequence.OnComplete(() => sequence = null);
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        canvasGroup = GetOrAddCanvasGroup(gameObject);
        scaleTarget = window != null ? window : (RectTransform)transform;
        defaultScale = scaleTarget.localScale;
    }

    /// <summary>
    /// 演出を行わずに、表示し終わった状態へ戻す。
    /// </summary>
    private void RestoreImmediate()
    {
        Initialize();

        canvasGroup.alpha = 1f;
        scaleTarget.localScale = defaultScale;
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
