using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // シーン遷移時に呼ぶ
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    // 起動時の初期化処理に呼ぶ
    public void LoadInitialize(System.Action onComplete)
    {
        StartCoroutine(InitializeAsync(onComplete));
    }

    // シーン遷移のロード処理
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        LoadingUI.Instance.Show();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // Unityのロード進行度は0～0.9まで
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            LoadingUI.Instance.SetProgress(progress);

            // 90%まで来たら完了とみなして画面を切り替える
            if (operation.progress >= 0.9f)
            {
                LoadingUI.Instance.SetProgress(1f);
                yield return new WaitForSeconds(0.3f); // 100%を少し見せる
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        LoadingUI.Instance.Hide();
    }

    // 起動時の初期化ロード処理
    private IEnumerator InitializeAsync(System.Action onComplete)
    {
        LoadingUI.Instance.Show();

        // 初期化処理を複数ステップに分けて進捗を表示
        yield return StartCoroutine(InitStep("カードデータ読み込み中", 0.0f, 0.4f,
            () =>
            {
                // 例：CardDatabase.LoadAllCards()など重い処理
            }));

        yield return StartCoroutine(InitStep("マスターデータ読み込み中", 0.4f, 0.7f,
            () =>
            {
                // 例：その他マスターデータの初期化
            }));

        yield return StartCoroutine(InitStep("セーブデータ読み込み中", 0.7f, 1.0f,
            () =>
            {
                // 例：SaveManager.Load()
            }));

        yield return new WaitForSeconds(0.3f);
        LoadingUI.Instance.Hide();
        onComplete?.Invoke();
    }

    // 各初期化ステップ
    private IEnumerator InitStep(string label, float startProgress, float endProgress,
        System.Action process)
    {
        LoadingUI.Instance.SetProgress(startProgress);
        yield return null; // 1フレーム待ってUIを更新

        process?.Invoke();

        // startからendまで滑らかに進行
        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            LoadingUI.Instance.SetProgress(Mathf.Lerp(startProgress, endProgress, t));
            yield return null;
        }
    }
}