using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // 所持カード（カードIDのリスト）
    public List<int> ownedCardIds = new List<int>();

    // 所持金
    public int gold = 0;

    // パックのアンロック状況（パックIDのリスト）
    public List<int> unlockedPackIds = new List<int>();

    // CPUとの対戦モード用の登録デッキ
    public List<DeckSaveData> cpuBattleDecks = new List<DeckSaveData>();

    // ローグライクモード用の登録デッキ
    public List<DeckSaveData> roguelikeDecks = new List<DeckSaveData>();

    // ローグライクモードの進捗
    public RoguelikeSaveData roguelikeProgress = new RoguelikeSaveData();

    // 各モードのステージクリア状況
    public StageClearData stageClearData = new StageClearData();

    // 設定
    public GameSettings settings = new GameSettings();
}

// ========================================
// デッキ保存データ（CPU対戦・ローグライク共通）
// ========================================
[System.Serializable]
public class DeckSaveData
{
    public string deckName;
    public List<int> cardIds = new List<int>();
}

// ========================================
// ローグライクモードの進捗
// ========================================
[System.Serializable]
public class RoguelikeSaveData
{
    public int currentHP;
    public int maxHP;
    public int gold;
    public List<int> currentDeck = new List<int>();
    public string currentNodeId;                          // NodeDataのnodeIdで保存
    public List<string> clearedNodeIds = new List<string>();
    public int currentMapIndex;
    public string stageDataName;                          // RoguelikeStageDataの名前で保存
    public bool isInProgress = false;                     // ローグライク進行中かどうか
}

// ========================================
// 各モードのステージクリア状況
// ========================================
[System.Serializable]
public class StageClearData
{
    // リーサルパズルのクリア済みステージID一覧
    public List<int> clearedLethalPuzzleStageIds = new List<int>();

    // ローグライクのクリア済みステージ名一覧
    public List<string> clearedRoguelikeStageNames = new List<string>();

    // 2Pickのクリア済みステージID一覧
    public List<int> clearedTwoPickStageIds = new List<int>();
}

// ========================================
// 設定
// ========================================
[System.Serializable]
public class GameSettings
{
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
}