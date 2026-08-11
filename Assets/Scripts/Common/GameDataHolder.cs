using System.Collections.Generic;
using UnityEngine;

public class GameDataHolder : MonoBehaviour
{
    public static GameDataHolder Instance { get; private set; }

    private SaveData _data;

    // 呼ばれた時点でnullなら自動で読み込む（Lazy初期化）
    public SaveData Data
    {
        get
        {
            if (_data == null)
                LoadIntoMemory();
            return _data;
        }
        private set => _data = value;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadIntoMemory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadIntoMemory()
    {
        if (_data != null) return; // 二重読み込み防止
        _data = SaveManager.LoadOrInitialize();
        Debug.Log("セーブデータをメモリに読み込みました");
    }

    // 明示的に呼んだ時だけファイルへ書き込む
    public void SaveToFile()
    {
        SaveManager.Save(Data);
        Debug.Log("セーブデータをファイルに保存しました");
    }

    // ========================================
    // デッキ編集ブリッジ（添字方式のワーキングデータ ⇄ IDそのまま方式のセーブデータ）
    // ========================================

    // デッキ編集用ワーキングデータ（添字方式）。CardListData.Decksの代替
    public List<int> EditingDeckCounts { get; private set; }

    // 所持カードの表示用コピー（添字方式）。CardListData.PossessionCardの代替
    public List<int> DisplayPossessionCard { get; private set; }

    public void BeginDeckEdit(int deckNum, bool isRoguelike)
    {
        int cardTypeCount = CardDatabase.LoadAllCards().Length;

        List<DeckSaveData> decks = isRoguelike ? Data.roguelikeDecks : Data.cpuBattleDecks;
        while (decks.Count <= deckNum)
            decks.Add(new DeckSaveData());

        EditingDeckCounts = ConvertCardIdsToCountList(decks[deckNum].cardIds, cardTypeCount);
        DisplayPossessionCard = ConvertCardIdsToCountList(
            CardOwnershipConverter.ToCardIdList(Data.ownedCardCounts), cardTypeCount);

        Debug.Log($"[BeginDeckEdit] deckNum={deckNum} isRoguelike={isRoguelike} " +
            $"savedCardIds.Count={decks[deckNum].cardIds.Count} EditingDeckCounts合計={SumOf(EditingDeckCounts)}");
    }

    public void CommitDeckEdit(int deckNum, bool isRoguelike)
    {
        List<DeckSaveData> decks = isRoguelike ? Data.roguelikeDecks : Data.cpuBattleDecks;
        while (decks.Count <= deckNum)
            decks.Add(new DeckSaveData());

        decks[deckNum].cardIds = ConvertCountListToCardIds(EditingDeckCounts);
        SaveToFile();

        Debug.Log($"[CommitDeckEdit] deckNum={deckNum} isRoguelike={isRoguelike} " +
            $"savedCardIds.Count={decks[deckNum].cardIds.Count}");
    }

    private int SumOf(List<int> counts)
    {
        int sum = 0;
        foreach (int c in counts) sum += c;
        return sum;
    }

    // IDそのままリスト → 添字方式（枚数ベース）に変換。sizeは必ず全カード種数を渡す
    private List<int> ConvertCardIdsToCountList(List<int> cardIds, int size)
    {
        List<int> result = new List<int>(new int[size]);
        if (cardIds == null) return result;

        foreach (int id in cardIds)
        {
            int index = id - 1;
            if (index >= 0 && index < result.Count)
                result[index]++;
        }
        return result;
    }

    // 添字方式（枚数ベース） → IDそのままリストに変換
    private List<int> ConvertCountListToCardIds(List<int> countList)
    {
        List<int> result = new List<int>();
        if (countList == null) return result;

        for (int i = 0; i < countList.Count; i++)
        {
            int cardId = i + 1;
            int count = countList[i];
            for (int n = 0; n < count; n++)
                result.Add(cardId);
        }
        return result;
    }

    // ========================================
    // 所持カード関連
    // ========================================
    public void AddCard(int cardId)
    {
        CardOwnershipConverter.AddCard(Data.ownedCardCounts, cardId);
    }

    public bool RemoveCard(int cardId)
    {
        return CardOwnershipConverter.RemoveCard(Data.ownedCardCounts, cardId);
    }

    public int GetCardCount(int cardId)
    {
        return CardOwnershipConverter.GetCardCount(Data.ownedCardCounts, cardId);
    }

    public List<int> GetOwnedCardIds()
    {
        return CardOwnershipConverter.ToCardIdList(Data.ownedCardCounts);
    }

    // ========================================
    // 所持金関連
    // ========================================
    public int Gold => Data.gold;

    public void AddGold(int amount)
    {
        Data.gold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (Data.gold < amount) return false;
        Data.gold -= amount;
        return true;
    }

    // ========================================
    // パックアンロック関連
    // ========================================
    public bool IsPackUnlocked(int packId)
    {
        return Data.unlockedPackIds.Contains(packId);
    }

    public void UnlockPack(int packId)
    {
        if (!Data.unlockedPackIds.Contains(packId))
            Data.unlockedPackIds.Add(packId);
    }

    // ========================================
    // デッキ関連
    // ========================================
    public void AddCpuBattleDeck(DeckSaveData deck)
    {
        Data.cpuBattleDecks.Add(deck);
    }

    public void AddRoguelikeDeck(DeckSaveData deck)
    {
        Data.roguelikeDecks.Add(deck);
    }

    // ========================================
    // ローグライク進捗関連
    // ========================================
    public RoguelikeSaveData RoguelikeProgress => Data.roguelikeProgress;

    public void ResetRoguelikeProgress()
    {
        Data.roguelikeProgress = new RoguelikeSaveData();
    }

    // ========================================
    // ステージクリア関連
    // ========================================
    public void ClearLethalPuzzleStage(int stageId)
    {
        if (!Data.stageClearData.clearedLethalPuzzleStageIds.Contains(stageId))
            Data.stageClearData.clearedLethalPuzzleStageIds.Add(stageId);
    }

    public void ClearRoguelikeStage(string stageName)
    {
        if (!Data.stageClearData.clearedRoguelikeStageNames.Contains(stageName))
            Data.stageClearData.clearedRoguelikeStageNames.Add(stageName);
    }

    public void ClearTwoPickStage(int stageId)
    {
        if (!Data.stageClearData.clearedTwoPickStageIds.Contains(stageId))
            Data.stageClearData.clearedTwoPickStageIds.Add(stageId);
    }

    public bool IsLethalPuzzleStageCleared(int stageId)
    {
        return Data.stageClearData.clearedLethalPuzzleStageIds.Contains(stageId);
    }

    public bool IsRoguelikeStageCleared(string stageName)
    {
        return Data.stageClearData.clearedRoguelikeStageNames.Contains(stageName);
    }

    public bool IsTwoPickStageCleared(int stageId)
    {
        return Data.stageClearData.clearedTwoPickStageIds.Contains(stageId);
    }

    // ========================================
    // 設定関連
    // ========================================
    public GameSettings Settings => Data.settings;
}