using System.Collections.Generic;
using UnityEngine;

public class RoguelikeStageUI : MonoBehaviour
{
    [SerializeField] private RoguelikeStageData stageData; // 対応するSO
    [SerializeField] private List<MapManager> mapManagers; // このステージのマップ群

    public RoguelikeStageData StageData => stageData;

    public void Initialize(MapData mapData, RoguelikeGameState state)
    {
        // 全マップを非アクティブに
        foreach (var manager in mapManagers)
            manager.gameObject.SetActive(false);

        // mapDataに対応するManagerを探してアクティブ化
        MapManager target = mapManagers.Find(m => m.MapData == mapData);

        if (target == null)
        {
            Debug.LogError($"対応するMapManagerが見つかりません: {mapData.mapName}");
            return;
        }

        target.gameObject.SetActive(true);
        target.Initialize(state);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}