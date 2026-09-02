using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelectionUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject modeSelectPanel;      // モード選択メインパネル
    [SerializeField] private GameObject cpuBattlePanel;       // CPU戦選択パネル
    [SerializeField] private GameObject lethalPuzzlePanel;    // 詰みパズル選択パネル
    [SerializeField] private GameObject twoPickPanel;         // 2Pick選択パネル
    [SerializeField] private GameObject roguelikePanel;       // ローグライク選択パネル

    [Header("シーン名設定")]
    [SerializeField] private string battleScene = "Battle";
    [SerializeField] private string titleScene = "Title";

    private void Start()
    {
        // デバッグ対応：GameModeを設定せずに対戦した場合、モード選択・各サブパネルを
        // 一切経由せず直接デッキ編成画面へ戻す
        if (GameSession.DebugReturnToDeckEdit)
        {
            GameSession.DebugReturnToDeckEdit = false;

            modeSelectPanel.SetActive(false);
            cpuBattlePanel.SetActive(false);
            lethalPuzzlePanel.SetActive(false);
            twoPickPanel.SetActive(false);
            roguelikePanel.SetActive(false);

            // DeckEditパネルは既定で非アクティブなため、非アクティブ含めて検索する
            DeckBuilderUI deckBuilderUI = FindFirstObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);
            deckBuilderUI.ShowDeckEditPanel();
            DeckBuilderManager.Instance.OpenDeckEditForDeck(GameSession.DebugReturnDeckNum);
            return;
        }

        // 復帰対応：戦闘シーンから戻ってきた場合、それより先に実行されるRoguelikeSceneController.Awake()が
        // ローグライクパネルを有効化している。(Start()の実行順序は保証されないため
        // RoguelikeSession.GameStateのチェックでは弱いため、実際のアクティブ状態を参照)
        if (roguelikePanel.activeSelf)
            return;

        // 復帰対応：CPU戦から戻ってきた場合、CPU戦選択パネルを表示（結果パネルはCpuBattleSelectionPanel側で表示）
        if (GameSession.CpuBattleFinished)
        {
            ShowCpuBattlePanel();
            return;
        }

        // 復帰対応：詰みパズルから戻ってきた場合、詰みパズル選択パネルを表示（結果パネルはLethalPuzzleSelectionPanel側で表示）
        if (GameSession.LethalPuzzleFinished)
        {
            ShowLethalPuzzlePanel();
            return;
        }

        // 復帰対応：2Pickから戻ってきた場合（敗北/やめる）、2Pick選択パネルを表示
        if (GameSession.TwoPickFinished)
        {
            ShowTwoPickPanel();
            return;
        }

        // 初期状態：モード選択パネルのみ表示
        ShowModeSelectPanel();
    }

    // ========================================
    // パネル表示処理
    // ========================================

    /// <summary>
    /// モード選択パネルを表示
    /// </summary>
    public void ShowModeSelectPanel()
    {
        modeSelectPanel.SetActive(true);
        cpuBattlePanel.SetActive(false);
        lethalPuzzlePanel.SetActive(false);
        twoPickPanel.SetActive(false);
        roguelikePanel.SetActive(false);
    }

    /// <summary>
    /// CPU戦選択パネルを表示
    /// </summary>
    private void ShowCpuBattlePanel()
    {
        modeSelectPanel.SetActive(false);
        cpuBattlePanel.SetActive(true);
        lethalPuzzlePanel.SetActive(false);
        twoPickPanel.SetActive(false);
        roguelikePanel.SetActive(false);
    }

    /// <summary>
    /// 詰みパズル選択パネルを表示
    /// </summary>
    private void ShowLethalPuzzlePanel()
    {
        modeSelectPanel.SetActive(false);
        cpuBattlePanel.SetActive(false);
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
        cpuBattlePanel.SetActive(false);
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
        cpuBattlePanel.SetActive(false);
        lethalPuzzlePanel.SetActive(false);
        twoPickPanel.SetActive(false);
        roguelikePanel.SetActive(true);
    }

    // ========================================
    // モード選択ボタン（メインパネル）
    // ========================================

    /// <summary>
    /// CPU戦モードボタン
    /// </summary>
    public void OnClickCpuBattleMode()
    {
        if (ModeConfigManager.Instance != null)
            ModeConfigManager.Instance.ChangeMode(GameMode.CPU_BATTLE);

        ShowCpuBattlePanel();
    }

    /// <summary>
    /// 詰みパズルモードボタン
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

    /// <summary>
    /// CPU戦選択パネルから戻る
    /// </summary>
    public void OnClickBackFromCpuBattle()
    {
        if (ModeConfigManager.Instance != null)
            ModeConfigManager.Instance.ChangeMode(GameMode.NONE);

        ShowModeSelectPanel();
    }
}