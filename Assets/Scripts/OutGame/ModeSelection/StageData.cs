using System.Collections.Generic;
using UnityEngine;

// ========================================
// ステージタイプ
// ========================================
public enum StageType
{
    NORMAL_BATTLE,  // 通常戦闘
    ELITE_BATTLE,   // エリート戦闘
    BOSS_BATTLE,    // ボス戦闘
    TREASURE,       // 宝箱（報酬のみ）
    SHOP,           // ショップ
    EVENT,          // イベント
    REST,           // 休憩（HP回復）
    DAMAGE,         // ダメージ
    CARD_LOSS       // カードロスト
}

// ========================================
// パラメータ変更タイプ
// ========================================
public enum ParameterModifierType
{
    HP_BOOST,           // 最大HP増加
    HP_REDUCTION,       // 最大HP減少
    MANA_BOOST,         // 最大マナ増加
    MANA_REDUCTION,     // 最大マナ減少
    ATTACK_BOOST,       // 攻撃力増加
    ATTACK_REDUCTION,   // 攻撃力減少
    DEFENSE_BOOST,      // 防御力増加
    DEFENSE_REDUCTION,  // 防御力減少
    CARD_DRAW_BOOST,    // ドロー枚数増加
    CARD_DRAW_REDUCTION // ドロー枚数減少
}

// ========================================
// パラメータ変更設定
// ========================================
[System.Serializable]
public class ParameterModifier
{
    [Header("変更タイプ")]
    public ParameterModifierType modifierType;

    [Header("変更値")]
    public int value; // 増減値
    public bool isPercentage; // パーセント指定か（falseなら固定値）

    [Header("適用対象")]
    public bool applyToPlayer = true; // プレイヤーに適用
    public bool applyToEnemy = false;  // 敵に適用

    [Header("説明")]
    [TextArea(1, 2)]
    public string description; // UI表示用
}

// ========================================
// ステージデータ ScriptableObject
// ========================================
[CreateAssetMenu(fileName = "StageData", menuName = "Roguelike/StageData")]
public class StageData : ScriptableObject
{
    [Header("=== ステージ基本情報 ===")]
    public string stageName = "Stage 1";
    public int stageNumber = 1;
    public StageType stageType = StageType.NORMAL_BATTLE;

    [TextArea(2, 4)]
    public string stageDescription;

    public Sprite stageIcon; // ステージアイコン
    public Sprite stageBackground; // 背景画像

    [Header("=== 敵設定 ===")]
    [Tooltip("敵の名前")]
    public string enemyName = "Goblin";

    [Tooltip("敵のHP")]
    public int enemyHP = 20;

    [Tooltip("敵の初期マナ")]
    public int enemyInitialMana = 1;

    [Tooltip("敵が使用するデッキ（カードIDのリスト）")]
    public List<int> enemyDeck = new List<int>();

    [Header("=== パラメータ変更ギミック ===")]
    [Tooltip("このステージで適用されるパラメータ変更")]
    public List<ParameterModifier> parameterModifiers = new List<ParameterModifier>();

    [Header("=== 報酬設定 ===")]
    [Tooltip("クリア時のゴールド報酬")]
    public int goldReward = 50;

    [Tooltip("クリア時に選べるカード数")]
    public int cardRewardCount = 3;

    [Tooltip("報酬カードプール（空の場合は共通プールから）")]
    public List<int> rewardCardPool = new List<int>();

    [Header("=== 特殊設定 ===")]
    [Tooltip("休憩地点の場合の回復量")]
    public int healAmount = 10;

    [Tooltip("ショップの場合の販売カード数")]
    public int shopCardCount = 5;

    [Tooltip("ショップの場合の販売カードリスト")]
    public List<int> shopCardList = new List<int>();

    [Tooltip("宝箱の場合のカード獲得数")]
    public int treasureCardCount = 1;
}

/*
 * ステージ情報
 * マップ情報
 * ステージ情報
 * 敵情報
 * バフデバフ
 * 報酬
 * 
 * 
*/