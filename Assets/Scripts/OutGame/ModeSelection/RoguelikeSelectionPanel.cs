using UnityEngine;

public class RoguelikeSelectionPanel : MonoBehaviour
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

    public void StartRoguelike()
    {
        ModeConfigManager.Instance.currentGameMode = GameMode.ROGUELIKE;
        //ÉVÅ[ÉìïœçX
        //UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");

    }
}
