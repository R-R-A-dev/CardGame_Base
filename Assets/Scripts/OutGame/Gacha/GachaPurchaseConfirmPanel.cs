using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaPurchaseConfirmPanel : MonoBehaviour
{
    [Header("パック情報")]
    [SerializeField] private TextMeshProUGUI packNameText;
    [SerializeField] private TextMeshProUGUI totalPriceText;

    [Header("数量選択")]
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;

    [Header("確定ボタン")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("設定")]
    [SerializeField] private int minQuantity = 1;
    [SerializeField] private int maxQuantity = 10;

    private PackData pendingPack;
    private int quantity = 1;
    private int currentGold = 0; // 所持金を保持
    private System.Action<PackData, int> onConfirmed;

    // シーン上で非アクティブ配置のため、初回Open()のSetActive(true)で初めて初期化が走る。
    // Startで非表示にすると初回Open直後に閉じてしまうので、Awakeでリスナー登録のみ行う。
    private void Awake()
    {
        minusButton.onClick.RemoveAllListeners();
        plusButton.onClick.RemoveAllListeners();
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        minusButton.onClick.AddListener(OnMinusClicked);
        plusButton.onClick.AddListener(OnPlusClicked);
        yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(OnNoClicked);
    }

    public void Open(PackData pack, System.Action<PackData, int> onConfirmedCallback)
    {
        pendingPack = pack;
        onConfirmed = onConfirmedCallback;
        quantity = minQuantity;

        // 所持金を取得
        SaveData saveData = SaveManager.Load();
        currentGold = saveData.gold;

        packNameText.text = pack.packName;

        UpdateDisplay();
        gameObject.SetActive(true);
    }

    private void OnMinusClicked()
    {
        quantity = Mathf.Max(minQuantity, quantity - 1);
        UpdateDisplay();
    }

    private void OnPlusClicked()
    {
        quantity = Mathf.Min(maxQuantity, quantity + 1);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        int totalPrice = pendingPack.price * quantity;

        quantityText.text = quantity.ToString();
        totalPriceText.text = $"合計: G {totalPrice}";

        // 所持金を超えていたらYesボタンを非活性
        yesButton.interactable = totalPrice <= currentGold;
    }

    private void OnYesClicked()
    {
        gameObject.SetActive(false);
        onConfirmed?.Invoke(pendingPack, quantity);
    }

    private void OnNoClicked()
    {
        gameObject.SetActive(false);
    }
}