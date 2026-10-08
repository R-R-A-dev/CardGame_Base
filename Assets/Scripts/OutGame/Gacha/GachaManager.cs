using System.Collections.Generic;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    [SerializeField] private List<PackData> allPacks;
    [SerializeField] private GachaUI gachaUI;
    [SerializeField] private GachaOpenUI gachaOpenUI;
    [SerializeField] private GachaResultUI gachaResultUI;

    
    [SerializeField] private GameObject bg;
    [SerializeField] private GameObject header;
    [SerializeField] private GameObject gachaUIObj;
    [SerializeField] private GameObject gachaOpenUIObj;
    [SerializeField] private GameObject gachaResultUIObj;

    // ガチャボタン（モード選択画面側）の押下演出。未設定の場合は演出を挟まず即座に開く
    [SerializeField] private ModeButtonPressEffect gachaButtonEffect;
    // ガチャ画面の戻るボタンの押下演出。GachaPanelのフェードアウトを担当する
    [SerializeField] private ModeButtonPressEffect returnButtonEffect;
    // モード選択パネルの登場演出。ガチャを閉じた時に明示的に再生する
    [SerializeField] private PanelEntranceAnimator modeSelectionEntrance;
    // ガチャ画面の登場演出。GachaPanelは常時アクティブでOnEnableが走らないため明示的に再生する
    [SerializeField] private PanelEntranceAnimator gachaEntrance;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenGacha()
    {
        if (gachaButtonEffect == null)
        {
            ShowGachaUI();
            return;
        }

        gachaButtonEffect.Play(ShowGachaUI);
    }

    /// <summary>
    /// ガチャを閉じてモード選択画面へ戻る。
    /// 戻るボタンの押下演出（GachaPanelのフェードアウト）が完了した後に呼ばれる。
    /// </summary>
    public void CloseGacha()
    {
        gachaUI.Hide();
        bg.SetActive(false);
        header.SetActive(false);

        // GachaPanelは常時アクティブなため、フェードアウトさせたalphaを明示的に戻す
        if (returnButtonEffect != null)
            returnButtonEffect.ResetState();

        // モード選択パネルも非アクティブにならないため、ガチャボタンの拡大とフェードを元へ戻す
        if (gachaButtonEffect != null)
            gachaButtonEffect.ResetState();

        // SetActiveを経由せず戻るので、登場演出はここから直接再生する
        if (modeSelectionEntrance != null)
            modeSelectionEntrance.Play();
    }

    private void ShowGachaUI()
    {
        gachaUI.Open(GetUnlockedPacks());
        bg.SetActive(true);
        header.SetActive(true);

        // 中身を表示し終えてから、パネル全体の登場演出を再生する
        if (gachaEntrance != null)
            gachaEntrance.Play();
    }

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

    // quantity個のパックをまとめて購入
    public void PurchasePack(PackData pack, int quantity)
    {
        if (quantity <= 0) return;

        const int MAX_PURCHASE_QUANTITY = 10;
        if (quantity > MAX_PURCHASE_QUANTITY)
            quantity = MAX_PURCHASE_QUANTITY;

        int totalPrice = pack.price * quantity;

        // メモリ上のデータを直接参照
        if (!GameDataHolder.Instance.SpendGold(totalPrice))
        {
            Debug.Log("所持金が足りません");
            return;
        }

        List<List<GachaCardEntry>> packResults = new List<List<GachaCardEntry>>();
        for (int i = 0; i < quantity; i++)
        {
            List<GachaCardEntry> drawnCards = DrawCards(pack);
            packResults.Add(drawnCards);

            foreach (GachaCardEntry entry in drawnCards)
                GameDataHolder.Instance.AddCard(entry.cardId);
        }

        // ここでまとめてファイルに保存
        GameDataHolder.Instance.SaveToFile();

        gachaUI.Hide();
        gachaOpenUI.Open(pack, packResults);
    }

    public void OnAllPacksOpened(List<GachaCardEntry> allDrawnCards)
    {
        gachaOpenUI.Hide();

        List<int> cardIds = new List<int>();
        foreach (GachaCardEntry entry in allDrawnCards)
            cardIds.Add(entry.cardId);

        gachaResultUI.Open(cardIds);
    }

    public void OnResultClose()
    {
        gachaResultUI.Hide();
        gachaUI.Open(GetUnlockedPacks());
    }

    public void UnlockPack(int packIndex)
    {
        SaveData saveData = SaveManager.Load();
        if (!saveData.unlockedPackIds.Contains(packIndex))
        {
            saveData.unlockedPackIds.Add(packIndex);
            SaveManager.Save(saveData);
        }
    }

    // カード抽選（レアリティ決定 → 該当レアリティ内から均等抽選）
    // 戻り値をList<GachaCardEntry>に変更（cardId・rarityのペア）
    private List<GachaCardEntry> DrawCards(PackData pack)
    {
        List<GachaCardEntry> result = new List<GachaCardEntry>();

        if (pack.cardPool == null || pack.cardPool.Count == 0)
        {
            Debug.LogWarning($"{pack.packName}のcardPoolが空です");
            return result;
        }

        if (pack.rarityDropRates == null || pack.rarityDropRates.Count == 0)
        {
            Debug.LogWarning($"{pack.packName}のrarityDropRatesが空です");
            return result;
        }

        for (int i = 0; i < pack.drawCount; i++)
        {
            CardRarity rarity = DrawRarity(pack.rarityDropRates);
            int cardId = DrawCardFromRarity(pack.cardPool, rarity);
            result.Add(new GachaCardEntry { cardId = cardId, rarity = rarity });
        }
        return result;
    }

    private CardRarity DrawRarity(List<RarityDropRate> rarityDropRates)
    {
        float totalRate = 0f;
        foreach (RarityDropRate rate in rarityDropRates)
            totalRate += rate.dropRatePercent;

        float randomValue = Random.Range(0f, totalRate);
        float cumulative = 0f;

        foreach (RarityDropRate rate in rarityDropRates)
        {
            cumulative += rate.dropRatePercent;
            if (randomValue <= cumulative)
                return rate.rarity;
        }

        return rarityDropRates[rarityDropRates.Count - 1].rarity;
    }

    private int DrawCardFromRarity(List<GachaCardEntry> cardPool, CardRarity rarity)
    {
        List<GachaCardEntry> sameRarityCards = new List<GachaCardEntry>();
        foreach (GachaCardEntry entry in cardPool)
        {
            if (entry.rarity == rarity)
                sameRarityCards.Add(entry);
        }

        if (sameRarityCards.Count == 0)
        {
            Debug.LogWarning($"レアリティ{rarity}のカードがcardPoolに存在しません");
            return cardPool[Random.Range(0, cardPool.Count)].cardId;
        }

        int index = Random.Range(0, sameRarityCards.Count);
        return sameRarityCards[index].cardId;
    }
}