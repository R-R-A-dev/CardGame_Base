using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultUI : MonoBehaviour
{
    [SerializeField] private List<GachaResultCardItem> cardItems;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);
        gameObject.SetActive(false);
    }

    public void Open(List<int> allDrawnCards)
    {
        gameObject.SetActive(true);

        Dictionary<int, int> countMap = new Dictionary<int, int>();
        foreach (int cardId in allDrawnCards)
        {
            if (countMap.ContainsKey(cardId))
                countMap[cardId]++;
            else
                countMap[cardId] = 1;
        }

        List<int> uniqueIds = new List<int>(countMap.Keys);

        for (int i = 0; i < cardItems.Count; i++)
        {
            if (i < uniqueIds.Count)
            {
                int cardId = uniqueIds[i];
                cardItems[i].gameObject.SetActive(true);
                cardItems[i].Setup(cardId, countMap[cardId]);
            }
            else
            {
                cardItems[i].gameObject.SetActive(false);
            }
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