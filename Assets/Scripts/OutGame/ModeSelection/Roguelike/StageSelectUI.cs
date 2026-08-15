using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectUI : MonoBehaviour
{
    [SerializeField] private List<Button> selectButtons;
    [SerializeField] private GameObject confirmPanel;

    [SerializeField] private List<StageSelectItem> stageItems;

    public System.Action<int> OnStageSelected; // 外部への通知

    private void OnEnable()
    {
        for (int i = 0; i < selectButtons.Count; i++)
        {
            int index = i;
            selectButtons[i].onClick.AddListener(() => OnSelected(index));
        }
    }

    private void OnSelected(int index)
    {

        OnStageSelected?.Invoke(index); // DeckAndStageSelectUIに通知するだけ
    }

    public void SetSelectButtonsInteractable(bool interactable)
    {
        foreach (var button in selectButtons)
        {
            button.interactable = interactable;
        }
    }

    public void OpenConfirmPannel()
    {

    }

    public void CloseConfirmPannel()
    {

    }

    public void StartGame()
    {

    }

    void Start()
    {

    }

    void Update()
    {

    }
}
