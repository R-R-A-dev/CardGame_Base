using UnityEngine;

public class TwoPickSelectionPanel : MonoBehaviour
{
    [SerializeField] GameObject ConfirmPanel;
    [SerializeField] private GameObject modeSelectPanel;
    [SerializeField] private GameObject twoPickPanel;
    [SerializeField] private GameObject cardInfoPannel;

    [Header("やめた時の報酬パネル")]
    [SerializeField] private TwoPickRewardUI twoPickRewardUI;

    private void Start()
    {
        // 復帰対応：2Pickの戦闘から戻ってきた場合、フラグを消費する
        if (GameSession.TwoPickFinished)
        {
            GameSession.TwoPickFinished = false;

            if (GameSession.TwoPickReward > 0)
            {
                twoPickRewardUI.Open(GameSession.TwoPickReward);
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
        TwoPickModeManager.Instance?.StartPick();
        //シーン遷移
        //UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
    }
}
/*画面の操作とシーン遷移
 * 遷移時に渡すデータと受け取る機能
 *
 *
*/