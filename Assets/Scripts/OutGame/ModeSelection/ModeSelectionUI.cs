using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelectionUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject modeSelectPanel;      // モード選択メインパネル
    [SerializeField] private GameObject lethalPuzzlePanel;    // 詰将棋選択パネル
    [SerializeField] private GameObject twoPickPanel;         // 2Pick選択パネル
    [SerializeField] private GameObject roguelikePanel;       // ローグライク選択パネル

    [Header("シーン名設定")]
    [SerializeField] private string battleScene = "Battle";
    [SerializeField] private string titleScene = "Title";

    private void Start()
    {
        // 初期状態：モード選択パネルのみ表示
        ShowModeSelectPanel();
    }

    // ========================================
    // パネル表示制御
    // ========================================

    /// <summary>
    /// モード選択パネルを表示
    /// </summary>
    private void ShowModeSelectPanel()
    {
        modeSelectPanel.SetActive(true);
        lethalPuzzlePanel.SetActive(false);
        twoPickPanel.SetActive(false);
        roguelikePanel.SetActive(false);
    }

    /// <summary>
    /// 詰将棋選択パネルを表示
    /// </summary>
    private void ShowLethalPuzzlePanel()
    {
        modeSelectPanel.SetActive(false);
        lethalPuzzlePanel.SetActive(true);
        twoPickPanel.SetActive(false);
        roguelikePanel.SetActive(false);
    }

    /// <summary>
    /// 2Pick選択パネルを表示
    /// </summary>
    private void ShowTwoPickPanel()
    {
        modeSelectPanel.SetActive(false);
        lethalPuzzlePanel.SetActive(false);
        twoPickPanel.SetActive(true);
        roguelikePanel.SetActive(false);
    }

    /// <summary>
    /// ローグライク選択パネルを表示
    /// </summary>
    private void ShowRoguelikePanel()
    {
        modeSelectPanel.SetActive(false);
        lethalPuzzlePanel.SetActive(false);
        twoPickPanel.SetActive(false);
        roguelikePanel.SetActive(true);
    }

    // ========================================
    // モード選択ボタン（メインパネル）
    // ========================================

    /// <summary>
    /// 詰将棋モードボタン
    /// </summary>
    public void OnClickLethalPuzzleMode()
    {
        ShowLethalPuzzlePanel();
    }

    /// <summary>
    /// 2Pickモードボタン
    /// </summary>
    public void OnClickTwoPickMode()
    {
        ShowTwoPickPanel();
    }

    /// <summary>
    /// ローグライクモードボタン
    /// </summary>
    public void OnClickRoguelikeMode()
    {
        ShowRoguelikePanel();
    }

    /// <summary>
    /// タイトルに戻る
    /// </summary>
    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene(titleScene);
    }

    // ========================================
    // 戻るボタン（各サブパネル）
    // ========================================

    /// <summary>
    /// モード選択に戻る
    /// </summary>
    public void OnClickBackToModeSelect()
    {
        ShowModeSelectPanel();
    }
}