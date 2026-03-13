using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LethalPuzzleSelectionPanel : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform parent;

    [SerializeField] GameObject ConfirmPanel;

    int selectNum = 0;
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
        //ÉVÅ[ÉìïœçX
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
