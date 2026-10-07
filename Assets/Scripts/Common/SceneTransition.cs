using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// シーン遷移時の暗転演出。
/// 暗転 → シーンを非同期で読み込む → 読み込み完了後に明るくする、の順で遷移する。
/// DontDestroyOnLoad で常駐し、暗転用の Canvas / Image は Awake で自動生成する。
///
/// 各スクリプトからは SceneManager.LoadScene の代わりに SceneTransition.Load を呼ぶこと。
/// Game シーンから直接再生した場合など、このコンポーネントが存在しない時は
/// 従来通り SceneManager.LoadScene で即座に遷移する。
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private Color fadeColor = Color.black;
    [Tooltip("暗転用 Canvas の Sort Order。他のどの UI よりも手前に描画する")]
    [SerializeField] private int sortingOrder = short.MaxValue;

    private CanvasGroup canvasGroup;
    private Tween fadeTween;
    private bool isTransitioning;

    private void Awake()
    {
        // Field シーンへ戻ってきた時にシーン側の新しいインスタンスが重複するため、後から来た方を破棄する
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        fadeTween?.Kill();
        Instance = null;
    }

    // ========================================
    // 呼び出し口
    // ========================================

    /// <summary>
    /// 暗転を挟んでシーンを読み込む。コンポーネントが存在しない場合は即座に遷移する。
    /// </summary>
    public static void Load(string sceneName)
    {
        if (Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Instance.StartTransition(() => SceneManager.LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// 暗転を挟んでシーンを読み込む（ビルドインデックス指定）。
    /// </summary>
    public static void Load(int sceneBuildIndex)
    {
        if (Instance == null)
        {
            SceneManager.LoadScene(sceneBuildIndex);
            return;
        }

        Instance.StartTransition(() => SceneManager.LoadSceneAsync(sceneBuildIndex));
    }

    /// <summary>
    /// シーン遷移を伴わない画面切り替え用。暗転しきった時点で onDark を実行し、その後明るくする。
    /// コンポーネントが存在しない場合は即座に onDark を実行する。
    /// </summary>
    public static void FadeAndRun(System.Action onDark)
    {
        if (Instance == null)
        {
            onDark?.Invoke();
            return;
        }

        Instance.StartTransition(() =>
        {
            onDark?.Invoke();
            return null;
        });
    }

    // ========================================
    // 遷移処理
    // ========================================

    private void StartTransition(System.Func<AsyncOperation> loadScene)
    {
        // 暗転中にボタンの連打や終了処理の重複で再度呼ばれても、最初の遷移だけを行う
        if (isTransitioning)
            return;

        StartCoroutine(TransitionRoutine(loadScene));
    }

    private IEnumerator TransitionRoutine(System.Func<AsyncOperation> loadScene)
    {
        isTransitioning = true;

        // 暗転が始まった時点で入力を止める
        canvasGroup.blocksRaycasts = true;
        yield return Fade(1f, fadeOutDuration);

        // FadeAndRun の場合は読み込みを伴わないため null が返る
        AsyncOperation operation = loadScene();
        while (operation != null && !operation.isDone)
            yield return null;

        yield return Fade(0f, fadeInDuration);
        canvasGroup.blocksRaycasts = false;

        isTransitioning = false;
    }

    private IEnumerator Fade(float endAlpha, float duration)
    {
        fadeTween?.Kill();
        // timeScale を変更している場面（ポーズや演出のスロー）でも一定の速さで暗転させる
        fadeTween = canvasGroup.DOFade(endAlpha, duration).SetUpdate(true).SetTarget(this);
        yield return fadeTween.WaitForCompletion();
        fadeTween = null;
    }

    // ========================================
    // 暗転用 UI の生成
    // ========================================

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("FadeCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        // blocksRaycasts で入力を止めるには GraphicRaycaster が必要
        canvasObject.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject imageObject = new GameObject("FadeImage", typeof(RectTransform));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = fadeColor;
    }
}
