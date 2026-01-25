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
    public int cardsPerPick = 2; // 1回につき何枚から選択

    [Header("使用可能カードプール")]
    public List<CardGroup> twoPickCards = new List<CardGroup>();

    [Header("ゲーム設定")]
    public int playerInitialHP = 20;
    public int enemyInitialHP = 20;
}

[System.Serializable]
public class CardGroup
{
    public List<int> cards = new List<int>();
}
