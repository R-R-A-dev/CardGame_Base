using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Roguelike/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("基本情報")]
    public string enemyName;
    public Sprite enemySprite;

    [Header("ステータス")]
    public int hp;
    public int initialMana;

    [Header("デッキ（カードIDのリスト）")]
    public List<int> enemyDeck;
}