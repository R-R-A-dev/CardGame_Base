using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaResultUI : MonoBehaviour
{
    [SerializeField] private Transform cardListParent;
    [SerializeField] private GachaResultCardItem cardItemPrefab;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);
        gameObject.SetActive(false);
    }

    // 複数パック分の合算カードを受け取り、重複をまとめて表示
    public void Open(List<int> allDrawnCards)
    {
        gameObject.SetActive(true);

        foreach (Transform child in cardListParent)
            Destroy(child.gameObject);

        // 同じカードIDが何枚あるか集計
        Dictionary<int, int> countMap = new Dictionary<int, int>();
        foreach (int cardId in allDrawnCards)
        {
            if (countMap.ContainsKey(cardId))
                countMap[cardId]++;
            else
                countMap[cardId] = 1;
        }

        // 集計結果をもとに1種類につき1つ生成
        foreach (var kvp in countMap)
        {
            GachaResultCardItem item = Instantiate(cardItemPrefab, cardListParent);
            item.Setup(kvp.Key, kvp.Value); // カードID・枚数
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