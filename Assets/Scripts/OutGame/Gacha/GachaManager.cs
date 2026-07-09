using System.Collections.Generic;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    [SerializeField] private List<PackData> allPacks; // 全パックのSO
    [SerializeField] private GachaUI gachaUI;
    [SerializeField] private GachaOpenUI gachaOpenUI;
    [SerializeField] private GachaResultUI gachaResultUI;

    private void Awake()
    {
        Instance = this;
    }

    // メニューからガチャ画面を開く
    public void OpenGacha()
    {
        gachaUI.Open(GetUnlockedPacks());
    }

    // アンロック済みパックを取得
    public List<PackData> GetUnlockedPacks()
    {
        SaveData saveData = SaveManager.Load();
        List<PackData> unlocked = new List<PackData>();

        foreach (PackData pack in allPacks)
        {
            if (pack.isUnlockedByDefault ||
                saveData.unlockedPackIds.Contains(allPacks.IndexOf(pack)))
                unlocked.Add(pack);
        }
        return unlocked;
    }

    // パックを購入して開封
    public void PurchasePack(PackData pack)
    {
        SaveData saveData = SaveManager.Load();

        // 所持金チェック
        if (saveData.gold < pack.price)
        {
            Debug.Log("所持金が足りません");
            return;
        }

        // 所持金を消費
        saveData.gold -= pack.price;

        // カードを抽選
        List<int> drawnCards = DrawCards(pack);

        // 所持カードに追加
        saveData.ownedCardIds.AddRange(drawnCards);
        SaveManager.Save(saveData);

        // 開封演出画面へ
        gachaUI.Hide();
        gachaOpenUI.Open(drawnCards);
    }

    // カードをランダム抽選
    private List<int> DrawCards(PackData pack)
    {
        List<int> pool = new List<int>(pack.cardPool);
        List<int> result = new List<int>();

        for (int i = 0; i < pack.drawCount; i++)
        {
            if (pool.Count == 0) break;
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return result;
    }

    // 開封演出終了後に一覧へ
    public void OnOpenAnimationComplete(List<int> drawnCards)
    {
        gachaOpenUI.Hide();
        gachaResultUI.Open(drawnCards);
    }

    // 一覧画面を閉じてガチャ画面に戻る
    public void OnResultClose()
    {
        gachaResultUI.Hide();
        gachaUI.Open(GetUnlockedPacks());
    }
}