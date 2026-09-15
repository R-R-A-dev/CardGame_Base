using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// パネル表示時の登場演出。
/// パネル全体をフェードインさせながら、ボタンや文字などの子要素を横から順番にスライドインさせる。
/// パネルが SetActive(true) された時点で自動的に再生される。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PanelEntranceAnimator : MonoBehaviour
{
    public enum SlideDirection
    {
        Left,   // 画面左側から入ってくる
        Right,  // 画面右側から入ってくる
    }

    [Header("フェードイン")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [Tooltip("パネル表示からフェードイン開始までの待ち時間")]
    [SerializeField] private float fadeInDelay = 0f;

    [Header("スライドイン")]
    [SerializeField] private SlideDirection slideFrom = SlideDirection.Right;
    [SerializeField] private float slideDistance = 160f;
    [SerializeField] private float slideDuration = 0.35f;
    [Tooltip("子要素ごとに開始時間をずらす量")]
    [SerializeField] private float stagger = 0.05f;
    [Tooltip("子要素を個別にもフェードインさせる")]
    [SerializeField] private bool fadeChildrenIndividually = true;

    [Header("対象")]
    [Tooltip("スライドさせる要素。未指定の場合は直下の子要素を自動的に対象にする")]
    [SerializeField] private List<RectTransform> slideTargets = new List<RectTransform>();
    [Tooltip("自動収集時に除外する要素（背景など、動かしたくないもの）")]
    [SerializeField] private List<RectTransform> excludeFromSlide = new List<RectTransform>();

    private CanvasGroup canvasGroup;
    private readonly List<RectTransform> targets = new List<RectTransform>();
    private readonly List<Vector2> defaultPositions = new List<Vector2>();
    private readonly List<CanvasGroup> targetCanvasGroups = new List<CanvasGroup>();
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
        // 演出の途中で非表示になった場合、次に表示した時のために見た目を戻しておく
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

        Vector2 offset = new Vector2(slideFrom == SlideDirection.Left ? -slideDistance : slideDistance, 0f);

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        sequence = DOTween.Sequence().SetTarget(this);
        sequence.Insert(fadeInDelay, canvasGroup.DOFade(1f, fadeInDuration));

        for (int i = 0; i < targets.Count; i++)
        {
            RectTransform target = targets[i];
            if (target == null)
                continue;

            float startTime = fadeInDelay + stagger * i;

            target.anchoredPosition = defaultPositions[i] + offset;
            sequence.Insert(startTime,
                            target.DOAnchorPos(defaultPositions[i], slideDuration).SetEase(Ease.OutCubic));

            if (!fadeChildrenIndividually)
                continue;

            CanvasGroup targetCanvasGroup = targetCanvasGroups[i];
            targetCanvasGroup.alpha = 0f;
            sequence.Insert(startTime, targetCanvasGroup.DOFade(1f, slideDuration));
        }

        sequence.OnComplete(() =>
        {
            sequence = null;
            canvasGroup.blocksRaycasts = true;
        });
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        canvasGroup = GetOrAddCanvasGroup(gameObject);

        CollectTargets();

        defaultPositions.Clear();
        targetCanvasGroups.Clear();
        foreach (RectTransform target in targets)
        {
            defaultPositions.Add(target.anchoredPosition);
            targetCanvasGroups.Add(GetOrAddCanvasGroup(target.gameObject));
        }
    }

    private void CollectTargets()
    {
        targets.Clear();

        if (slideTargets.Count > 0)
        {
            foreach (RectTransform target in slideTargets)
            {
                if (target != null)
                    targets.Add(target);
            }
            return;
        }

        // 未指定の場合は直下の子要素を対象にする
        foreach (Transform child in transform)
        {
            RectTransform target = child as RectTransform;
            if (target == null || excludeFromSlide.Contains(target))
                continue;

            targets.Add(target);
        }
    }

    /// <summary>
    /// 演出を行わずに、表示し終わった状態へ戻す。
    /// </summary>
    private void RestoreImmediate()
    {
        Initialize();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
                continue;

            targets[i].anchoredPosition = defaultPositions[i];
            targetCanvasGroups[i].alpha = 1f;
        }
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
