using UnityEngine;

public class TwoPickSelectionPanel : MonoBehaviour
{
    [SerializeField] GameObject ConfirmPanel;
    [SerializeField] private GameObject modeSelectPanel;
    [SerializeField] private GameObject twoPickPanel;
    [SerializeField] private GameObject cardInfoPannel;
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
        //シーン変更
        //UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
    }
}
/*画面の操作とシーン遷移
 * 遷移時に渡されるデータと受け取る機能
 * 
 * 
*/