using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoguelikeData", menuName = "GameMode/RoguelikeData")]
public class RoguelikeData : ScriptableObject
{
    [Header("所持ステージ設定")]
    public List<StageRow> stageMap = new List<StageRow>();

    [Header("ローグライク基本設定")]
    public string runName = "Adventure Run";
    [TextArea(3, 5)]
    public string description;

    [Header("ゲーム設定")]
    public int playerInitialHP = 20;
    public int startingDeckSize = 40;

    [Header("初期デッキ")]
    public List<int> startingDeck = new List<int>();

    [Header("ステージ設定")]
    public int totalStages = 10;
    public List<int> bossStages = new List<int> { 3, 6, 10 }; // ボス戦のステージ番号

    [Header("報酬設定")]
    public int cardsPerReward = 5; // 報酬で選べるカード数
    public List<int> rewardCardPool = new List<int>(); // 報酬カードプール

    [Header("敵設定")]
    public List<int> normalEnemyDecks = new List<int>(); // 通常敵のデッキリスト
    public List<int> bossEnemyDecks = new List<int>(); // ボスのデッキリスト

}

[System.Serializable]
public class StageRow
{
    [Tooltip("この行のステージ一覧 (列方向)")]
    public List<StageData> stages = new List<StageData>();
}
/*ローグライクモードで必要なデータや画面、他も
 * 開始ボタンを押すとデッキデッキ選択ウィンドウ開く
 * 画面外か×ボタンで戻る
 * 開始を押すと遷移
 * 
 * 
 * 
 * 
 * 
 * 
*/