using System.Collections.Generic;

public static class RoguelikeSession
{
    public static NodeData CurrentNode;
    public static RoguelikeGameState GameState;
    public static bool IsBattleWin;
    public static List<ParameterModifier> BattleModifiers; // 追加

    public static void Clear()
    {
        CurrentNode = null;
        GameState = null;
        IsBattleWin = false;
    }
}