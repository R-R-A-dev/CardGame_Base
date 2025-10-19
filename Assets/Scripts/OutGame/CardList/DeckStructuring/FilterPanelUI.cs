using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FilterPanelUI : MonoBehaviour
{
    [Header("DeckBuilderUI参照")]
    private DeckBuilderUI deckBuilderUI;
    private bool forDeck;

    [Header("トグル群")]
    public Toggle[] costToggles;
    public Button selectAllCostButton;
    public TextMeshProUGUI costButtonText;

    public Toggle[] attackToggles;
    public Button selectAllAttackButton;
    public TextMeshProUGUI attackButtonText;

    public Toggle[] hpToggles;
    public Button selectAllHPButton;
    public TextMeshProUGUI hpButtonText;

    [Header("分類トグル")]
    public Toggle[] categoryToggles; // 0 = Follower, 1 = Spell
    public Button selectAllCategoryButton;
    public TextMeshProUGUI categoryButtonText;

    public Toggle[] rarityToggles;
    public Button selectAllRarityButton;
    public TextMeshProUGUI rarityButtonText;

    [Header("操作ボタン")]
    public Button applyButton;
    public Button cancelButton;
    public Button resetButton;

    private CardFilterSettings currentFilter = new CardFilterSettings();

    /// <summary>
    /// 一覧かデッキのフィルター設定パネルを開く
    /// </summary>
    /// <param name="builderUI"></param>
    /// <param name="deckMode"></param>
    public void Open(DeckBuilderUI builderUI, bool deckMode, CardFilterSettings previousFilter)
    {
        deckBuilderUI = builderUI;
        forDeck = deckMode;

        currentFilter = CloneFilter(previousFilter);

        // UI に反映
        LoadFilterToUI(currentFilter);

        gameObject.SetActive(true);
        UpdateAllButtons();
    }

    void Start()
    {
        applyButton.onClick.AddListener(OnApply);
        cancelButton.onClick.AddListener(() => gameObject.SetActive(false));
        resetButton.onClick.AddListener(OnReset);

        SetupButton(selectAllCostButton, costToggles, costButtonText);
        SetupButton(selectAllAttackButton, attackToggles, attackButtonText);
        SetupButton(selectAllHPButton, hpToggles, hpButtonText);
        SetupButton(selectAllRarityButton, rarityToggles, rarityButtonText);
        SetupButton(selectAllCategoryButton, categoryToggles, categoryButtonText);
    }

    /// <summary>
    /// 全選択ボタンとトグルの連動設定
    /// </summary>
    /// <param name="button"></param>
    /// <param name="toggles"></param>
    /// <param name="text"></param>
    void SetupButton(Button button, Toggle[] toggles, TextMeshProUGUI text)
    {
        button.onClick.AddListener(() =>
        {
            bool anyOn = System.Array.Exists(toggles, t => t.isOn);
            foreach (var t in toggles) t.isOn = !anyOn;
            text.text = anyOn ? "All On" : "All Off";
        });

        foreach (var t in toggles)
            t.onValueChanged.AddListener(_ => UpdateAllButtons());
    }

    /// <summary>
    /// 全選択ボタンのテキスト全更新
    /// </summary>
    void UpdateAllButtons()
    {
        UpdateButtonText(costToggles, costButtonText);
        UpdateButtonText(attackToggles, attackButtonText);
        UpdateButtonText(hpToggles, hpButtonText);
        UpdateButtonText(rarityToggles, rarityButtonText);
        UpdateButtonText(categoryToggles, categoryButtonText);
    }

    /// <summary>
    /// 全選択ボタンのテキスト更新
    /// </summary>
    /// <param name="toggles"></param>
    /// <param name="text"></param>
    void UpdateButtonText(Toggle[] toggles, TextMeshProUGUI text)
    {
        bool anyOn = System.Array.Exists(toggles, t => t.isOn);
        text.text = anyOn ? "All Off" : "All On";
    }

    /// <summary>
    /// フィルター条件と UI のトグルを初期化する
    /// </summary>
    void OnReset()
    {
        currentFilter.Clear();
        foreach (var t in GetComponentsInChildren<Toggle>())
            t.isOn = false;
        UpdateAllButtons();
    }

    /// <summary>
    /// 決定ボタンを押下後の処理
    /// </summary>
    void OnApply()
    {
        currentFilter.Clear();

        for (int i = 0; i < costToggles.Length; i++)
            if (costToggles[i].isOn) currentFilter.costFilter.Add(i + 1);

        for (int i = 0; i < attackToggles.Length; i++)
            if (attackToggles[i].isOn) currentFilter.attackFilter.Add(i + 1);

        for (int i = 0; i < hpToggles.Length; i++)
            if (hpToggles[i].isOn) currentFilter.hpFilter.Add(i + 1);

        for (int i = 0; i < rarityToggles.Length; i++)
            if (rarityToggles[i].isOn)
                currentFilter.rarityFilter.Add((RARE)i);

        for (int i = 0; i < categoryToggles.Length; i++)
            if (categoryToggles[i].isOn)
                currentFilter.categoryFilter.Add(i); // 0:Follower, 1:Spell

        deckBuilderUI.ApplyFilter(currentFilter, forDeck);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// フィルター内容 → UIへ反映
    /// </summary>
    /// <param name="filter"></param>
    void LoadFilterToUI(CardFilterSettings filter)
    {
        void SetToggles(Toggle[] toggles, int count, Func<int, bool> condition)
        {
            for (int i = 0; i < toggles.Length; i++)
                toggles[i].isOn = condition(i);
        }

        SetToggles(costToggles, costToggles.Length, i => filter.costFilter.Contains(i + 1));
        SetToggles(attackToggles, attackToggles.Length, i => filter.attackFilter.Contains(i + 1));
        SetToggles(hpToggles, hpToggles.Length, i => filter.hpFilter.Contains(i + 1));
        SetToggles(rarityToggles, rarityToggles.Length, i => filter.rarityFilter.Contains((RARE)i));
        SetToggles(categoryToggles, categoryToggles.Length, i => filter.categoryFilter.Contains(i));
    }

    /// <summary>
    /// フィルター複製（参照共有を防ぐ） ======
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    CardFilterSettings CloneFilter(CardFilterSettings src)
    {
        var copy = new CardFilterSettings();
        copy.costFilter.AddRange(src.costFilter);
        copy.attackFilter.AddRange(src.attackFilter);
        copy.hpFilter.AddRange(src.hpFilter);
        copy.rarityFilter.AddRange(src.rarityFilter);
        copy.categoryFilter.AddRange(src.categoryFilter);
        return copy;
    }
}
