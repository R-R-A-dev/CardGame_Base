using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MapNodeUI : MonoBehaviour
{
    [SerializeField] private Button nodeButton;

    [Header("選択可能時の点滅")]
    [SerializeField] private Image blinkImage;                  // 未設定なら子オブジェクトのImageを自動取得
    [SerializeField] private float blinkMinAlpha = 0f;
    [SerializeField] private float blinkDuration = 0.4f;

    private NodeData nodeData;
    private System.Action<NodeData> onNodeClicked;

    private bool isSelectable;
    private bool isBlinkInitialized;
    private float blinkBaseAlpha = 1f;
    private Tween blinkTween;

    // 非アクティブのまま SetSelectable が呼ばれることがあるため Awake ではなく遅延初期化
    private void InitializeBlink()
    {
        if (isBlinkInitialized) return;
        isBlinkInitialized = true;

        if (blinkImage == null)
        {
            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject == gameObject) continue;
                blinkImage = image;
                break;
            }
        }

        if (blinkImage != null)
            blinkBaseAlpha = blinkImage.color.a;
    }

    private void OnEnable()
    {
        if (isSelectable) StartBlink();
    }

    private void OnDisable()
    {
        StopBlink();
    }

    public void Setup(NodeData data, bool isSelectable, System.Action<NodeData> onClicked)
    {
        nodeData = data;
        onNodeClicked = onClicked;

        SetSelectable(isSelectable);

        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(() => onNodeClicked?.Invoke(nodeData));
    }

    public void SetSelectable(bool isSelectable)
    {
        this.isSelectable = isSelectable;
        nodeButton.interactable = isSelectable;

        if (isSelectable && isActiveAndEnabled) StartBlink();
        else StopBlink();
    }

    private void StartBlink()
    {
        InitializeBlink();
        if (blinkImage == null || blinkTween != null) return;

        SetBlinkAlpha(blinkBaseAlpha);
        blinkTween = blinkImage.DOFade(blinkBaseAlpha * blinkMinAlpha, blinkDuration)
            .SetEase(Ease.InOutExpo)   // 両端で溜めて素早く切り替わる、メリハリのある明滅
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void StopBlink()
    {
        InitializeBlink();
        blinkTween?.Kill();
        blinkTween = null;

        if (blinkImage != null) SetBlinkAlpha(blinkBaseAlpha);
    }

    private void SetBlinkAlpha(float alpha)
    {
        Color color = blinkImage.color;
        color.a = alpha;
        blinkImage.color = color;
    }
}
