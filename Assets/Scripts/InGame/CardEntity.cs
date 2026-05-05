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
    public int price;
    [TextArea] public string description;
    public AudioClip summonAudio;
    public AudioClip summonAbilityAudio;
    public AudioClip attackAudio;
    public AudioClip hitAudio;
    public AudioClip destroyAudio;
    public ParticleSystem summonEffect;
    public ParticleSystem summonAbilityEffect;
    public ParticleSystem attackEffect;
    public ParticleSystem hitEffect;
    public ParticleSystem destroyEffect;
    public float attackTime = 0f;
    public Sprite icon;
    public ATTACKTYPE attackType;
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
    SUMMON_SPECIFIC_UNIT = 4096, // 特定のユニットをフィールドに出す
    ON_DESTROY_TRIGGER = 8192,   // 破壊された時に発動
    EFFECT_SELECTION_FRIEND = 16384,  // 味方効果対象を選択
    EFFECT_SELECTION_ENEMY = 32768,   // 敵効果対象を選択
    RANDOM_ENEMY = 65536,        // 敵のランダムターゲット
    RANDOM_FRIEND = 131072,      // 味方のランダムターゲット
    REDUCE_HAND_COST = 262144,   // 手札のカードのコストを減らす
    INCREASE_ENEMY_COST = 524288, // 敵のコストを増やす
    DAMAGE_NULLIFY_ONCE = 1048576,// 一度だけ受けるダメージを0にする
    DOUBLE_ACTION = 2097152,     // 二回行動
    HEAL_BY_DAMAGE = 4194304,    // 攻撃した分回復する
    PIERCE = 8388608,            // 貫通
    CONDITIONAL_ENEMY_DEBUFF = 16777216, // 敵のパラメータ変更（条件付き）
    STATS_UP_ON_ATTACK = 33554432, // 攻撃をするたびにパラメータアップ
    //RANDOM_DAMAGE = 67108864,     // ランダムダメージ
    DISCARD_ENEMY_HAND = 67108864,  // 相手の手札を破棄
    DISCARD_ALL_ENEMY_HAND = 134217728, // 相手の手札を全て破棄
    DISCARD_FRIEND_HAND = 268435456,  // 自分の手札を破棄
    DISCARD_ALL_FRIEND_HAND = 536870912, // 自分の手札を全て破棄
    DESTROY_ATTACKED_TARGET = 1073741824, // 攻撃した相手を破壊する
    //ON_FIELD_TRIGGER = 4294967296, //フィールで任意のタイミングでアビリティ発動
}

public enum ATTACKTYPE
{
    NONE,
    THROW,
    DIRECT,
    SPAWN,
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
    EFFECT_SELECTION_FRIEND = 2048,  // 味方効果対象を選択
    EFFECT_SELECTION_ENEMY = 4096,   // 敵効果対象を選択
    REDUCE_HAND_COST = 8192,         // 手札のカードのコストを減らす
    INCREASE_ENEMY_COST = 16384,     // 敵のコストを増やす
    HEAL_BY_DAMAGE = 32768,          // 攻撃した分回復する頭
    CONDITIONAL_ENEMY_DEBUFF = 65536, // 敵のパラメータ変更（条件付き）
    CONDITIONAL_FRIEND_BUFF = 131072, // 味方のパラメータ変更（条件付き）
    RANDOM_DAMAGE = 262144,          // ランダムダメージ
    DESTROY_ALL_FIELD_CARDS = 524288, // 場のカードをすべて破壊する
    RANDOM_ENEMY = 1048576,          // 敵のランダムターゲット
    RANDOM_FRIEND = 2097152,         // 味方のランダムターゲット
    SWAP_HP_ATK = 4194304,           // HPと攻撃力を入れ替える
    DISCARD_ENEMY_HAND = 8388608,    // 相手の手札を破棄
    DISCARD_ALL_ENEMY_HAND = 16777216, // 相手の手札を全て破棄
    DISCARD_FRIEND_HAND = 33554432,  // 自分の手札を破棄
    DISCARD_ALL_FRIEND_HAND = 67108864, // 自分の手札を全て破棄
}