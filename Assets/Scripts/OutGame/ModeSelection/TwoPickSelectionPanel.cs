using UnityEngine;
using UnityEngine.UI;

public class TwoPickSelectionPanel : MonoBehaviour
{
    [SerializeField] GameObject ConfirmPanel;
    [SerializeField] private GameObject modeSelectPanel;
    [SerializeField] private GameObject twoPickPanel;
    [SerializeField] private GameObject cardInfoPannel;

    [Header("モード選択に戻るボタン")]
    // TwoPickSelectPanelの背景はレイキャストを通すため、ピック中は明示的に押せなくする
    [SerializeField] private Button returnButton;

    [Header("やめた時の報酬パネル")]
    [SerializeField] private TwoPickRewardUI twoPickRewardUI;

    private void Start()
    {
        // ピック画面は閉じた状態で始まるため、戻るボタンの状態を実際のアクティブ状態に合わせる
        SetReturnButtonInteractable(!twoPickPanel.activeSelf);

        // 復帰対応：2Pickの戦闘から戻ってきた場合、フラグを消費する
        if (GameSession.TwoPickFinished)
        {
            GameSession.TwoPickFinished = false;

            if (GameSession.TwoPickReward > 0)
            {
                twoPickRewardUI.SetRewardGold(GameSession.TwoPickReward);
                twoPickRewardUI.Open();
                GameSession.TwoPickReward = 0;
            }
        }
    }

    public void StartPanelOpen()
    {
        cardInfoPannel.SetActive(false);
        ConfirmPanel.SetActive(true);
    }

    public void StartPanelClose()
    {
        ConfirmPanel.SetActive(false);
    }

    public void StartTwoPick()
    {
        ModeConfigManager.Instance.currentGameMode = GameMode.TWO_PICK;
        ConfirmPanel.SetActive(false);

        modeSelectPanel.SetActive(false);
        twoPickPanel.SetActive(true);
        // ピック開始後はモード選択へ戻れないようにする
        SetReturnButtonInteractable(false);
        TwoPickModeManager.Instance?.StartPick();
        //シーン遷移
        //UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
    }

    private void SetReturnButtonInteractable(bool interactable)
    {
        if (returnButton != null)
            returnButton.interactable = interactable;
    }
}
/*画面の操作とシーン遷移
 * 遷移時に渡すデータと受け取る機能
 *
 *
*/