using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject DeckListPanel;
    [SerializeField] private List<Button> DeckButtons;

    public System.Action<int> OnDeckSelected;

    private void OnEnable()
    {
        for (int i = 0; i < DeckButtons.Count; i++)
        {
            int index = i;
            DeckButtons[i].onClick.AddListener(() => OnSelected(index));
        }
    }

    private void OnSelected(int index)
    {

        OnDeckSelected?.Invoke(index); // DeckAndStageSelectUIに通知するだけ
    }

    //ステージも上記と同じように作成する

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OpenDeckListPanel()
    {
        DeckListPanel.SetActive(true);
    }

    public void CloseDeckListPanel()
    {
        DeckListPanel.SetActive(false);
    }



}
