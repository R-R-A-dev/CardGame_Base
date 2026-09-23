using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LethalPuzzleData", menuName = "GameMode/LethalPuzzleData")]
public class LethalPuzzleData : ScriptableObject
{
    [Header("パズル基本設定")]
    public string puzzleName;
    public int puzzleId;
    [TextArea(3, 5)]
    public string description;

    [Header("HP/マナ設定")]
    public int playerInitialHP = 20;
    public int enemyInitialHP = 10;
    public int playerInitialMana = 10;
    public int turnLimit = 1;

    [Header("初期手札")]
    public List<int> playerInitialHand = new List<int>();

    [Header("初期フィールド")]
    public List<int> playerInitialField = new List<int>();
    public List<int> enemyInitialField = new List<int>();

    [Header("デッキ（ドロー用）")]
    public List<int> playerDeck = new List<int>();
    public List<int> enemyDeck = new List<int>();
}