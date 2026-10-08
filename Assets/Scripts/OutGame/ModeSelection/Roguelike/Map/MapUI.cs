using System.Collections.Generic;
using UnityEngine;

public class MapUI : MonoBehaviour
{
    [SerializeField] private List<RoguelikeStageUI> stageUIs; // 全ステージを登録
    [SerializeField] private MapStatusUI statusUI;

    private RoguelikeStageUI currentStageUI;

    public void Initialize(RoguelikeStageData stageData, MapData mapData, RoguelikeGameState state)
    {
        // 全ステージを非アクティブに
        foreach (var stageUI in stageUIs)
            stageUI.gameObject.SetActive(false);

        // stageDataに対応するStageUIを探してアクティブ化
        currentStageUI = stageUIs.Find(s => s.StageData == stageData);

        if (currentStageUI == null)
        {
            Debug.LogError($"対応するStageUIが見つかりません: {stageData.stageName}");
            return;
        }
        currentStageUI.gameObject.SetActive(true);
        currentStageUI.Initialize(mapData, state);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void PlayHealEffect(int healAmount, RoguelikeGameState state)
    {
        if (currentStageUI != null)
            currentStageUI.PlayHealEffect(healAmount, state);
    }

    // MapUI自体はアクティブのまま、現在表示中のステージUIだけを非表示にする
    public void HideCurrentStage()
    {
        if (currentStageUI != null)
            currentStageUI.Hide();
    }
}