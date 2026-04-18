using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckAndStageSelectUI : MonoBehaviour
{
    [SerializeField] private StageSelectUI stageSelectUI;
    [SerializeField] private DeckSelectUI deckSelectUI;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject stageConfirmPanle;
    [SerializeField] private GameObject deckConfirmPanle;
    [SerializeField] private GameObject deckEdit;
    [SerializeField] private GameObject ModeSelectionPanel;
    [SerializeField] private TextMeshProUGUI selectedDeck;

    private int selectedStageIndex = -1;
    public int selectedDeckId = 0;

    private void Start()
    {
        // 通知を受け取るだけ
        stageSelectUI.OnStageSelected += OnStageSelected;
        deckSelectUI.OnDeckSelected += OnDeckSelectedEdit;
    }

    private void OnStageSelected(int index)
    {
        selectedStageIndex = index;
        stageConfirmPanle.SetActive(true);
    }

    private void OnDeckSelectedEdit(int deckId)
    {
        selectedDeckId = deckId;
        deckConfirmPanle.SetActive(true);
        UpdateStartButton();
    }

    //値を受け取る
    private void UpdateStartButton()
    {

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
            DeckId = selectedDeckId,
            StageConfig = new StageConfig { StageId = selectedStageIndex }
        };
        // Managerに渡す
        RoguelikeManager.Instance.StartRoguelike(config);
        UpdateStartButton();
    }

    //デッキ編成
    public void OpenDeckEdit()
    {
        // デッキ編成UIを開く処理
        deckEdit.SetActive(true);
        ModeSelectionPanel.SetActive(false);
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