using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaPackItem : MonoBehaviour
{
    [SerializeField] private GameObject packVisual; // パックの見た目（子オブジェクトの画像群）
    [SerializeField] private TextMeshProUGUI packNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button packButton; // パック全体がボタン

    private PackData packData;
    private System.Action<PackData> onSelected;

    public void Setup(PackData data, System.Action<PackData> onSelectedCallback)
    {
        packData = data;
        onSelected = onSelectedCallback;

        if (packVisual != null)
            packVisual.SetActive(true);

        packNameText.text = data.packName;
        priceText.text = $"G {data.price}";

        packButton.onClick.RemoveAllListeners();
        packButton.onClick.AddListener(OnPackClicked);
    }

    private void OnPackClicked()
    {
        onSelected?.Invoke(packData);
    }
}
