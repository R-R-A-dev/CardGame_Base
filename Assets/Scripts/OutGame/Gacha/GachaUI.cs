using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaUI : MonoBehaviour
{
    [SerializeField] private List<GachaPackItem> packItems; // Hierarchyで並べた3つを登録
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GachaPurchaseConfirmPanel confirmPanel;

    private void Start()
    {
        // closeButton.onClick.RemoveAllListeners();
        // closeButton.onClick.AddListener(Close);
        gameObject.SetActive(false);
    }

    public void Open(List<PackData> unlockedPacks)
    {
        gameObject.SetActive(true);

        SaveData saveData = SaveManager.Load();
        goldText.text = $"G: {saveData.gold}";

        // 既存のpackItemsにデータを割り当てる
        for (int i = 0; i < packItems.Count; i++)
        {
            if (i < unlockedPacks.Count)
            {
                packItems[i].gameObject.SetActive(true);
                packItems[i].Setup(unlockedPacks[i], OnPackSelected);
            }
            else
            {
                // アンロックされていないパック枠は非表示
                packItems[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnPackSelected(PackData pack)
    {
        confirmPanel.Open(pack, OnPurchaseConfirmed);
    }

    private void OnPurchaseConfirmed(PackData pack, int quantity)
    {
        GachaManager.Instance.PurchasePack(pack, quantity);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Close()
    {
        Hide();
    }
}