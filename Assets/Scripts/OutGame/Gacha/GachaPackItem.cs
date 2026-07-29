using UnityEngine;
using UnityEngine.UI;

public class GachaPackItem : MonoBehaviour
{
    [SerializeField] private Image packImage;
    [SerializeField] private Button packButton; // パック全体がボタン

    private PackData packData;
    private System.Action<PackData> onSelected;

    public void Setup(PackData data, System.Action<PackData> onSelectedCallback)
    {
        packData = data;
        onSelected = onSelectedCallback;

        packImage.sprite = data.packImage;

        packButton.onClick.RemoveAllListeners();
        packButton.onClick.AddListener(OnPackClicked);
    }

    private void OnPackClicked()
    {
        onSelected?.Invoke(packData);
    }
}