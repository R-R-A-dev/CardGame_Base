using System.Collections.Generic;

[System.Serializable]
public class CardFilterSettings
{
    public List<int> costFilter = new List<int>();
    public List<int> attackFilter = new List<int>();
    public List<int> hpFilter = new List<int>();
    public List<RARE> rarityFilter = new List<RARE>();

    // 分類（フォロワー or スペル）→ 0: Follower, 1: Spell
    public List<int> categoryFilter = new List<int>();

    public void Clear()
    {
        costFilter.Clear();
        attackFilter.Clear();
        hpFilter.Clear();
        rarityFilter.Clear();
        categoryFilter.Clear();
    }

    /// <summary>
    /// 1枚のカードがフィルター条件に一致するか判定
    /// </summary>
    public bool Matches(CardEntity entity)
    {
        // コスト・攻撃・HP・レア度の一致チェック
        if (costFilter.Count > 0 && !costFilter.Contains(entity.cost)) return false;
        if (attackFilter.Count > 0 && !attackFilter.Contains(entity.at)) return false;
        if (hpFilter.Count > 0 && !hpFilter.Contains(entity.hp)) return false;
        if (rarityFilter.Count > 0 && !rarityFilter.Contains(entity.rare)) return false;

        // 分類（Follower / Spell）
        bool isSpell = entity.spells != SPELLS.NONE;
        int categoryIndex = isSpell ? 1 : 0; // 0=Follower, 1=Spell
        if (categoryFilter.Count > 0 && !categoryFilter.Contains(categoryIndex)) return false;

        return true;
    }
}
