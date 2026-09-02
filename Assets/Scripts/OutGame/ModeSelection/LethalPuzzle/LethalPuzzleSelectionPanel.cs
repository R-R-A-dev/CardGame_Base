using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LethalPuzzleSelectionPanel : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform parent;

    [SerializeField] GameObject ConfirmPanel;

    [Header("クリア報酬パネル")]
    [SerializeField] private LethalPuzzleRewardUI lethalPuzzleRewardUI;

    int selectNum = 0;

    private void Start()
    {
        // 戦闘から戻ってきた場合、クリアしていれば報酬パネルを表示する
        if (GameSession.LethalPuzzleFinished)
        {
            bool won = GameSession.LethalPuzzleWon;
            GameSession.LethalPuzzleFinished = false;

            if (won)
            {
                int puzzleId = ModeConfigManager.Instance.lethalPuzzleList[ModeConfigManager.Instance.LethalPuzzleIndex].puzzleId;
                lethalPuzzleRewardUI.Open(puzzleId);
            }
        }
    }

    public void StartPanelOpen()
    {
        ConfirmPanel.SetActive(true);
    }

    public void StartPanelClose()
    {
        ConfirmPanel.SetActive(false);
    }

    void GeneratePuzzleButtons()
    {
        for (int i = 0; i < ModeConfigManager.Instance.lethalPuzzleList.Count; i++)
        {
            int buttonNumber = i;
            GameObject btn = Instantiate(buttonPrefab, parent);

            btn.GetComponent<Button>().onClick.AddListener(() => OnClickButton(buttonNumber));
        }
    }

    private void OnEnable()
    {
        GeneratePuzzleButtons();
    }

    void OnClickButton(int number)
    {
        //シーン遷移
        ConfirmPanel.SetActive(true);
        selectNum = number;
        ModeConfigManager.Instance.LethalPuzzleIndex = selectNum;
    }

    public void StartLethalPuzzle()
    {

        ModeConfigManager.Instance.ChangeMode(GameMode.LETHAL_PUZZLE);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }
}
