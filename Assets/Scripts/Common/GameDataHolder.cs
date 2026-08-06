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