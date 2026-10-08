using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    public static LoadingUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいで保持
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        loadingPanel.SetActive(false);
    }

    public void Show()
    {
        loadingPanel.SetActive(true);
        SetProgress(0f);
    }

    public void Hide()
    {
        loadingPanel.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        progressBar.value = progress;
        progressText.text = $"{Mathf.FloorToInt(progress * 100)}%";
    }
}