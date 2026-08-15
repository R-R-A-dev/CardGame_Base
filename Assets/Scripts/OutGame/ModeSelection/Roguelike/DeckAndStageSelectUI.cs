using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckAndStageSelectUI : MonoBehaviour
{
    [SerializeField] private StageSelectUI stageSelectUI;
    [SerializeField] private DeckSelectUI deckSelectUI;
    [SerializeField] private Button startButton;
    [SerializeField] private Button decideDeckButton; // deckConfirmPanle内の「決定」ボタン（DecideDeckがアタッチされたもの）
    [SerializeField] private GameObject stageConfirmPanle;
    [SerializeField] private GameObject deckConfirmPanle;
    [SerializeField] private GameObject deckEdit;
    [SerializeField] private GameObject ModeSelectionPanel;
    [SerializeField] private TextMeshProUGUI selectedDeck;

    [SerializeField] private DeckBuilderUI deckBuilderUI; // 追加：DeckBuilderUIへの参照


    private const int MIN_DECK_SIZE = 40;

    private int selectedStageIndex = -1;
    public int selectedDeckId = -1;
    private int decidedDeckId = -1; // DecideDeckで決定されたデッキIDのみを保持

    private void Start()
    {
        // 通知を受け取るだけ
        stageSelectUI.OnStageSelected += OnStageSelected;
        deckSelectUI.OnDeckSelected += OnDeckSelectedEdit;

        // デッキが決定されるまでステージ選択ボタンは非活性にしておく
        stageSelectUI.SetSelectButtonsInteractable(false);

        // 注意: DeckEditパネルはシーン開始時点で非アクティブなため、
        // ここではDeckBuilderManager.Instanceがまだnull（Awakeが未実行）。
        // OnDeckEditClosedの購読はOpenDeckEdit()側（パネルを有効化した直後）で行う。
    }

    // デッキ編集画面を開く際に非表示にしたModeSelectionPanelを、完了時に再表示する
    private void ShowModeSelectionPanel()
    {
        ModeSelectionPanel.SetActive(true);
    }

    private void OnStageSelected(int index)
    {
        selectedStageIndex = index;
        stageConfirmPanle.SetActive(true);
    }

    private void OnDeckSelectedEdit(int deckId)
    {
        // DeckButtonsでデッキが選択された時点で、規定枚数未満なら決定できないようにする
        selectedDeckId = deckId;
        deckConfirmPanle.SetActive(true);
        UpdateDecideDeckButton();
    }

    private int GetSelectedDeckSize()
    {
        var decks = GameDataHolder.Instance.Data.roguelikeDecks;
        if (selectedDeckId < 0 || selectedDeckId >= decks.Count) return 0;
        return decks[selectedDeckId].cardIds.Count;
    }

    //選択中デッキが規定枚数未満なら決定ボタンを非活性にする
    private void UpdateDecideDeckButton()
    {
        decideDeckButton.interactable = GetSelectedDeckSize() >= MIN_DECK_SIZE;
    }

    public void OnStartButtonClick()
    {
        // ステージ情報はStageSelectUIから取得
        //StageSelectItem selectedStage = stageSelectUI.GetSelectedItem(selectedStageIndex);

        //var config = new RoguelikeStartConfig
        //{
        //    DeckId = selectedDeckId,
        //    StageConfig = new StageConfig { StageId = selectedStageIndex }
        //};
        //RoguelikeManager.Instance.StartRoguelike(config);
    }


    public void StartGame()
    {
        //ローグライク
        RoguelikeStartConfig config = new RoguelikeStartConfig
        {
            DeckId = decidedDeckId,
            StageConfig = new StageConfig { StageId = selectedStageIndex }
        };
        // Managerに渡し、生成されたゲーム状態を受け取る
        RoguelikeManager.Instance.StartRoguelike(config);
    }

    //デッキ編成
    public void OpenDeckEdit()
    {
        // このクラスはローグライク専用のデッキ・ステージ選択画面のため、
        // ここで確実にROGUELIKEにしておく。
        // （currentGameModeが実際にROGUELIKEになるのはRoguelikeSelectionPanel.StartRoguelike()等
        //   もっと後のタイミングのため、ここでセットしないとStartDeckEdit側のisRoguelike判定が
        //   Falseになり、cpuBattleDecks側を誤って読み込んでしまう）
        if (ModeConfigManager.Instance != null)
            ModeConfigManager.Instance.currentGameMode = GameMode.ROGUELIKE;

        // SetActive(true)でDeckBuilderManager.OnEnableが走り、その時点のdeckNumで
        // StartDeckEditが自動的に1回実行される（Instanceもここで確定する）
        deckEdit.SetActive(true);
        ModeSelectionPanel.SetActive(false);

        // 多重購読を避けるため、一度解除してから購読し直す
        DeckBuilderManager.Instance.OnDeckEditClosed -= UpdateDecideDeckButton;
        DeckBuilderManager.Instance.OnDeckEditClosed -= ShowModeSelectionPanel;
        DeckBuilderManager.Instance.OnDeckEditClosed += UpdateDecideDeckButton;
        DeckBuilderManager.Instance.OnDeckEditClosed += ShowModeSelectionPanel;

        // OnEnable側が使ったdeckNumが選択中のデッキと異なる場合だけ、正しいdeckNumで開始し直す。
        // 一致している場合に再度呼ぶと、同一フレーム内でStartDeckEditが2回走り、
        // Destroy()の反映が1フレーム遅延する影響でカード表示が崩れる（意図せず全て消えるなど）ため。
        Debug.Log($"Opening Deck Edit for Deck ID: {selectedDeckId}");
        if (DeckBuilderManager.Instance.deckNum != selectedDeckId)
            DeckBuilderManager.Instance.OpenDeckEditForDeck(selectedDeckId);
    }

    public void CloseStagePannle()
    {
        stageConfirmPanle.SetActive(false);
    }

    public void CloseDeckPannle()
    {
        deckConfirmPanle.SetActive(false);
    }

    public void DecideDeck()
    {
        deckConfirmPanle.SetActive(false);
        selectedDeck.text = selectedDeckId.ToString();

        decidedDeckId = selectedDeckId;
        stageSelectUI.SetSelectButtonsInteractable(true);
    }
}
/*ゲームスタート後にitemからstageのデータを取得
 * itemで反映など
 * 実際のゲーム画面の構成を考え直す
 * 
 * デッキ選択UIから選択できるようにする
 * デッキ参照
 * 
 * 選択していることがわかるようにする
 * デッキを設定していないと選択できない
 * 確認ウィンドウ
 * 
 * デッキ情報の選択、渡し方、
 * 
*/