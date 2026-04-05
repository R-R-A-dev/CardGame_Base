using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoguelikeStageData", menuName = "Roguelike/RoguelikeStageData")]
public class RoguelikeStageData : ScriptableObject
{
    [Header("ステージ情報")]
    public string stageName;
    public Sprite stageBackground;
    [TextArea(2, 4)]
    public string stageDescription;

    [Header("マップ一覧（順番に進む）")]
    public List<MapData> maps;

    [Header("プレイヤー初期設定")]
    public int playerInitialHP = 20;
    public int playerInitialMana = 1;
}