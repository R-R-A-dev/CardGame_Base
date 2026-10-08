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

    // 2Pick: 選択中のステージデータと、現在何戦目か（0始まり）
    public static TwoPickData TwoPickData;
    public static int TwoPickBattleIndex;

    // 2Pickから戻ってきたかどうか（結果パネル表示の判定用。表示側で消費したらfalseに戻す）
    public static bool TwoPickFinished;

    // 2Pick：「やめる」を選んだ時点で確定した報酬金額
    public static int TwoPickReward;

    // デバッグ用：GameModeを設定せずに対戦した場合、終了後にモード選択画面を経由せず
    // 直接デッキ編成画面へ戻すかどうか（表示側で消費したらfalseに戻す）
    public static bool DebugReturnToDeckEdit;
    public static int DebugReturnDeckNum;

    /// <summary>
    /// 現在の対戦インデックスに対応する敵デッキを取得する（未設定・範囲外ならnull）
    /// </summary>
    public static List<int> GetTwoPickEnemyDeck()
    {
        if (TwoPickData == null || TwoPickData.enemyDecks == null)
            return null;
        if (TwoPickBattleIndex < 0 || TwoPickBattleIndex >= TwoPickData.enemyDecks.Count)
            return null;

        return new List<int>(TwoPickData.enemyDecks[TwoPickBattleIndex].cards);
    }

    /// <summary>
    /// 次の対戦（敵デッキ）が残っているかどうか
    /// </summary>
    public static bool HasNextTwoPickBattle()
    {
        return TwoPickData != null && TwoPickData.enemyDecks != null &&
            TwoPickBattleIndex >= 0 && TwoPickBattleIndex < TwoPickData.enemyDecks.Count;
    }
}