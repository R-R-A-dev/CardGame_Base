using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaResultCardItem : MonoBehaviour
{
    [SerializeField] private CardController cardController;
    [SerializeField] private TextMeshProUGUI countText; // "×2"等
    [SerializeField] private GameObject countBadge;      // 1枚の場合は非表示にする

    public void Setup(int cardId, int count)
    {
        cardController.Init(cardId, false);

        if (count > 1)
        {
            countBadge.SetActive(true);
            countText.text = $"×{count}";
        }
        else
        {
            countBadge.SetActive(false);
        }
    }
}