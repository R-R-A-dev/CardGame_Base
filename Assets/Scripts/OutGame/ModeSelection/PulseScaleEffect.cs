using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゆっくり拡大縮小を繰り返すパルス演出。
/// 非表示（OnDisable）になると停止して元のスケールに戻る。
/// </summary>
public class PulseScaleEffect : MonoBehaviour
{
    [Header("パルス")]
    [SerializeField] private float maxScale = 1.06f;
    [Tooltip("片道（1.0→最大）にかける時間")]
    [SerializeField] private float halfDuration = 0.8f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    [Header("停止条件（任意）")]
    [Tooltip("指定するとこのボタンが押せない間は停止する")]
    [SerializeField] private Button interactableSource;
    [Tooltip("指定するとこのオブジェクトが表示されている間は停止する（全画面パネルに覆われる場合など）")]
    [SerializeField] private GameObject coverObject;

    private Vector3 defaultScale;
    private Tween pulseTween;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    private void OnEnable()
    {
        UpdatePulse();
    }

    private void OnDisable()
    {
        StopPulse();
    }

    private void OnDestroy()
    {
        pulseTween?.Kill();
    }

    private void Update()
    {
        UpdatePulse();
    }

    private void UpdatePulse()
    {
        if (ShouldPlay()) StartPulse();
        else StopPulse();
    }

    private bool ShouldPlay()
    {
        if (interactableSource != null && !interactableSource.interactable) return false;
        if (coverObject != null && coverObject.activeInHierarchy) return false;
        return true;
    }

    private void StartPulse()
    {
        if (pulseTween != null) return;

        transform.localScale = defaultScale;
        pulseTween = transform.DOScale(defaultScale * maxScale, halfDuration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void StopPulse()
    {
        if (pulseTween == null) return;

        pulseTween.Kill();
        pulseTween = null;
        transform.localScale = defaultScale;
    }
}
