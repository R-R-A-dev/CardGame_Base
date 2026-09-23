using System.Collections.Generic;
using UnityEngine;

public class GameModeSession : MonoBehaviour
{
    public static GameMode CurrentGameMode { get; set; } = GameMode.NONE;

    // 選択されたデータ
    public static LethalPuzzleData SelectedPuzzle { get; set; }
    public static TwoPickData SelectedTwoPick { get; set; }
    public static RoguelikeStageData SelectedRoguelike { get; set; }

    // 2Pick/ローグライクで構築されたデッキ
    public static List<int> PlayerDeck { get; set; } = new List<int>();
    public static List<int> EnemyDeck { get; set; } = new List<int>();

    // ローグライク用の進行状態
    public static int CurrentStage { get; set; } = 1;
    public static int PlayerHP { get; set; } = 20;

    /// <summary>
    /// セッションをリセット
    /// </summary>
    public static void Reset()
    {
        SelectedPuzzle = null;
        SelectedTwoPick = null;
        SelectedRoguelike = null;
        PlayerDeck.Clear();
        EnemyDeck.Clear();
        CurrentStage = 1;
    }
}
