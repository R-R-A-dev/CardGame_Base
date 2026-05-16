using UnityEngine;

public class RoguelikeSceneController : MonoBehaviour
{
    [SerializeField] private GameObject roguelikePanel;

    private void Awake()
    {
        // 戦闘シーンから戻ってきた場合はRoguelikePanelをアクティブに
        if (RoguelikeSession.GameState != null)
        {
            roguelikePanel.SetActive(true);
        }
    }
}