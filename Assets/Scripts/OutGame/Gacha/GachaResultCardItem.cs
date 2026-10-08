using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaResultCardItem : MonoBehaviour
{
    [SerializeField] private CardController cardController;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button cardButton;
    [SerializeField] private GachaCardDetailPanel detailPanel;
    // detailPanel引数を削除
    public void Setup(int id, int count)
    {
        cardController.Init(id, false);

        countText.text = $"×{count}";

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => detailPanel.Open(cardController)); // cardControllerを渡す

    }
}