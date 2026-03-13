using UnityEngine;
using UnityEngine.UI;

public class DeckAndStageSelectUI : MonoBehaviour
{
    [SerializeField] private StageSelectUI stageSelectUI;
    [SerializeField] private DeckSelectUI deckSelectUI;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject confirmPanle;

    private int selectedStageIndex = -1;
    private int selectedDeckId = -1;

    private void Start()
    {
        // 通知を受け取るだけ
        stageSelectUI.OnStageSelected += OnStageSelected;
        //deckSelectUI.OnDeckSelected += OnDeckSelected;
    }

    private void OnStageSelected(int index)
    {
        selectedStageIndex = index;
        confirmPanle.SetActive(true);
        
    }

    private void OnDeckSelected(int deckId)
    {
        selectedDeckId = deckId;
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
        UpdateStartButton();
    }

    public void ClosePannle()
    {
        confirmPanle.SetActive(false);
    }
}
/*ゲームスタート後にitemからstageのデータを取得
 * itemで反映など
 * 実際のゲーム画面の構成を考え直す
 * 
 * 
 * デッキを設定していないと選択できない
 * 確認ウィンドウ
 * 
 * デッキ情報の選択、渡し方、
 * 
*/