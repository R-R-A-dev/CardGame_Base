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
        CurrentDeck = new List<int>(config.DeckId);
        CurrentDeck.Add(3);
        CurrentDeck.Add(3);
        CurrentStageData = stageData;
        CurrentMapIndex = 0;
    }
}

