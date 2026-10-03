using System.Collections.Generic;
using UnityEngine;

public class RoguelikeStageUI : MonoBehaviour
{
    [SerializeField] private RoguelikeStageData stageData; // 対応するSO
    [SerializeField] private List<MapManager> mapManagers; // このステージのマップ群

    private MapManager currentMapManager;

    public RoguelikeStageData StageData => stageData;

    public void Initialize(MapData mapData, RoguelikeGameState state)
    {
        // 全マップを非アクティブに
        foreach (var manager in mapManagers)
            manager.gameObject.SetActive(false);

        // mapDataに対応するManagerを探してアクティブ化
        currentMapManager = mapManagers.Find(m => m.MapData == mapData);

        if (currentMapManager == null)
        {
            Debug.LogError($"対応するMapManagerが見つかりません: {mapData.mapName}");
            return;
        }

        currentMapManager.gameObject.SetActive(true);
        currentMapManager.Initialize(state);
    }

    public void PlayHealEffect(int healAmount, RoguelikeGameState state)
    {
        if (currentMapManager != null)
            currentMapManager.PlayHealEffect(healAmount, state);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}