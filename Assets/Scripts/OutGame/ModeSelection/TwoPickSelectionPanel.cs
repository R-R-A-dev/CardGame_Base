using UnityEngine;

public class TwoPickSelectionPanel : MonoBehaviour
{
    [SerializeField] GameObject ConfirmPanel;
    public void StartPanelOpen()
    {
        ConfirmPanel.SetActive(true);
    }

    public void StartPanelClose()
    {
        ConfirmPanel.SetActive(false);
    }

    public void StartTwoPick()
    {
        ModeConfigManager.Instance.currentGameMode = GameMode.TWO_PICK;
        //シーン変更
        //UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
    }
}
/*画面の操作とシーン遷移
 * 遷移時に渡されるデータと受け取る機能
 * 
 * 
*/