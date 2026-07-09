using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaUI : MonoBehaviour
{
    [SerializeField] private Transform packListParent;
    [SerializeField] private GachaPackItem packItemPrefab;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button closeButton;

    private List<GachaPackItem> spawnedItems = new List<GachaPackItem>();

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);
        gameObject.SetActive(false);
    }

    public void Open(List<PackData> unlockedPacks)
    {
        gameObject.SetActive(true);

        // 所持金表示
        SaveData saveData = SaveManager.Load();
        goldText.text = $"G: {saveData.gold}";

        // パック一覧を生成
        foreach (Transform child in packListParent)
            Destroy(child.gameObject);
        spawnedItems.Clear();

        foreach (PackData pack in unlockedPacks)
        {
            GachaPackItem item = Instantiate(packItemPrefab, packListParent);
            item.Setup(pack, OnPackSelected);
            spawnedItems.Add(item);
        }
    }

    private void OnPackSelected(PackData pack)
    {
        GachaManager.Instance.PurchasePack(pack);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Close()
    {
        Hide();
        // メニュー画面に戻る処理
    }
}