using System;

// 敵AIの盤面評価・攻撃対象選択を担当する純粋ロジッククラス。
// MonoBehaviour / ScriptableObject ではないため .asset の作成やInspector割り当ては不要。
public static class AIEvaluator
{
    // ---- AI視点固定のラッパー ----
    // 敵AIのカードは isPlayerCard == false。
    // GetEnemyFieldCards / GetFriendFieldCards の引数は「返るカードの持ち主」ではなく
    // 「問い合わせ側の視点」なので、AIEvaluator内では必ずこのラッパー経由で呼び出すこと。
    public static CardController[] SelfField() => Alive(GameManager.instance.GetFriendFieldCards(false));
    public static CardController[] OppField() => Alive(GameManager.instance.GetEnemyFieldCards(false));
    public static CardController[] SelfHand() => GameManager.instance.GetFriendHandTransform(false);
    public static CardController[] OppHand() => GameManager.instance.GetEnemyHandTransform(false);

    // 破壊演出中（Destroy()予約済みだが未破棄）のカードを除外する。
    // ※ 手札には使わないこと。スペルカードは hp=0 のため isAlive/hp>0 判定に引っかかり、
    //   手札に適用すると敵がスペルを一切使えなくなる（CardView.cs でスペルは hp/at 非表示の仕様）。
    public static CardController[] Alive(CardController[] source)
    {
        return Array.FindAll(source, c => c != null && c.model != null && c.model.isAlive && c.model.hp > 0);
    }

    // カード1体の脅威度スコア
    public static float Threat(CardController c)
    {
        if (c == null || c.model == null) return 0f;

        float score = c.model.at * 1.2f + c.model.hp * 0.8f;
        ABILITIES abilities = c.model.abilities;

        if (abilities.HasFlag(ABILITIES.SHIELD)) score += 3f;
        if (abilities.HasFlag(ABILITIES.DESTROY_ATTACKED_TARGET)) score += 4f;
        if (abilities.HasFlag(ABILITIES.DOUBLE_ACTION)) score += c.model.at;
        if (abilities.HasFlag(ABILITIES.DAMAGE_NULLIFY_ONCE)) score += 2f;
        if (abilities.HasFlag(ABILITIES.STATS_UP_ON_ATTACK)) score += 2f;
        if (abilities.HasFlag(ABILITIES.PIERCE)) score += 1f;
        if (abilities.HasFlag(ABILITIES.HEAL_BY_DAMAGE)) score += 1f;

        return score;
    }

    // 自陣と相手陣の脅威度差（プラスなら自陣有利）
    public static float BoardAdvantage()
    {
        float self = 0f;
        foreach (CardController c in SelfField()) self += Threat(c);

        float opp = 0f;
        foreach (CardController c in OppField()) opp += Threat(c);

        return self - opp;
    }

    // カードを攻撃対象にした場合のスコア
    static float ScoreAgainstCard(CardController attacker, CardController defender)
    {
        bool kill = attacker.model.abilities.HasFlag(ABILITIES.DESTROY_ATTACKED_TARGET)
            || attacker.model.at >= defender.model.hp;
        bool survive = attacker.model.abilities.HasFlag(ABILITIES.DAMAGE_NULLIFY_ONCE)
            || defender.model.at < attacker.model.hp;

        if (kill && survive) return 100f + Threat(defender);
        if (kill && !survive) return 50f + Threat(defender) - Threat(attacker);
        if (!kill && survive) return 10f + attacker.model.at;
        return -50f;
    }

    // ヒーローを攻撃対象にした場合のスコア
    static float ScoreAgainstHero(CardController attacker)
    {
        float faceWeight = 1.0f;
        if (GameManager.instance.player.heroHp <= 5) faceWeight += 1.5f;
        if (BoardAdvantage() > 0f) faceWeight += 0.8f;

        return attacker.model.at * faceWeight;
    }

    // 攻撃可能カードの中から (attacker, defender) の最良の組み合わせを選ぶ。
    // defender が null の場合はヒーローを攻撃する。
    // attackers は呼び出し側（AI.cs）で canAttack == true かつ生存中に絞り込み済みであること。
    public static AttackPlan NextAttack(CardController[] attackers)
    {
        if (attackers == null || attackers.Length == 0)
            return new AttackPlan { attacker = null, defender = null };

        CardController[] oppField = OppField();
        bool oppHasShield = Array.Exists(oppField, c => c.model.abilities.HasFlag(ABILITIES.SHIELD));

        CardController bestAttacker = null;
        CardController bestDefender = null;
        float bestScore = float.NegativeInfinity;
        bool found = false;

        foreach (CardController attacker in attackers)
        {
            // 攻撃候補のdefenderを組み立てる。
            // 相手盤面に守護がいて、かつ貫通を持たない場合は守護のみが対象になる（ヒーローは候補外）。
            bool blockedByShield = oppHasShield && !attacker.model.abilities.HasFlag(ABILITIES.PIERCE);
            CardController[] candidates = blockedByShield
                ? Array.FindAll(oppField, c => c.model.abilities.HasFlag(ABILITIES.SHIELD))
                : oppField;

            foreach (CardController defender in candidates)
            {
                float score = ScoreAgainstCard(attacker, defender);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAttacker = attacker;
                    bestDefender = defender;
                    found = true;
                }
            }

            // 守護に阻まれていない場合のみヒーローも候補に入れる
            if (!blockedByShield)
            {
                float score = ScoreAgainstHero(attacker);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAttacker = attacker;
                    bestDefender = null;
                    found = true;
                }
            }
        }

        if (!found)
        {
            // 通常は到達しない保険（相手盤面に守護がいれば候補は必ず1件以上、
            // 守護がいなければヒーローが常に候補になるため）。
            // 無限ループ防止のため、行動権だけ消費させて次のカードへ進める。
            CardController fallback = attackers[0];
            fallback.SetCanAttack(false);
            return new AttackPlan { attacker = fallback, defender = null };
        }

        return new AttackPlan { attacker = bestAttacker, defender = bestDefender };
    }
}

public struct AttackPlan
{
    public CardController attacker;
    public CardController defender;  // null ならヒーローへの攻撃
}
