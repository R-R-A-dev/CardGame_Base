using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CardEntity", menuName = "Create CardEntity")]
public class CardEntity : ScriptableObject
{
    public int no;
    public new string name;
    public int hp;
    public int at;
    public int cost;
    public int effectDmg;
    public int effectHeal;
    [TextArea] public string description;
    public AudioClip summonAudio;
    public AudioClip attackAudio;
    public AudioClip destroyAudio;
    public Sprite icon;
    public RARE rare;
    public ABILITY ability;
    public SPELL spell;
    public SPELLS spells;
    public ABILITIES abilities;
    public int[] targetCardID;

}

[Flags]
public enum ABILITIES
{
    NONE = 0,
    INIT_ATTACKABLE = 1,         // 速攻
    SHIELD = 2,                  // 守護
    DAMAGE_ENEMY_CARD = 4,       // 敵フォロワー一体を攻撃
    DAMAGE_ENEMY_CARDS = 8,      // 敵フォロワー全体を攻撃
    DAMAGE_ENEMY_HERO = 16,      // 敵リーダーへのダメージ
    HEAL_FRIEND_CARD = 32,       // 自分のフォロワー一体を回復
    HEAL_FRIEND_CARDS = 64,      // 自分のフォロワー全体回復
    HEAL_FRIEND_HERO = 128,      // 自分のリーダーを回復
    DESTROY_ENEMY_CARD = 256,    // 敵を一体倒す
    STEAL_ENEMY_CARD = 512,      // 敵のフィールドのカードを自分のフィールドのカードにする
    DRAW_CARDS = 1024,           // カードを複数枚引く //ここまでやった
    SEARCH_SPECIFIC_UNIT = 2048, // 特定のユニットを手札にする
    SUMMON_SPECIFIC_UNIT = 4096, // 特定のユニットをフィールドに出す//保留
    ON_DESTROY_TRIGGER = 8192,   // 破壊された時に発動
    EFFECT_SELECTION = 16384,    // 効果対象を選択
    REDUCE_HAND_COST = 32768,    // 手札のカードのコストを減らす
    INCREASE_ENEMY_COST = 65536, // 敵のコストを増やす
    DAMAGE_NULLIFY_ONCE = 131072,// 一度だけ受けるダメージを0にする
    DOUBLE_ACTION = 262144,      // 二回行動
    HEAL_BY_DAMAGE = 524288,     // 攻撃した分回復する
    PIERCE = 1048576,            // 貫通
    CONDITIONAL_ENEMY_DEBUFF = 2097152, // 敵のパラメータ変更（条件付き）
    STATS_UP_ON_ATTACK = 4194304, // 攻撃をするたびにパラメータアップ
    RANDOM_DAMAGE = 8388608,     // ランダムダメージ
    DISCARD_ENEMY_HAND = 16777216,   // 相手の手札を破棄
    DISCARD_ALL_ENEMY_HAND = 33554432, // 相手の手札を全て破棄
    DISCARD_FRIEND_HAND = 67108864,  // 自分の手札を破棄
    DISCARD_ALL_FRIEND_HAND = 134217728, // 自分の手札を全て破棄
}

public enum RARE 
{
    N,
    R,
    SR,
    UR,
}


public enum ABILITY
{
    NONE,
    INIT_ATTACKABLE,
    SHIELD,
}

public enum SPELL
{
    NONE,
    DAMAGE_ENEMY_CARD,
    DAMAGE_ENEMY_CARDS,
    DAMAGE_ENEMY_HERO,
    HEAL_FRIEND_CARD,
    HEAL_FRIEND_CARDS,
    HEAL_FRIEND_HERO,
    STEAL_ENEMY_CARD,
    DRAW_CARDS,
}

[Flags]
public enum SPELLS
{
    NONE = 0,
    DAMAGE_ENEMY_CARD = 1,           // 敵フォロワー一体を攻撃
    DAMAGE_ENEMY_CARDS = 2,          // 敵フォロワー全体を攻撃
    DAMAGE_ENEMY_HERO = 4,           // 敵リーダーへのダメージ
    HEAL_FRIEND_CARD = 8,            // 自分のフォロワー一体を回復
    HEAL_FRIEND_CARDS = 16,          // 自分のフォロワー全体回復
    HEAL_FRIEND_HERO = 32,           // 自分のリーダーを回復
    DESTROY_ENEMY_CARD = 64,         // 敵を一体倒す
    STEAL_ENEMY_CARD = 128,          // 敵のフィールドのカードを自分のフィールドのカードにする
    DRAW_CARDS = 256,                // カードを複数枚引く
    SEARCH_SPECIFIC_UNIT = 512,      // 特定のユニットを手札にする
    SUMMON_SPECIFIC_UNIT = 1024,     // 特定のユニットをフィールドに出す
    EFFECT_SELECTION = 2048,         // 効果対象を選択
    REDUCE_HAND_COST = 4096,         // 手札のカードのコストを減らす
    INCREASE_ENEMY_COST = 8192,     // 敵のコストを増やす
    HEAL_BY_DAMAGE = 16384,          // 攻撃した分回復する
    CONDITIONAL_ENEMY_DEBUFF = 32768, // 敵のパラメータ変更（条件付き）
    STATS_UP_ON_ATTACK = 65536,     // 攻撃をするたびにパラメータアップ
    RANDOM_DAMAGE = 131072,          // ランダムダメージ
}