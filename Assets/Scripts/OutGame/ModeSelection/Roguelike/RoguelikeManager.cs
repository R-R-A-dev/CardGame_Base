using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoguelikeManager : MonoBehaviour
{
    public static RoguelikeManager Instance { get; private set; }

    public RoguelikeGameState GameState => gameState;

    [SerializeField] private DeckAndStageSelectUI deckAndStageSelectUI;
    [SerializeField] private MapUI mapUI;
    [SerializeField] private List<RoguelikeStageData> stageData;

    [SerializeField] private RestUI restUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private TreasureUI treasureUI;
    [SerializeField] private EventUI eventUI;
    [SerializeField] private CardLossUI cardLossUI;


    [SerializeField] private GameObject treasureCardSelectPanel;

    [SerializeField] private BattleRewardUI battleRewardUI;
    [SerializeField] private RoguelikeStageClearUI stageClearUI;

    [SerializeField] private GameObject Header;

    private RoguelikeGameState gameState;
    private RoguelikeStageData currentStageData;
    private int currentMapIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 戦闘シーンから戻ってきた場合
        if (RoguelikeSession.GameState != null)
        {

            gameState = RoguelikeSession.GameState;
            currentStageData = gameState.CurrentStageData;
            currentMapIndex = gameState.CurrentMapIndex;
            if (RoguelikeSession.IsBattleWin)
                OnBattleWin();      // 報酬パネルを開く
            else
                OnGameOver();       // ゲームオーバー処理

            RoguelikeSession.Clear();
            return;
        }

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

    /// <summary>
    /// DeckAndStageSelectUIの開始ボタンから呼ぶ
    /// RoguelikeStartConfigとRoguelikeStageDataを所持しておく</summary>
    /// <param name="config"></param>
    public RoguelikeGameState StartRoguelike(RoguelikeStartConfig config)
    {
        treasureCardSelectPanel.SetActive(false);

        currentStageData = stageData[config.StageConfig.StageId];
        currentMapIndex = 0;

        // ゲーム状態を初期化
        gameState = new RoguelikeGameState(config, stageData[config.StageConfig.StageId]);

        // デッキ選択画面を閉じてマップへ
        //deckAndStageSelectUI.gameObject.SetActive(false);
        OpenMap();

        return gameState;
    }

    /// <summary>
    /// マップ開く
    /// </summary>
    private void OpenMap()
    {
        MapData mapData = currentStageData.maps[currentMapIndex];
        mapUI.Initialize(currentStageData, mapData, gameState); // stageDataも渡す
    }

    /// <summary>
    /// MapManagerからノード選択時に呼ばれる
    /// </summary>
    /// <param name="nodeData"></param>
    public void OnNodeSelected(NodeData nodeData)
    {
        if (nodeData.modifiers != null && nodeData.modifiers.Count > 0)
            ApplyModifiers(nodeData.modifiers);


        switch (nodeData.stageType)
        {
            case StageType.NORMAL_BATTLE:
            case StageType.ELITE_BATTLE:
            case StageType.BOSS_BATTLE:
                // 戦闘データをセッションに保存して別シーンへ
                RoguelikeSession.CurrentNode = nodeData;
                RoguelikeSession.GameState = gameState;
                RoguelikeSession.BattleModifiers = nodeData.modifiers;
                ModeConfigManager.Instance.currentGameMode = GameMode.ROGUELIKE;
                //RoguelikeSession.GameState.CurrentDeck
                SceneTransition.Load("Game");
                break;

            case StageType.REST:
                restUI.Open(nodeData.restData, gameState);
                break;

            case StageType.SHOP:
                shopUI.Open(nodeData.shopData, gameState);
                break;

            case StageType.TREASURE:
                treasureUI.Open(nodeData.treasureData, gameState);
                break;

            case StageType.DAMAGE:
                ApplyDamage(nodeData.damageData);
                break;

            case StageType.CARD_LOSS:
                cardLossUI.Open(nodeData.cardLossData, gameState);
                break;
        }
    }

    // RoguelikeManager
    private void ApplyModifiers(List<ParameterModifier> modifiers)
    {
        foreach (ParameterModifier modifier in modifiers)
        {
            if (modifier.applyToPlayer)
                ApplyModifierToPlayer(modifier);
        }
    }

    private void ApplyModifierToPlayer(ParameterModifier modifier)
    {
        int value = modifier.isPercentage
            ? Mathf.FloorToInt(gameState.MaxHP * modifier.value / 100f)
            : modifier.value;

        switch (modifier.modifierType)
        {
            case ParameterModifierType.HP_BOOST:
                gameState.MaxHP += value;
                gameState.CurrentHP += value;
                break;
            case ParameterModifierType.HP_REDUCTION:
                gameState.MaxHP -= value;
                //gameState.CurrentHP = Mathf.Min(gameState.CurrentHP, gameState.MaxHP);
                break;
            case ParameterModifierType.MANA_BOOST:
                break;
            case ParameterModifierType.MANA_REDUCTION:
                break;
            case ParameterModifierType.ATTACK_BOOST:
                break;
            case ParameterModifierType.ATTACK_REDUCTION:
                break;
            case ParameterModifierType.DEFENSE_BOOST:
                break;
            case ParameterModifierType.DEFENSE_REDUCTION:
                break;
            case ParameterModifierType.CARD_DRAW_BOOST:
                break;
            case ParameterModifierType.CARD_DRAW_REDUCTION:
                break;
        }
    }

    // ========================================
    // 回復演出（RestUIから呼ばれる）
    // ========================================
    public void PlayHealEffect(int healAmount)
    {
        mapUI.PlayHealEffect(healAmount, gameState);
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
        // シーン再読み込み後のStart()でgameState.CurrentMapIndexから復元されるため同期しておく
        gameState.CurrentMapIndex = currentMapIndex;

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
        Debug.Log("ステージクリア！");

        // 報酬パネルを開く（ランで貯めたゴールドを所持金に加算）
        // MapUI自体はアクティブのまま、現在のステージUIだけを非表示にする
        mapUI.HideCurrentStage();
        stageClearUI.Open(gameState, currentStageData.stageName);
    }

    // ========================================
    // ステージクリア報酬パネルを閉じた時（RoguelikeStageClearUIから呼ばれる）
    // ========================================
    public void OnStageClearConfirmed()
    {
        deckAndStageSelectUI.gameObject.SetActive(true);
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
        if (damageData == null)
        {
            Debug.LogWarning("DamageNodeDataがnullです");
            ReturnToMap();
            return;
        }

        int damage = damageData.isPercentageDamage
            ? Mathf.FloorToInt(gameState.MaxHP * damageData.damagePercentage / 100f)
            : damageData.damageAmount;

        gameState.CurrentHP = Mathf.Max(0, gameState.CurrentHP - damage);

        Debug.Log($"ダメージ: {damage} / 残りHP: {gameState.CurrentHP}");

        if (gameState.CurrentHP <= 0)
        {
            OnGameOver();
            return;
        }

        ReturnToMap();
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
    public void OnBattleWin()
    {

        // 報酬パネルを開く
        NodeData node = RoguelikeSession.CurrentNode;
        if (node.rewardData != null)
        {
            Debug.Log("報酬パネルを開く");
            //mapUI.Hide();
            mapUI.gameObject.SetActive(true);
            battleRewardUI.Open(node.rewardData, gameState);
        }
        else
        {
            Debug.Log("報酬なし、マップへ戻る");
            ReturnToMap();
            return;
        }
    }

    // ========================================
    // パネルを閉じるボタンから呼ぶ
    // ========================================
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        Header.SetActive(false);
    }
}