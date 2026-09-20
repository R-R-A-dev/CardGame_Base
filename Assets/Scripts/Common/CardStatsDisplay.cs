using TMPro;

/// <summary>
/// スペルカードは攻撃力・体力を持たないため、カードを表示する画面で共通して隠すためのヘルパー。
/// </summary>
public static class CardStatsDisplay
{
    public static bool IsSpell(SPELLS spells)
    {
        return spells != SPELLS.NONE;
    }

    /// <summary>
    /// スペルなら攻撃力・体力のテキストを非表示にする。フォロワーなら表示する。
    /// </summary>
    public static void SetActiveStatsText(SPELLS spells, params TextMeshProUGUI[] statsTexts)
    {
        bool show = !IsSpell(spells);
        foreach (TextMeshProUGUI statsText in statsTexts)
        {
            if (statsText != null) statsText.gameObject.SetActive(show);
        }
    }
}
