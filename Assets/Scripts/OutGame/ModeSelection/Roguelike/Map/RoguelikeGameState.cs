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

    public RoguelikeGameState(RoguelikeStartConfig config, RoguelikeStageData stageData)
    {
        MaxHP = stageData.playerInitialHP;
        CurrentHP = MaxHP;
        Gold = 0;
        CurrentDeck = new List<int>(config.DeckId);
    }
}