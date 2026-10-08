using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TwoPickData", menuName = "GameMode/TwoPickData")]
public class TwoPickData : ScriptableObject
{
    [Header("2Pick基本設定")]
    public string modeName = "2Pick";
    [TextArea(3, 5)]
    public string description;

    [Header("ピック設定")]
    public int deckSize = 30;
    public int pickRounds = 15; // 何回選択するか
    public int cardsPerPick = 2; // 1回につきいくつから選択するか

    [Header("使用可能カードプール")]
    public List<CardGroup> twoPickCards = new List<CardGroup>();

    [Header("ゲーム設定")]
    public int playerInitialHP = 20;
    public int enemyInitialHP = 20;

    [Header("敵デッキ設定（1ステージ分。対戦相手ごとに順に設定）")]
    public List<EnemyDeckEntry> enemyDecks = new List<EnemyDeckEntry>();

    [Header("報酬設定")]
    public int moneyPerWin = 10; // 1勝ごとに獲得できるお金
}

[System.Serializable]
public class CardGroup
{
    public List<int> cards = new List<int>();
}

[System.Serializable]
public class EnemyDeckEntry
{
    public List<int> cards = new List<int>();
}