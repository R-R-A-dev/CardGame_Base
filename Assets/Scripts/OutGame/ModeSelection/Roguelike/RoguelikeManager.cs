using System.Collections.Generic;
using UnityEngine;

public class RoguelikeManager : MonoBehaviour
{
    public static RoguelikeManager Instance { get; private set; }

    [SerializeField] private DeckAndStageSelectUI deckAndStageSelectUI;
    [SerializeField] private MapUI mapUI;
    [SerializeField] private List<RoguelikeStageData> stageData;

    private RoguelikeGameState gameState;
    private RoguelikeStageData currentStageData;
    private int currentMapIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 最初は全て非アクティブ
        //deckAndStageSelectUI.gameObject.SetActive(false);
        //mapUI.Hide();
    }

    // ========================================
    // タイトル画面のローグライクボタンから呼ぶ
    // ========================================
    public void OpenDeckAndStageSelect()
    {
        deckAndStageSelectUI.gameObject.SetActive(true);
    }

    // ========================================
    // DeckAndStageSelectUIの開始ボタンから呼ぶ
    // RoguelikeStartConfigとRoguelikeStageDataを所持しておく
    // ========================================
    public void StartRoguelike(RoguelikeStartConfig config)
    {
        currentStageData = stageData[config.StageConfig.StageId];
        currentMapIndex = 0;

        // ゲーム状態を初期化
        gameState = new RoguelikeGameState(config, stageData[config.StageConfig.StageId]);

        // デッキ選択画面を閉じてマップへ
        //deckAndStageSelectUI.gameObject.SetActive(false);
        OpenMap();
    }

    // ========================================
    // マップを開く
    // ========================================
    private void OpenMap()
    {
        MapData mapData = currentStageData.maps[currentMapIndex];
        mapUI.Initialize(currentStageData, mapData, gameState); // stageDataも渡す
    }

    // ========================================
    // MapManagerからノード選択時に呼ばれる
    // ========================================
    public void OnNodeSelected(NodeData nodeData)
    {
        switch (nodeData.stageType)
        {
            case StageType.NORMAL_BATTLE:
            case StageType.ELITE_BATTLE:
            case StageType.BOSS_BATTLE:
                // 戦闘画面へ
                // BattleManager.Instance.StartBattle(nodeData.enemyData, gameState);
                mapUI.Hide();
                break;

            case StageType.REST:
                // 休憩画面へ
                // RestUI.Instance.Open(nodeData.restData, gameState);
                mapUI.Hide();
                break;

            case StageType.SHOP:
                // ショップ画面へ
                // ShopUI.Instance.Open(nodeData.shopData, gameState);
                mapUI.Hide();
                break;

            case StageType.TREASURE:
                // 宝箱画面へ
                // TreasureUI.Instance.Open(nodeData.treasureData, gameState);
                mapUI.Hide();
                break;

            case StageType.EVENT:
                // イベント画面へ
                // EventUI.Instance.Open(nodeData.eventData, gameState);
                mapUI.Hide();
                break;

            case StageType.DAMAGE:
                // ダメージ処理（即時適用）
                ApplyDamage(nodeData.damageData);
                break;

            case StageType.CARD_LOSS:
                // カード消失処理
                // CardLossUI.Instance.Open(nodeData.cardLossData, gameState);
                mapUI.Hide();
                break;
        }
    }

    // ========================================
    // 各イベントクリア後にマップへ戻る
    // ========================================
    public void ReturnToMap()
    {
        // ボスノードクリア後は次のマップへ
        if (gameState.CurrentNode == currentStageData.maps[currentMapIndex].bossNode)
        {
            OnMapCleared();
            return;
        }

        OpenMap();
    }

    // ========================================
    // マップクリア時
    // ========================================
    private void OnMapCleared()
    {
        currentMapIndex++;

        // 全マップクリアでステージクリア
        if (currentMapIndex >= currentStageData.maps.Count)
        {
            OnStageClear();
            return;
        }

        // 次のマップへ
        gameState.CurrentNode = null;
        OpenMap();
    }

    // ========================================
    // ステージクリア時
    // ========================================
    private void OnStageClear()
    {
        // ステージクリア画面へ
        // StageClearUI.Instance.Open();
        Debug.Log("ステージクリア！");
    }

    // ========================================
    // ゲームオーバー時
    // ========================================
    public void OnGameOver()
    {
        mapUI.Hide();
        // GameOverUI.Instance.Open();
        Debug.Log("ゲームオーバー");
    }

    // ========================================
    // ダメージ処理（DAMAGEノード用）
    // ========================================
    private void ApplyDamage(DamageNodeData damageData)
    {
        if (damageData == null) return;

        int damage = damageData.isPercentageDamage
            ? Mathf.FloorToInt(gameState.MaxHP * damageData.damagePercentage / 100f)
            : damageData.damageAmount;

        gameState.CurrentHP = Mathf.Max(0, gameState.CurrentHP - damage);

        // HP0でゲームオーバー
        if (gameState.CurrentHP <= 0)
        {
            OnGameOver();
            return;
        }

        // ダメージ後はそのままマップへ戻る
        OpenMap();
    }

    // ========================================
    // デッキ編集画面を開く
    // ========================================
    public void OpenDeckEdit()
    {
        DeckBuilderManager.Instance.deckBuilderUI.gameObject.SetActive(true);
    }

    public void CloseDeckEdit()
    {
        DeckBuilderManager.Instance.deckBuilderUI.gameObject.SetActive(false);
    }
}