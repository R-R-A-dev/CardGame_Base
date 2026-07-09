using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaPackItem : MonoBehaviour
{
    [SerializeField] private Image packImage;
    [SerializeField] private TextMeshProUGUI packNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button selectButton;

    private PackData packData;
    private System.Action<PackData> onSelected;

    public void Setup(PackData data, System.Action<PackData> onSelectedCallback)
    {
        packData = data;
        onSelected = onSelectedCallback;

        packImage.sprite = data.packImage;
        packNameText.text = data.packName;
        priceText.text = $"G {data.price}";
        descriptionText.text = data.description;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onSelected?.Invoke(packData));
    }
}