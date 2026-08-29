using System.Collections.Generic;
using UnityEngine;

public static class GameSession
{
    public static List<int> SelectedDeck;
    public static List<int> EnemyDeck;

    // CPU戦から戻ってきたかどうか（結果パネル表示の判定用。表示側で消費したらfalseに戻す）
    public static bool CpuBattleFinished;
    public static bool CpuBattleWon;

    // 詰みパズルから戻ってきたかどうか（結果パネル表示の判定用。表示側で消費したらfalseに戻す）
    public static bool LethalPuzzleFinished;
    public static bool LethalPuzzleWon;
}