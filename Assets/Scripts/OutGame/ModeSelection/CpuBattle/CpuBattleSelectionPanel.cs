using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CpuBattleSelectionPanel : MonoBehaviour
{
    [Header("デッキ選択モーダル（自分・敵の変更ボタンで共通。8個のデッキボタンはシーン上に配置済み）")]
    [SerializeField] private GameObject deckListPanel;
    [SerializeField] private List<Button> deckButtons;

    [Header("デッキ確認モーダル（決定/デッキ編成/閉じる）")]
    [SerializeField] private GameObject deckConfirmPanel;
    [SerializeField] private Button decideDeckButton;

    [Header("デッキ編成画面")]
    [SerializeField] private GameObject deckEditPanel;

    [Header("選択結果表示")]
    [SerializeField] private TextMeshProUGUI selectedPlayerDeckText;
    [SerializeField] private TextMeshProUGUI selectedEnemyDeckText;

    [Header("対戦開始")]
    [SerializeField] private Button startBattleButton;
    [SerializeField] private string battleScene = "Game";

    [Header("勝利報酬パネル")]
    [SerializeField] private CpuBattleRewardUI cpuBattleRewardUI;

    private const int MIN_DECK_SIZE = 0; // TODO: 動作確認用に一旦解除。本来は40

    // デッキ選択モーダルを自分用/敵用のどちらのボタンから開いたか
    private bool isSelectingEnemyDeck;
    // モーダルで確認中のデッキ
    private int confirmingDeckId = -1;

    // 決定済みのデッキ
    private int decidedPlayerDeckId = -1;
    private int decidedEnemyDeckId = -1;

    private void Start()
    {
        // シーン上のデッキボタン数に対して保存データの枠が不足している場合、空デッキで埋めておく
        // （古いセーブデータや初期セーブデータの枠数がボタン数より少ない場合の保険）
        EnsureDeckSlotsExist();

        // シーン上に配置済みの8個のデッキボタンに、一度だけリスナーを登録する
        // （自分用・敵用どちらのモーダルとして開かれたかはisSelectingEnemyDeckをクリック時に参照して判定）
        for (int i = 0; i < deckButtons.Count; i++)
        {
            int deckId = i;
            deckButtons[i].onClick.AddListener(() => OnDeckSelected(deckId));
        }

        UpdateSelectedDeckText();
        UpdateStartBattleButton();

        // 戦闘から戻ってきた場合、勝利していれば報酬パネルを表示する
        if (GameSession.CpuBattleFinished)
        {
            bool won = GameSession.CpuBattleWon;
            GameSession.CpuBattleFinished = false;

            if (won)
                cpuBattleRewardUI.Open();
        }
    }

    // 「自分のデッキを変更」ボタン
    public void OpenPlayerDeckList()
    {
        isSelectingEnemyDeck = false;
        RefreshDeckButtonLabels();
        deckListPanel.SetActive(true);
    }

    // 「敵のデッキを変更」ボタン
    public void OpenEnemyDeckList()
    {
        isSelectingEnemyDeck = true;
        RefreshDeckButtonLabels();
        deckListPanel.SetActive(true);
    }

    // デッキ選択モーダルの「閉じる」ボタン
    public void CloseDeckListPanel()
    {
        deckListPanel.SetActive(false);
    }

    // シーン上のデッキボタン数に対して保存データの枠が不足している場合、空デッキで埋めておく
    private void EnsureDeckSlotsExist()
    {
        var decks = GameDataHolder.Instance.Data.cpuBattleDecks;
        while (decks.Count < deckButtons.Count)
            decks.Add(new DeckSaveData());
    }

    // 各デッキボタンのラベルをcpuBattleDecksのデッキ名で更新する
    private void RefreshDeckButtonLabels()
    {
        var decks = GameDataHolder.Instance.Data.cpuBattleDecks;

        for (int i = 0; i < deckButtons.Count; i++)
        {
            TextMeshProUGUI label = deckButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) continue;

            label.text = i < decks.Count && !string.IsNullOrEmpty(decks[i].deckName)
                ? decks[i].deckName
                : $"デッキ{i}";
        }
    }

    private void OnDeckSelected(int deckId)
    {
        confirmingDeckId = deckId;

        deckConfirmPanel.SetActive(true);
        UpdateDecideDeckButton();
    }

    private int GetDeckSize(int deckId)
    {
        var decks = GameDataHolder.Instance.Data.cpuBattleDecks;
        if (deckId < 0 || deckId >= decks.Count) return 0;
        return decks[deckId].cardIds.Count;
    }

    // 規定枚数未満のデッキは決定できないようにする
    private void UpdateDecideDeckButton()
    {
        decideDeckButton.interactable = GetDeckSize(confirmingDeckId) >= MIN_DECK_SIZE;
    }

    private void UpdateSelectedDeckText()
    {
        if (selectedPlayerDeckText != null)
            selectedPlayerDeckText.text = decidedPlayerDeckId >= 0 ? decidedPlayerDeckId.ToString() : "未選択";

        if (selectedEnemyDeckText != null)
            selectedEnemyDeckText.text = decidedEnemyDeckId >= 0 ? decidedEnemyDeckId.ToString() : "未選択";
    }

    private void UpdateStartBattleButton()
    {
        startBattleButton.interactable =
            decidedPlayerDeckId >= 0 && GetDeckSize(decidedPlayerDeckId) >= MIN_DECK_SIZE &&
            decidedEnemyDeckId >= 0 && GetDeckSize(decidedEnemyDeckId) >= MIN_DECK_SIZE;
    }

    // モーダル：「決定」ボタン
    public void DecideDeck()
    {
        if (isSelectingEnemyDeck)
            decidedEnemyDeckId = confirmingDeckId;
        else
            decidedPlayerDeckId = confirmingDeckId;

        deckConfirmPanel.SetActive(false);

        UpdateSelectedDeckText();
        UpdateStartBattleButton();
    }

    // モーダル：「デッキ編成」ボタン
    public void OpenDeckEdit()
    {
        // CPU戦専用デッキ（cpuBattleDecks）として編集されるようにしておく
        if (ModeConfigManager.Instance != null)
            ModeConfigManager.Instance.currentGameMode = GameMode.CPU_BATTLE;

        deckConfirmPanel.SetActive(false);
        deckListPanel.SetActive(false);

        deckEditPanel.SetActive(true);
        gameObject.SetActive(false);

        // 多重購読を避けるため、一度解除してから購読し直す
        DeckBuilderManager.Instance.OnDeckEditClosed -= OnDeckEditClosed;
        DeckBuilderManager.Instance.OnDeckEditClosed += OnDeckEditClosed;

        if (DeckBuilderManager.Instance.deckNum != confirmingDeckId)
            DeckBuilderManager.Instance.OpenDeckEditForDeck(confirmingDeckId);
    }

    private void OnDeckEditClosed()
    {
        gameObject.SetActive(true);

        // デッキ編成前に開いていたデッキ一覧・確認モーダルを表示に戻す
        deckListPanel.SetActive(true);
        deckConfirmPanel.SetActive(true);
        UpdateDecideDeckButton();

        UpdateSelectedDeckText();
        UpdateStartBattleButton();
    }

    // 確認モーダル：「閉じる」ボタン
    public void CloseConfirmModal()
    {
        deckConfirmPanel.SetActive(false);
    }

    // 対戦開始ボタン
    public void StartBattle()
    {
        var decks = GameDataHolder.Instance.Data.cpuBattleDecks;

        if (decidedPlayerDeckId < 0 || decidedPlayerDeckId >= decks.Count ||
            decidedEnemyDeckId < 0 || decidedEnemyDeckId >= decks.Count)
        {
            Debug.LogWarning("存在しないデッキ枠が選択されているため対戦を開始できません");
            return;
        }

        GameSession.SelectedDeck = new List<int>(decks[decidedPlayerDeckId].cardIds);
        GameSession.EnemyDeck = new List<int>(decks[decidedEnemyDeckId].cardIds);

        if (ModeConfigManager.Instance != null)
            ModeConfigManager.Instance.ChangeMode(GameMode.CPU_BATTLE);

        SceneManager.LoadScene(battleScene);
    }
}
