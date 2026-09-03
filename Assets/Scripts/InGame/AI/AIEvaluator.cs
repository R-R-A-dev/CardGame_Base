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

    // このターン、プレイヤーのヒーローを打点で倒し切れるかどうか（段階3）。
    // AI.cs のターン開始直後（canAttack が立った直後）と、攻撃フェーズ直前の2回計算する
    // （速攻(INIT_ATTACKABLE)持ちを召喚した場合、召喚前の1回目だけでは打点に数えられないため）。
    public static bool isLethalTurn;

    // 到達可能打点（盤面の攻撃打点 + includeHandBurnがtrueなら手札のバーンスペルのeffectDmg）
    // を計算し、isLethalTurn を更新する。
    // includeHandBurn: true  … ターン開始直後用。これから撃つバーンスペルの分も見積もりに含める
    //                  false … 攻撃フェーズ直前用。召喚フェーズが終わった後はもうスペルを
    //                          撃てないため、手札のバーンを含めると過大評価になる
    // ★呼び出すたびに必ず false へリセットしてから再計算する
    //   （リセットを忘れると、一度リーサルが成立した後は永久にヒーローだけを
    //   殴り続けるAIになってしまうため）。
    public static void CalculateLethalTurn(bool includeHandBurn)
    {
        isLethalTurn = false;

        // 盤面の攻撃打点。相手に守護がいれば貫通持ちの分しか通らない。
        CardController[] oppField = OppField();
        bool oppHasShield = Array.Exists(oppField, c => c.model.abilities.HasFlag(ABILITIES.SHIELD));

        int boardDamage = 0;
        foreach (CardController c in SelfField())
        {
            if (!c.model.canAttack) continue;
            // DOUBLE_ACTIONの2回目は数えない（安全側の見積もり）
            if (oppHasShield && !c.model.abilities.HasFlag(ABILITIES.PIERCE)) continue;
            boardDamage += c.model.at;
        }

        // 手札のバーンスペル（マナ内で撃てる分だけ、貪欲に加算）
        int burnDamage = 0;
        if (includeHandBurn)
        {
            int remainingMana = GameManager.instance.enemy.manaCost;
            foreach (CardController c in SelfHand())
            {
                if (!c.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO)) continue;
                if (c.model.cost > remainingMana) continue;

                burnDamage += c.model.effectDmg;
                remainingMana -= c.model.cost;
            }
        }

        int totalDamage = boardDamage + burnDamage;
        isLethalTurn = totalDamage >= GameManager.instance.player.heroHp;
    }

    // スペルが実際に効果を発揮できる対象を持っているかどうか（段階4-C）。
    // CardController.CanUseSpells() は GetEnemyFieldCards(...) をそのまま使うため、
    // 破壊演出中（hp==0/isAlive==false）のカードも対象として数えてしまい、
    // Select系（Alive()済みのSelfField()/OppField()を使う）とズレが生じる。
    // そのズレを埋めるため、AI.cs側でCanUseSpells()とのANDとしてこちらを使う。
    // ★対象が必要な効果が1つでも成立しないなら false を返す。
    //   EFFECT_SELECTION_ENEMY/FRIENDと組み合わさっているかどうかは区別しない
    //   （どちらの場合もカード自体を出さない仕様）。
    public static bool HasValidSpellTarget(CardController card)
    {
        SPELLS spells = card.model.spells;

        if (spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARD) || spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS) ||
            spells.HasFlag(SPELLS.DESTROY_ENEMY_CARD) || spells.HasFlag(SPELLS.CONDITIONAL_ENEMY_DEBUFF) ||
            spells.HasFlag(SPELLS.STEAL_ENEMY_CARD) || spells.HasFlag(SPELLS.RANDOM_ENEMY) ||
            spells.HasFlag(SPELLS.SWAP_HP_ATK))
        {
            if (OppField().Length == 0) return false;
        }

        if (spells.HasFlag(SPELLS.HEAL_FRIEND_CARD) || spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS) ||
            spells.HasFlag(SPELLS.CONDITIONAL_FRIEND_BUFF) || spells.HasFlag(SPELLS.RANDOM_FRIEND))
        {
            if (SelfField().Length == 0) return false;
        }

        if (spells.HasFlag(SPELLS.DESTROY_ALL_FIELD_CARDS))
        {
            if (OppField().Length == 0 && SelfField().Length == 0) return false;
        }

        if (spells.HasFlag(SPELLS.INCREASE_ENEMY_COST) || spells.HasFlag(SPELLS.DISCARD_ENEMY_HAND) ||
            spells.HasFlag(SPELLS.DISCARD_ALL_ENEMY_HAND))
        {
            if (OppHand().Length == 0) return false;
        }

        if (spells.HasFlag(SPELLS.REDUCE_HAND_COST) || spells.HasFlag(SPELLS.DISCARD_FRIEND_HAND) ||
            spells.HasFlag(SPELLS.DISCARD_ALL_FRIEND_HAND))
        {
            // 自手札は Alive() を通さない（スペルは hp==0 のため消えてしまう）。
            // CastSpellOf が hand.Length - 1 で自分自身を除外しているのに合わせ、
            // ここでも自分自身を除いて1枚以上あるかを数える。
            int othersInHand = Array.FindAll(SelfHand(), c => c != card).Length;
            if (othersInHand == 0) return false;
        }

        if (spells.HasFlag(SPELLS.STEAL_ENEMY_CARD) || spells.HasFlag(SPELLS.SUMMON_SPECIFIC_UNIT))
        {
            if (SelfField().Length > 4) return false;
        }

        return true;
    }

    // ---- スペル/アビリティの効果対象選択（段階4） ----
    // 対象が0件のときは必ず null を返す。呼び出し側（AI.cs）は null を効果不発として扱う。

    // ダメージ系（DAMAGE_ENEMY_CARD / CONDITIONAL_ENEMY_DEBUFF）の対象を選ぶ。
    // card.model.effectDmg で倒せる相手がいればその中でThreat最大、いなければ全体でThreat最大。
    public static CardController SelectDamageTarget(CardController card)
    {
        CardController[] candidates = OppField();
        if (candidates.Length == 0) return null;

        CardController best = null;
        float bestThreat = float.NegativeInfinity;
        foreach (CardController c in candidates)
        {
            if (card.model.effectDmg < c.model.hp) continue;
            float t = Threat(c);
            if (t > bestThreat)
            {
                bestThreat = t;
                best = c;
            }
        }
        if (best != null) return best;

        // 倒せる相手がいなければThreat最大
        foreach (CardController c in candidates)
        {
            float t = Threat(c);
            if (t > bestThreat)
            {
                bestThreat = t;
                best = c;
            }
        }
        return best;
    }

    // 除去/奪取系（DESTROY_ENEMY_CARD / STEAL_ENEMY_CARD）の対象を選ぶ。Threat最大。
    public static CardController SelectDestroyTarget(CardController card)
    {
        CardController[] candidates = OppField();
        if (candidates.Length == 0) return null;

        CardController best = null;
        float bestThreat = float.NegativeInfinity;
        foreach (CardController c in candidates)
        {
            float t = Threat(c);
            if (t > bestThreat)
            {
                bestThreat = t;
                best = c;
            }
        }
        return best;
    }

    // 回復系（HEAL_FRIEND_CARD）の対象を選ぶ。
    // 段階6でmaxHpを追加するまでは暫定的にhpが最小の味方を返す。
    public static CardController SelectHealTarget(CardController card)
    {
        CardController[] candidates = SelfField();
        if (candidates.Length == 0) return null;

        CardController best = null;
        int bestHp = int.MaxValue;
        foreach (CardController c in candidates)
        {
            if (c.model.hp < bestHp)
            {
                bestHp = c.model.hp;
                best = c;
            }
        }
        return best;
    }

    // バフ系（CONDITIONAL_FRIEND_BUFF）の対象を選ぶ。
    // canAttack==trueの味方のうちhp最大（生き残りやすい＝バフが無駄にならない）。
    public static CardController SelectBuffTarget(CardController card)
    {
        CardController[] candidates = Array.FindAll(SelfField(), c => c.model.canAttack);
        if (candidates.Length == 0) return null;

        CardController best = null;
        int bestHp = int.MinValue;
        foreach (CardController c in candidates)
        {
            if (c.model.hp > bestHp)
            {
                bestHp = c.model.hp;
                best = c;
            }
        }
        return best;
    }

    // カードを攻撃対象にした場合のスコア。
    // 判定にはランタイムの isDestroyer / isDamageNullifyOnce を使う
    // （abilitiesは静的フラグのままで、isDamageNullifyOnceは1回使うと消費されて false に戻るため）。
    //
    // CardController.cs側の仕様：DAMAGE_NULLIFY_ONCE未消費の相手への攻撃は、
    // 破壊者(DESTROY_ATTACKED_TARGET)の攻撃も含めて「1回の攻撃イベント」として
    // 丸ごと無効化される（Destroys()の破壊試行が無効化を消費した後、
    // 通常ダメージの重複適用はスキップされるよう修正済み）。
    // そのため kill/survive の判定はどちらも「相手の無効化が未消費なら結果は確定で不発」に単純化される。
    static float ScoreAgainstCard(CardController attacker, CardController defender)
    {
        // defenderの無効化が未消費なら、attackerの攻撃（破壊者でも）はダメージが1も通らない
        bool zeroDamage = defender.model.isDamageNullifyOnce;

        bool kill = !zeroDamage && (attacker.model.isDestroyer || attacker.model.at >= defender.model.hp);

        bool survive;
        if (attacker.model.isDamageNullifyOnce)
        {
            // attacker自身の無効化が未消費なら、defenderの反撃（破壊者でも）は丸ごと無効化され必ず生存する
            survive = true;
        }
        else if (defender.model.isDestroyer)
        {
            // 無効化はもう無いので、破壊者の反撃は必ず通る
            survive = false;
        }
        else
        {
            survive = defender.model.at < attacker.model.hp;
        }

        if (kill && survive) return 100f + Threat(defender);
        if (kill && !survive) return 50f + Threat(defender) - Threat(attacker);
        if (!kill && survive && zeroDamage) return -10f;
        if (!kill && survive) return 10f + attacker.model.at;
        return -50f;
    }

    // ヒーローを攻撃対象にした場合のスコア
    // boardAdvantage は呼び出し側で1回だけ計算した値を受け取る（attackerごとに再計算しない）
    static float ScoreAgainstHero(CardController attacker, float boardAdvantage)
    {
        // リーサルターンは他のどんな選択肢よりヒーロー攻撃を優先させる。
        // NextAttack()側の「守護がいてPIERCEを持たない場合はヒーローを候補に入れない」判定は
        // ここより前段で行われるため、リーサルでも守護を無視することはない。
        if (isLethalTurn) return attacker.model.at * 1000f;

        float faceWeight = 1.0f;
        if (GameManager.instance.player.heroHp <= 5) faceWeight += 1.5f;
        if (boardAdvantage > 0f) faceWeight += 0.8f;

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
        float boardAdvantage = BoardAdvantage(); // attackerごとに再計算しない

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
                float score = ScoreAgainstHero(attacker, boardAdvantage);
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
