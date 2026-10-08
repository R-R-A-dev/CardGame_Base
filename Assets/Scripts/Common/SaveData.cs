using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // 所持カード（添え字＝カードID-1、値＝所持枚数）
    // 例: ownedCardCounts[0] は カードID:1 の所持枚数
    public List<int> ownedCardCounts = new List<int>();

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

[System.Serializable]
public class DeckSaveData
{
    public string deckName;
    public List<int> cardIds = new List<int>();
}

[System.Serializable]
public class RoguelikeSaveData
{
    public int currentHP;
    public int maxHP;
    public int gold;
    public List<int> currentDeck = new List<int>();
    public string currentNodeId;
    public List<string> clearedNodeIds = new List<string>();
    public int currentMapIndex;
    public string stageDataName;
    public bool isInProgress = false;
}

[System.Serializable]
public class StageClearData
{
    public List<int> clearedLethalPuzzleStageIds = new List<int>();
    public List<string> clearedRoguelikeStageNames = new List<string>();
    public List<int> clearedTwoPickStageIds = new List<int>();
}

[System.Serializable]
public class GameSettings
{
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
}