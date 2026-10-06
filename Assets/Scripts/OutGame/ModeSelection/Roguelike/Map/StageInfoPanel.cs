using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// マップでクリックしたマスの情報を表示し、Startボタンでマスを作動させるパネル。
/// マスを選択していない間は非表示。パネル背景・ボタンの画像はインスペクターの設定をそのまま使い、スクリプトでは変更しない
/// </summary>
public class StageInfoPanel : MonoBehaviour
{
    [System.Serializable]
    public class StageTypeDisplay
    {
        public StageType stageType;
        public string tagText;              // 例: 戦闘 COMBAT
        public string stageName;            // 例: 戦闘ステージ
        [TextArea(1, 3)]
        public string description;          // 例: 敵と戦う
        public Color diamondColor = Color.white;    // ひし形の色（マップのノードと同じ色）
        public Color stageNameColor = Color.white;  // ステージ名の文字色（凡例の種類名と同じ色）
        public Sprite icon;
    }

    [Header("マスの情報")]
    [SerializeField] private TextMeshProUGUI typeTagText;
    [SerializeField] private Image diamondImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("主な報酬（報酬が無いマスでは非表示）")]
    [SerializeField] private GameObject rewardGroup;
    [SerializeField] private TextMeshProUGUI rewardText;

    [Header("Startボタン")]
    [SerializeField] private Button startButton;

    [Header("マスの種類ごとの表示設定")]
    [SerializeField] private List<StageTypeDisplay> stageTypeDisplays;

    private System.Action onStart;

    private void Awake()
    {
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() => onStart?.Invoke());
    }

    public void SetOnStart(System.Action onStartClicked)
    {
        onStart = onStartClicked;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// マスの情報を表示する。今は進めないマスの場合は canStart=false で Start を押せなくする
    /// </summary>
    public void Show(NodeData nodeData, RoguelikeGameState state, bool canStart)
    {
        StageTypeDisplay display = stageTypeDisplays.Find(d => d.stageType == nodeData.stageType);
        if (display == null)
        {
            Debug.LogWarning($"StageInfoPanelに{nodeData.stageType}の表示設定がありません");
            display = new StageTypeDisplay { stageType = nodeData.stageType };
        }

        gameObject.SetActive(true);

        typeTagText.text = display.tagText;
        diamondImage.color = display.diamondColor;
        iconImage.sprite = display.icon;
        iconImage.enabled = display.icon != null;
        stageNameText.text = display.stageName;
        stageNameText.color = display.stageNameColor;
        descriptionText.text = display.description;

        List<string> rewards = GetRewardLines(nodeData, state);
        rewardGroup.SetActive(rewards.Count > 0);
        rewardText.text = string.Join("\n", rewards);

        startButton.interactable = canStart;
    }

    // 各UIで実際に付与される内容と同じ計算で、獲得できる報酬を並べる
    private List<string> GetRewardLines(NodeData nodeData, RoguelikeGameState state)
    {
        List<string> lines = new List<string>();

        switch (nodeData.stageType)
        {
            case StageType.NORMAL_BATTLE:
            case StageType.ELITE_BATTLE:
            case StageType.BOSS_BATTLE:
                AddBattleRewardLines(nodeData.rewardData, lines);
                break;

            case StageType.TREASURE:
                AddTreasureRewardLines(nodeData.treasureData, lines);
                break;

            case StageType.REST:
                AddRestRewardLines(nodeData.restData, state, lines);
                break;
        }
        return lines;
    }

    private void AddBattleRewardLines(RewardData data, List<string> lines)
    {
        if (data == null) return;

        if (data.hasGoldReward)
        {
            if (data.isRandomGold)
            {
                // BattleRewardUIはRandom.Range(int, int)なのでgoldMaxちょうどは出ない
                int max = Mathf.Max(data.goldMin, data.goldMax - 1);
                lines.Add(max > data.goldMin ? $"G +{data.goldMin}〜{max}" : $"G +{data.goldMin}");
            }
            else if (data.goldFixed > 0)
            {
                lines.Add($"G +{data.goldFixed}");
            }
        }

        if (data.hasCardReward && data.rewardCardPool != null)
        {
            int choiceCount = Mathf.Min(data.cardChoiceCount, data.rewardCardPool.Count);
            int selectCount = Mathf.Min(data.cardSelectCount, choiceCount);
            if (selectCount > 0)
                lines.Add($"カード {choiceCount}枚から{selectCount}枚選択");
        }
    }

    private void AddTreasureRewardLines(TreasureData data, List<string> lines)
    {
        if (data == null) return;

        if (data.goldAmount > 0)
            lines.Add($"G +{data.goldAmount}");

        if (data.treasureCardPool != null && data.treasureCardPool.Count > 0)
        {
            // TreasureUIと同じく、cardCountが0以下ならプールを全て獲得
            int cardCount = data.cardCount <= 0
                ? data.treasureCardPool.Count
                : Mathf.Min(data.cardCount, data.treasureCardPool.Count);
            lines.Add($"カード {cardCount}枚");
        }
    }

    private void AddRestRewardLines(RestData data, RoguelikeGameState state, List<string> lines)
    {
        if (data == null) return;

        // RestUIと同じ計算。割合回復は現在の最大HPから回復量を出す
        int heal = data.isPercentageHeal
            ? Mathf.FloorToInt(state.MaxHP * data.healPercentage / 100f)
            : data.healAmount;

        if (heal > 0)
            lines.Add($"HP +{heal}");
    }
}
