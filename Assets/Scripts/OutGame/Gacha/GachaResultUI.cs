using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultUI : MonoBehaviour
{
    [SerializeField] private List<CardController> cardList;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);
        gameObject.SetActive(false);
    }

    public void Open(List<int> drawnCards)
    {
        gameObject.SetActive(true);

        foreach (var card in cardList)
            card.gameObject.SetActive(false);

        for (int i = 0; i < drawnCards.Count; i++)
        {
            if (i >= cardList.Count) break;
            cardList[i].gameObject.SetActive(true);
            cardList[i].Init(drawnCards[i], false);
        }
    }

    private void OnCloseButtonClick()
    {
        GachaManager.Instance.OnResultClose();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}