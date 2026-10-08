using System.Collections.Generic;

[System.Serializable]
public class RoguelikeGameState
{
    public int CurrentHP;
    public int MaxHP;
    public int Gold;
    public List<int> CurrentDeck = new List<int>();
    public NodeData CurrentNode;
    public List<NodeData> ClearedNodes = new List<NodeData>();

    // シーンをまたぐために追加
    public RoguelikeStageData CurrentStageData;
    public int CurrentMapIndex;

    public RoguelikeGameState(RoguelikeStartConfig config, RoguelikeStageData stageData)
    {
        MaxHP = stageData.playerInitialHP;
        CurrentHP = MaxHP;
        Gold = 50;

        // 選択されたデッキIDから実際のカードIDリストを取得
        List<DeckSaveData> decks = GameDataHolder.Instance.Data.roguelikeDecks;
        CurrentDeck = (config.DeckId >= 0 && config.DeckId < decks.Count)
            ? new List<int>(decks[config.DeckId].cardIds)
            : new List<int>();

        CurrentStageData = stageData;
        CurrentMapIndex = 0;
    }
}

