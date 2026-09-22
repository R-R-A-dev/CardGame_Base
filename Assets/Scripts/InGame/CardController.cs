using Coffee.UIExtensions;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.GPUSort;

public class CardController : MonoBehaviour
{
    public CardView view;          // 見かけ(view)に関することを操作
    public CardModel model;        // データ(model)に関することを操作
    public CardMovement movement;  // 移動(movement)に関することを操作
    public EffectController effect;// エフェクト(effect)に関することを操作

    [SerializeField] public AudioSource audioSource;
    GameManager gameManager;

    // 破壊(Destroys)は同期でダメージを確定させるが、数字を出すのは攻撃エフェクトの着弾後になる。
    // その時点では相手のダメージ無効が消費済みで判別できないため、確定時の結果をここに控えておく。
    bool isLastDestroyNullified;

    public bool IsAbilities
    {
        get { return model.abilities != ABILITIES.NONE; }
    }

    public bool IsSpell
    {
        get { return model.spells != SPELLS.NONE; }
    }
    private void Awake()
    {
        view = GetComponent<CardView>();
        movement = GetComponent<CardMovement>();
        effect = GetComponent<EffectController>();
        gameManager = GameManager.instance;
    }

    public void Init(int cardID, bool isPlayer)
    {
        model = new CardModel(cardID, isPlayer);
        ApplyRoguelike(model);
        // ApplyRoguelikeがhpを書き換える場合があるため、その後にmaxHpを追従させる
        // （AIの評価にのみ使う参照値。段階6）
        model.maxHp = model.hp;
        view.SetCard(model);
    }

    void ApplyRoguelike(CardModel model)
    {
        if (RoguelikeSession.BattleModifiers == null ||
            RoguelikeSession.BattleModifiers.Count == 0) return;

        for (int i = 0; i < RoguelikeSession.BattleModifiers.Count; i++)
        {
            ParameterModifier modifier = RoguelikeSession.BattleModifiers[i];
            switch (modifier.modifierType)
            {
                case ParameterModifierType.MANA_REDUCTION:
                    model.cost -= modifier.value;
                    if(model.cost<1)
                        model.cost = 0;
                    break;

                case ParameterModifierType.MANA_BOOST:
                    model.cost += modifier.value;
                    break;

                case ParameterModifierType.ATTACK_BOOST:
                    model.at += modifier.value;
                    break;

                case ParameterModifierType.ATTACK_REDUCTION:
                    model.at -= modifier.value;
                    if(model.at<1)
                        model.at = 0;
                    break;

                case ParameterModifierType.DEFENSE_BOOST:
                    model.hp += modifier.value;
                    break;

                case ParameterModifierType.DEFENSE_REDUCTION:
                    model.hp -= modifier.value;
                    if (model.hp < 1)
                        model.hp = 1;
                    break;
            }
        }
    }

    public void EffectCardInit(CardModel effectModel)
    {
        //modelにパラメータを代入してviewにセット
        model = effectModel;
        view.SetCard(effectModel);
    }


    public void Attack(CardController enemyCard)
    {
        if (!model.isAlive) return;
        if (model.isDestroyer)
        {
            attackEffect(enemyCard, false);
            Destroys(enemyCard);
        }
        else
        {
            attackEffect(enemyCard, false);
            //model.Attack(enemyCard);
        }
        if (model.abilities.HasFlag(ABILITIES.HEAL_BY_DAMAGE))
        {
            Heal(this);
        }
        if (model.isDoubleAction)
        {
            if (!model.isSingleAction)
            {
                SetCanAttack(true);
                model.isSingleAction = true;
            }
            else
            {
                SetCanAttack(false);
                model.isSingleAction = false;
            }
        }
        else
        {
            SetCanAttack(false);
        }
        if (model.isStatsUpOnAttack)
        {
            model.at += model.effectDmg;
            model.hp += model.effectHeal;
            RefreshView();
        }
    }

    public void AttackHeroSpell(Transform enemyHero)
    {
        attackSpellEffectHero(enemyHero, true);
    }
    public void AttackHero(Transform enemyHero)
    {
        if (!model.isAlive) return;


        attackEffectHero(enemyHero, true);
        //model.Attack(enemyCard);

        if (model.abilities.HasFlag(ABILITIES.HEAL_BY_DAMAGE) || model.spells.HasFlag(SPELLS.HEAL_BY_DAMAGE))
        {
            Heal(this);
        }
        if (model.isDoubleAction)
        {
            if (!model.isSingleAction)
            {
                SetCanAttack(true);
                model.isSingleAction = true;
            }
            else
            {
                SetCanAttack(false);
                model.isSingleAction = false;
            }
        }
        else
        {
            SetCanAttack(false);
        }
        if (model.isStatsUpOnAttack)
        {
            model.at += model.effectDmg;
            model.hp += model.effectHeal;
            RefreshView();
        }
    }

    public void Defense(CardController enemyCard)
    {
        if (model.isDestroyer)
            Destroys(enemyCard);

        attackEffect(enemyCard, true);
        //model.Attack(enemyCard);
        SetCanAttack(false);
    }

    public void Destroys(CardController enemyCard)
    {
        isLastDestroyNullified = enemyCard != null && enemyCard.model.isDamageNullifyOnce;
        model.Destroy(enemyCard);
    }

    /// <summary>
    /// 攻撃ヒット時に表示するダメージ量を返す。
    /// ダメージ無効(DAMAGE_NULLIFY_ONCE)が残っている相手にはダメージが通らないので0を表示する。
    /// CardModel.Damage()を通すとフラグが消費されて判別できなくなるため、
    /// 必ずダメージを適用する前に呼ぶこと。
    /// </summary>
    int GetAttackDamageText(CardController target)
    {
        // 破壊者(isDestroyer)はDestroys()で既に無効化を消費しているので、その時点の結果を使う
        if (model.isDestroyer)
            return isLastDestroyNullified ? 0 : model.at;

        if (target != null && target.model.isDamageNullifyOnce)
            return 0;

        return model.at;
    }

    public void EffectAttack(CardController enemyCard)
    {
        if (enemyCard == null) return;

        // ダメージ無効(DAMAGE_NULLIFY_ONCE)に吸収されたかどうかは、
        // Damage()を通した後では区別できないため先に控えておく。
        bool isNullified = enemyCard.model.isDamageNullifyOnce;

        model.EffectDmg(enemyCard);
        enemyCard.RefreshView();

        // 吸収された場合は「0」を出して、当たったがダメージが通っていないことを伝える。
        // effectDmgが0の効果と違い、この0は必ず表示させたいのでforceShowで通す。
        if (isNullified)
            ShowEffectText(enemyCard.transform, 0, false, true);
        else
            ShowEffectText(enemyCard.transform, model.effectDmg, false);
    }

    public void Steal(CardController target)
    {
        if (target == null) return;

        model.Steal(target);

        // 親を付け替えただけでは奪った側の盤面のカードとして扱われない。
        // defaultParentが元の盤面のままだとドラッグ操作で元の場所へ戻ってしまうため、
        // 移動先のフィールドに合わせ直す。
        target.movement.defaultParent = target.transform.parent;
        target.movement.isHand = false;
        // 奪ったターンは攻撃させない（召喚酔いと同じ扱い）。
        // 攻撃可能表示も消す必要があるのでSetCanAttack()を通すこと。
        target.SetCanAttack(false);
        target.RefreshShieldPanel();
        target.RefreshView();
    }


    public void DrawCard(CardController card)
    {
        BattleAudioManager.Instance.PlaySE("CardCatch");
        for (int i = 0; i < card.model.effectDmg; i++)
        {
            GameManager.instance.DrawCard(model.isPlayerCard);
        }
        if (model.spells.HasFlag(SPELLS.DRAW_CARDS))
        {
            Destroy(card.gameObject);
            GameManager.instance.isAttacking = false;
        }
        else if (model.abilities.HasFlag(ABILITIES.DRAW_CARDS))
        {
            GameManager.instance.isAttacking = false;
        }

    }

    public void DiscardEnemyHandAllCard()
    {
        CardController[] cards = GameManager.instance.GetEnemyHandTransform(model.isPlayerCard);

        //cardsをすべて破棄する
        foreach (CardController card in cards)
        {
            Destroy(card.gameObject);
        }
    }

    public void DiscardPlayerHandAllCard()
    {
        CardController[] cards = GameManager.instance.GetFriendHandTransform(model.isPlayerCard);
        foreach (CardController card in cards)
        {
            Destroy(card.gameObject);
        }
    }

    public void DiscardEnemyHandCard(CardController card)
    {
        Destroy(card.gameObject);
    }
    public void DiscardPlayerHandCard(CardController card)
    {
        Destroy(card.gameObject);
    }


    public void CardToHand(CardController card)
    {
        Transform hand = gameManager.GetFriendHandFieldTransform(card.model.isPlayerCard);
        CardModel[] targetCard = card.model.targetCards;
        model.CardToHand(targetCard, hand, card.model.isPlayerCard);
    }

    public void SummonCard(CardController card)
    {
        Transform hand = gameManager.GetFriendFieldTransform(card.model.isPlayerCard);
        CardModel[] targetCard = card.model.targetCards;
        model.SummonCard(targetCard, hand, card.model.isPlayerCard, card);
    }

    public void EffectHeal(CardController friendCard)
    {
        if (friendCard == null) return;

        model.EffectHeal(friendCard);
        friendCard.RefreshView();
        ShowEffectText(friendCard.transform, model.effectHeal, true);
    }

    /// <summary>
    /// 効果ダメージ／回復の数字を対象の上にポップさせる。
    /// アビリティ・スペル、単体／全体、ATTACKTYPEの違いに関係なく
    /// 「実際に値を適用する場所」から呼ぶことで、表示漏れと二重表示を防いでいる。
    /// </summary>
    void ShowEffectText(Transform target, int amount, bool isHeal, bool forceShow = false)
    {
        if (target == null) return;
        // 効果量0のアビリティ・スペルで数字が出ないようにする。
        // ダメージ無効で0になったケースだけはforceShowで明示的に表示する。
        if (amount == 0 && !forceShow) return;

        // スペルカードはUseSpellTo()の最後に自分自身をDestroyするため、
        // このカードでStartCoroutineすると数字が出る前にコルーチンが止まる。
        // 必ずGameManager側で回すこと。
        // 座標も同じ理由でここで確定させる（対象が破壊されるケース対策）。
        Vector3 position = target.position;
        GameObject textObj = GameManager.instance.GetTextPool();
        if (isHeal)
            GameManager.instance.StartCoroutine(GameManager.instance.GenHealText(textObj, amount, position));
        else
            GameManager.instance.StartCoroutine(GameManager.instance.GenDamageText(textObj, amount, position));
    }

    public void Heal(CardController friendCard)
    {
        if (friendCard == null) return;

        // HEAL_BY_DAMAGE（攻撃した分回復）はmodel.at分の回復になる。
        // 他の回復と同じく「実際に値を適用する場所」で緑の数字を出す。
        model.Heal(friendCard);
        friendCard.RefreshView();
        ShowEffectText(friendCard.transform, model.at, true);
    }

    public void Show()
    {
        view.Show();
    }

    public void ReduceHandCost(CardController card)
    {
        //CardController[] handCards = gameManager.GetFriendHandTransform(card.model.isPlayerCard);
        //foreach (CardController handCard in handCards)
        //{

        //}
        model.ReduceHandCost(card);
        card.RefreshView();

    }

    public void IncreaseEnemyHandCost(CardController card)
    {
        //CardController[] enemyHandCards = gameManager.GetEnemyHandTransform(card.model.isPlayerCard);
        //foreach (CardController enemyHandCard in enemyHandCards)
        //{

        //}
        model.IncreaseEnemyHandCost(card);
        card.RefreshView();
    }

    public void SwapHPToATK(CardController target)
    {
        int swap = target.model.at;
        target.model.at = target.model.hp;
        target.model.hp = swap;
    }

    public void AttackDebuff(CardController card, CardController target)
    {
        // ATKは1未満にならないようclampするので、effectDmgではなく
        // 実際に下がった分を数字として出す。
        int before = target.model.at;
        target.model.at -= card.model.effectDmg;
        if (target.model.at < 1)
        {
            target.model.at = 1;
        }
        int reduced = before - target.model.at;
        // 既にATKが1で下がらなかった場合も、効果が当たったことが分かるように0を出す
        // （ダメージ無効で0を出しているのと同じ扱い）。
        card.ShowEffectText(target.transform, reduced, false, card.model.effectDmg > 0);
    }
    public void AttackBuff(CardController card, CardController target)
    {
        target.model.at += card.model.effectDmg;
    }

    public void RefreshView()
    {
        view.Refresh(model);
    }

    /// <summary>
    /// 守護の表示を model.isFieldCard に合わせて更新する。
    /// 場に出た／手札に戻った直後に呼ぶ。
    /// </summary>
    public void RefreshShieldPanel()
    {
        view.RefreshShieldPanel(model);
    }

    /// <summary>
    /// ダメージ無効（一度だけ）の表示を model の状態に合わせて更新する。
    /// 場に出てフラグが立った直後に呼ぶ。消費時の非表示はRefreshView()経由で行われる。
    /// </summary>
    public void RefreshOneceNull()
    {
        view.RefreshOneceNull(model);
    }

    public void SetCanAttack(bool canAttack)
    {
        model.canAttack = canAttack;
        view.SetActiveSelectablePanel(canAttack);
    }

    public void OnFiled()
    {
        /*        if (!IsSpell && model.summonEffect != null && !model.isPlayerCard)
                {
                    summonEffect(model.summonEffect, transform);
                }*/
        //if (!model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY) ||
        //    !model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND))
        gameManager.ReduceManaCost(model.cost, model.isPlayerCard);

        model.isFieldCard = true;
        RefreshShieldPanel();
        OnFiledAbilities();
    }

    public void OnFiledAbilities()
    {
        SetAbility(this);
        if (gameManager.isPlayerTurn)
        {
            if (model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND) || model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY))
            {
                if (CanUseAbilities())
                {
                    gameManager.isEffectSelectPhase = true;
                    StartCoroutine(movement.PlayerSelectMoveOn());
                    gameManager.DisableButtonCards();
                }
                else
                {
                    movement.isHand = false;
                    StartCoroutine(movement.SummonMove(this, gameManager.playerFieldTransform));
                }
            }
            else if (CanUseAbilities())
            {
                //攻撃エフェクト
                StartCoroutine(ActAbility());
            }
        }
    }

    IEnumerator ActAbility()
    {
        GameManager.instance.isAttacking = true;
        yield return new WaitForSeconds(1.5f);
        CardController target = null;
        CardController[] targets = null;
        Transform movePosition = null;
        // DESTROY_ATTACKED_TARGET / DAMAGE_NULLIFY_ONCE / DOUBLE_ACTION / STATS_UP_ON_ATTACK など、
        // ターゲット選択や専用エフェクトを伴わない「常時パッシブ」系アビリティ単体の場合、
        // 以降のどの分岐にも該当しない。その場合に isAttacking を戻し忘れると
        // ドラッグ操作・ターン終了操作がフリーズしたままになるため、実際に効果を発動したかを追跡する。
        bool actionTaken = false;

        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARDS))
            targets = gameManager.GetEnemyFieldCards(model.isPlayerCard);

        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARDS))
            targets = gameManager.GetFriendFieldCardsExcept(model.isPlayerCard, this);

        // REDUCE_HAND_COSTは自分自身を対象に含めない（CanUseAbilities()の判定と揃える）
        if (model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST))
            targets = gameManager.GetFriendHandTransformExcept(model.isPlayerCard, this);
        else if (model.abilities.HasFlag(ABILITIES.DISCARD_ALL_FRIEND_HAND))
            targets = gameManager.GetFriendHandTransform(model.isPlayerCard);

        if (model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST) || model.abilities.HasFlag(ABILITIES.DISCARD_ALL_ENEMY_HAND))
            targets = gameManager.GetEnemyHandTransform(model.isPlayerCard);

        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO) || model.abilities.HasFlag(ABILITIES.HEAL_BY_DAMAGE))
        {
            movePosition = gameManager.enemyHero;
            attackSpellEffectHero(movePosition, true);
            actionTaken = true;
        }

        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO))
        {
            movePosition = gameManager.playerHero;
            attackSpellEffectHero(movePosition, true);
            actionTaken = true;
        }

        if (model.abilities.HasFlag(ABILITIES.DISCARD_ENEMY_HAND))
        {
            CardController[] enemyCards = gameManager.GetEnemyHandTransform(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                target = enemyCards[UnityEngine.Random.Range(0, enemyCards.Length)];
            }
        }

        if (model.abilities.HasFlag(ABILITIES.DISCARD_FRIEND_HAND))
        {
            CardController[] friendCards = gameManager.GetFriendHandTransform(this.model.isPlayerCard);
            if (friendCards.Length > 0)
            {
                target = friendCards[UnityEngine.Random.Range(0, friendCards.Length)];
            }
        }

        if (model.abilities.HasFlag(ABILITIES.RANDOM_ENEMY))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                target = enemyCards[UnityEngine.Random.Range(0, enemyCards.Length)];
            }
        }

        if (model.abilities.HasFlag(ABILITIES.RANDOM_FRIEND))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            if (friendCards.Length > 0)
            {
                target = friendCards[UnityEngine.Random.Range(0, friendCards.Length)];
            }
        }

        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARDS) || model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARDS) ||
            model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST) || model.abilities.HasFlag(ABILITIES.DISCARD_ALL_ENEMY_HAND) ||
            model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST) || model.abilities.HasFlag(ABILITIES.DISCARD_ALL_FRIEND_HAND))
        {
            // targetsが空（対象なし）ならAbilityEffectを一度も呼ばないので、
            // actionTakenもtrueにしない（末尾の保険でisAttackingを戻す）
            if (targets != null && targets.Length > 0)
            {
                DG.Tweening.Sequence seq = DOTween.Sequence();

                seq.Append(transform
                    .DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd)
                    .OnUpdate(() =>
                    {
                        float y = transform.localEulerAngles.y;
                        // Unityでは-90度が270度として表現されることがあるので360でmod取る
                        if (y >= 90 && y <= 270)
                        {
                            view.maskPanel.SetActive(true);  // 裏面
                        }
                        else
                        {
                            view.maskPanel.SetActive(false); // 表面
                        }
                    })
                );
                seq.Play();
                for (int i = 0; i < targets.Length; i++)
                {
                    AbilityEffect(targets[i], true);
                }
                actionTaken = true;
            }
        }
        else if (model.abilities.HasFlag(ABILITIES.RANDOM_ENEMY) || model.abilities.HasFlag(ABILITIES.RANDOM_FRIEND) ||
                 model.abilities.HasFlag(ABILITIES.DISCARD_ENEMY_HAND) || model.abilities.HasFlag(ABILITIES.DISCARD_FRIEND_HAND))
        {
            // targetがnull（対象なし）ならAbilityEffectを呼ばない（NRE防止・末尾の保険に委ねる）
            if (target != null)
            {
                DG.Tweening.Sequence seq = DOTween.Sequence();

                seq.Append(transform
                    .DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd)
                    .OnUpdate(() =>
                    {
                        float y = transform.localEulerAngles.y;
                        // Unityでは-90度が270度として表現されることがあるので360でmod取る
                        if (y >= 90 && y <= 270)
                        {
                            view.maskPanel.SetActive(true);  // 裏面
                        }
                        else
                        {
                            view.maskPanel.SetActive(false); // 表面
                        }
                    })
                );
                seq.Play();
                AbilityEffect(target, true);
                actionTaken = true;
            }
        }
        else if (model.abilities.HasFlag(ABILITIES.DRAW_CARDS) || model.abilities.HasFlag(ABILITIES.SUMMON_SPECIFIC_UNIT))
        {
            DG.Tweening.Sequence seq = DOTween.Sequence();

            seq.Append(transform
                .DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd)
                .OnUpdate(() =>
                {
                    float y = transform.localEulerAngles.y;
                    // Unityでは-90度が270度として表現されることがあるので360でmod取る
                    if (y >= 90 && y <= 270)
                    {
                        view.maskPanel.SetActive(true);  // 裏面
                    }
                    else
                    {
                        view.maskPanel.SetActive(false); // 表面
                    }
                })
            );
            seq.Play();
            UseAbilitiesTo(this);
            // DRAW_CARDSはDrawCard()内でisAttackingをfalseに戻すのでactionTaken扱いにする。
            // SUMMON_SPECIFIC_UNIT単体はisAttackingに触れる経路がないため、
            // actionTakenをtrueにせず末尾の保険に委ねる（true固定にすると保険が働かずフリーズする）
            if (model.abilities.HasFlag(ABILITIES.DRAW_CARDS))
            {
                actionTaken = true;
            }
        }

        if (!actionTaken)
        {
            // DESTROY_ATTACKED_TARGET / DAMAGE_NULLIFY_ONCE / DOUBLE_ACTION / STATS_UP_ON_ATTACK など、
            // 常時パッシブ系アビリティ単体の場合はここに来る。
            // これらは OnFiledAbilities() 内の SetAbility(this) で既にフラグ適用済みなので、
            // このメソッドでの追加処理は不要。isAttacking を明示的に戻さないと
            // ドラッグ操作・ターン終了操作がフリーズしたままになる。
            GameManager.instance.isAttacking = false;
        }
    }

    public void SetAbility(CardController card)
    {
        if (card.model.abilities.HasFlag(ABILITIES.DAMAGE_NULLIFY_ONCE))
        {
            card.model.isDamageNullifyOnce = true;
            card.RefreshOneceNull();
        }

        if (card.model.abilities.HasFlag(ABILITIES.DOUBLE_ACTION))
            card.model.isDoubleAction = true;

        if (card.model.abilities.HasFlag(ABILITIES.STATS_UP_ON_ATTACK))
            card.model.isStatsUpOnAttack = true;

        if (card.model.abilities.HasFlag(ABILITIES.DESTROY_ATTACKED_TARGET))
            card.model.isDestroyer = true;
    }

    public void UseAbilitiesTo(CardController target = null)
    {
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARD))
        {
            if (target == null)
            {
                return;
            }
            if (target.model.isPlayerCard == model.isPlayerCard)
            {
                return;
            }
            EffectAttack(target);
            StartCoroutine(target.CheckAlive());
        }
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARDS))
        {
            //CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            //foreach (CardController enemyCard in enemyCards)
            //{
            //    EffectAttack(enemyCard);
            //}
            //foreach (CardController enemyCard in enemyCards)
            //{
            //    enemyCard.CheckAlive();
            //}
            EffectAttack(target);
            StartCoroutine(target.CheckAlive());
        }
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO))
        {
            //gameManager.AttackToHeroSpell(this);
            //gameManager.CheckHeroHP();
        }
        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO))
        {
            gameManager.HealToHeroAbility(this);
        }
        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARD))
        {
            if (target == null)
            {
                return;
            }
            if (target.model.isPlayerCard != model.isPlayerCard)
            {
                return;
            }
            EffectHeal(target);
        }
        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARDS))
        {
            //CardController[] friendsCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            //if (friendsCards.Length > 0)
            //{
            //    foreach (CardController friendCard in friendsCards)
            //    {
            //        EffectHeal(friendCard);
            //    }
            //}
            EffectHeal(target);
        }
        if (model.abilities.HasFlag(ABILITIES.DESTROY_ENEMY_CARD))
        {
            if (target == null)
            {
                return;
            }
            if (target.model.isPlayerCard == model.isPlayerCard)
            {
                return;
            }
            Destroys(target);
            StartCoroutine(target.CheckAlive());
        }
        if (model.abilities.HasFlag(ABILITIES.STEAL_ENEMY_CARD))
        {
            if (target == null)
            {
                return;
            }
            if (target.model.isPlayerCard == model.isPlayerCard)
            {
                return;
            }
            Steal(target);
        }

        if (model.abilities.HasFlag(ABILITIES.DRAW_CARDS))
        {
            DrawCard(this);
        }
        if (model.abilities.HasFlag(ABILITIES.SEARCH_SPECIFIC_UNIT))
        {
            CardToHand(this);
        }
        if (model.abilities.HasFlag(ABILITIES.SUMMON_SPECIFIC_UNIT))
        {
            SummonCard(this);
        }
        if (model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST))
        {
            //CardController[] handCards = gameManager.GetFriendHandTransform(this.model.isPlayerCard);
            //if (handCards.Length > 0)
            //{

            //}
            ReduceHandCost(target);
        }
        if (model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST))
        {
            //CardController[] enemyHandCards = gameManager.GetEnemyHandTransform(this.model.isPlayerCard);
            //if (enemyHandCards.Length > 0)
            //{

            //}
            IncreaseEnemyHandCost(target);
        }
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_NULLIFY_ONCE))
        {
            model.isDamageNullifyOnce = true;
            RefreshOneceNull();
        }
        if (model.abilities.HasFlag(ABILITIES.DOUBLE_ACTION))
        {
            model.isDoubleAction = true;
        }
        if (model.abilities.HasFlag(ABILITIES.DISCARD_ALL_ENEMY_HAND))
        {
            //DiscardEnemyHandAllCard();
        }
        if (model.abilities.HasFlag(ABILITIES.DISCARD_ENEMY_HAND) || model.abilities.HasFlag(ABILITIES.DISCARD_ALL_ENEMY_HAND))
        {
            DiscardEnemyHandCard(target);
        }
        if (model.abilities.HasFlag(ABILITIES.DISCARD_ALL_FRIEND_HAND))
        {
            //DiscardPlayerHandAllCard();
        }
        if (model.abilities.HasFlag(ABILITIES.DISCARD_FRIEND_HAND) || model.abilities.HasFlag(ABILITIES.DISCARD_ALL_FRIEND_HAND))
        {
            DiscardPlayerHandCard(target);
        }
        if (model.abilities.HasFlag(ABILITIES.CONDITIONAL_ENEMY_DEBUFF))
        {
            if (target != null)
            {
                AttackDebuff(this, target);
                target.RefreshView();
            }
        }
        if (model.abilities.HasFlag(ABILITIES.STATS_UP_ON_ATTACK))
        {
            model.isStatsUpOnAttack = true;
        }
        if (model.abilities.HasFlag(ABILITIES.DESTROY_ATTACKED_TARGET))
        {
            model.isDestroyer = true;
        }
    }

    public bool CanUseAbilities()
    {
        bool canUse = false;
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARD) || model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARDS) ||
            model.abilities.HasFlag(ABILITIES.DESTROY_ENEMY_CARD) || model.abilities.HasFlag(ABILITIES.CONDITIONAL_ENEMY_DEBUFF))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                canUse = true;
            }
            else
            {
                return false;
            }

        }
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO) || model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO) || model.abilities.HasFlag(ABILITIES.DRAW_CARDS))
        {
            canUse = true;
        }
        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARD))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            if (friendCards.Length > 0)
            {
                canUse = true;
            }
            else
            {
                return false;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARDS))
        {
            // 自分自身は回復対象に含めないため、他に味方がいなければ発動できない
            CardController[] friendCards = gameManager.GetFriendFieldCardsExcept(this.model.isPlayerCard, this);
            if (friendCards.Length > 0)
            {
                canUse = true;
            }
            else
            {
                return false;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.STEAL_ENEMY_CARD))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            // アビリティ版は「このカード自身＋奪ったカード」で2体分の枠が要る。
            // かつ、判定を呼ぶタイミングが経路によって違う
            // （プレイヤーは効果対象選択前＝まだ場にいない／AIはOnFiled後＝既に場にいる）ため、
            // 自分自身を必ず除外した数で数え、盤面上限5から2体分を引いた3以下を条件にする。
            CardController[] friendCards = gameManager.GetFriendFieldCardsExcept(this.model.isPlayerCard, this);
            if (enemyCards.Length > 0 && friendCards.Length <= 3)
            {
                canUse = true;
            }
            else
            {
                return false;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.SUMMON_SPECIFIC_UNIT))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            if (friendCards.Length <= 4)
            {
                canUse = true;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST))
        {
            // 自分自身はコスト減少の対象外なので、自身を除いた手札が1枚でもあれば発動できる。
            // （以前はGetFriendHandTransform() をそのまま数えて > 1 としていたため、
            //  自身が手札から外れているプレイヤーの経路では「他に2枚」必要になり、
            //  手札が他に1枚のときに発動しなかった）
            CardController[] handCards = gameManager.GetFriendHandTransformExcept(this.model.isPlayerCard, this);
            if (handCards.Length > 0)
            {
                canUse = true;
            }
            else
            {
                return false;
            }

        }
        if (model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST))
        {
            CardController[] enemyHandCards = gameManager.GetEnemyHandTransform(this.model.isPlayerCard);
            if (enemyHandCards.Length > 0)
            {
                canUse = true;
            }
            else
            {
                return false;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_NULLIFY_ONCE))
        {
            canUse = true;
        }
        if (model.abilities.HasFlag(ABILITIES.DOUBLE_ACTION))
        {
            canUse = true;
        }
        if (model.abilities.HasFlag(ABILITIES.SEARCH_SPECIFIC_UNIT))
        {
            if (model.targetCards != null && model.targetCards.Length > 0)
            {
                canUse = true;
            }
        }


        if (model.abilities.HasFlag(ABILITIES.DISCARD_ALL_ENEMY_HAND))
        {
            CardController[] cards = GameManager.instance.GetEnemyHandTransform(model.isPlayerCard);
            if (cards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.DISCARD_ENEMY_HAND))
        {
            CardController[] cards = GameManager.instance.GetEnemyHandTransform(model.isPlayerCard);
            if (cards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.DISCARD_ALL_FRIEND_HAND))
        {
            CardController[] cards = GameManager.instance.GetFriendHandTransform(model.isPlayerCard);
            if (cards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.DISCARD_FRIEND_HAND))
        {
            CardController[] cards = GameManager.instance.GetFriendHandTransform(model.isPlayerCard);
            if (cards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.STATS_UP_ON_ATTACK))
        {
            canUse = true;
        }
        if (model.abilities.HasFlag(ABILITIES.DESTROY_ATTACKED_TARGET))
        {
            canUse = true;
        }
        if (model.abilities.HasFlag(ABILITIES.RANDOM_ENEMY))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                canUse = true;
            }
            else
            {
                return false;
            }
        }
        if (model.abilities.HasFlag(ABILITIES.RANDOM_FRIEND))
        {
            CardController[] enemyCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                canUse = true;
            }
            else
            {
                return false;
            }
        }

        return canUse;
    }

    public IEnumerator CheckAlive()
    {
        if (model.isAlive)
        {
            RefreshView();
        }
        else
        {
            if (this == null) yield break;
            destroyEffect(model.destroyEffect, transform);
            Destroy(this.gameObject);
        }
    }

    public void UseSpellTo(CardController target)
    {
        if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARD))
        {
            // 特定の敵を攻撃する
            if (target == null)
            {
                return;
            }
            if (target.model.isPlayerCard == model.isPlayerCard)
            {
                return;
            }
            EffectAttack(target);
            StartCoroutine(target.CheckAlive());
        }

        if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS))
        {
            // 相手フィールドの全てのカードに攻撃する
            //CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            //foreach (CardController enemyCard in enemyCards)
            //{
            //    EffectAttack(enemyCard);
            //}
            //foreach (CardController enemyCard in enemyCards)
            //{
            //    enemyCard.CheckAlive();
            //}
            EffectAttack(target);
        }
        if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO))
        {
            gameManager.AttackToHero(this);
            gameManager.CheckHeroHP();
        }
        if (model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARD))
        {
            if (target == null)
            {
                return;
            }
            if (target.model.isPlayerCard != model.isPlayerCard)
            {
                return;
            }
            EffectHeal(target);
        }
        if (model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS))
        {
            //CardController[] friendsCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            //foreach (CardController friendCard in friendsCards)
            //{
            //    EffectHeal(friendCard);
            //}
            EffectHeal(target);
        }
        if (model.spells.HasFlag(SPELLS.DESTROY_ENEMY_CARD))
        {
            if (target == null)
            {
                return;
            }
            if (target.model.isPlayerCard == model.isPlayerCard)
            {
                return;
            }
            Destroys(target);
            StartCoroutine(target.CheckAlive());
        }
        if (model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
        {
            gameManager.AttackToHero(this);
            gameManager.CheckHeroHP();
        }
        if (model.spells.HasFlag(SPELLS.STEAL_ENEMY_CARD))
        {
            if (target == null)
            {
                return;
            }
            if (target.model.isPlayerCard == model.isPlayerCard)
            {
                return;
            }
            Steal(target);
        }
        if (model.spells.HasFlag(SPELLS.DRAW_CARDS))
        {
            DrawCard(this);
        }
        if (model.spells.HasFlag(SPELLS.DESTROY_ALL_FIELD_CARDS))
        {
            //CardController[] friendsCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            //CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            //if (friendsCards.Length > 0)
            //{
            //    foreach (CardController friendCard in friendsCards)
            //    {
            //        Destroys(friendCard);
            //        friendCard.CheckAlive();
            //    }
            //}
            //if (enemyCards.Length > 0)
            //{
            //    foreach (CardController enemyCard in enemyCards)
            //    {
            //        Destroys(enemyCard);
            //        enemyCard.CheckAlive();
            //    }
            //}

            Destroys(target);
            StartCoroutine(target.CheckAlive());
        }
        if (model.spells.HasFlag(SPELLS.RANDOM_DAMAGE))
        {
            if (target == null)
            {
                CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
                target = enemyCards[UnityEngine.Random.Range(0, enemyCards.Length - 1)];
            }
            EffectAttack(target);
            StartCoroutine(target.CheckAlive());
        }

        if (model.spells.HasFlag(SPELLS.SWAP_HP_ATK))
        {
            if (target != null)
            {
                SwapHPToATK(target);
                target.RefreshView();
            }
        }

        if (model.spells.HasFlag(SPELLS.CONDITIONAL_ENEMY_DEBUFF))
        {
            if (target != null)
            {
                AttackDebuff(this, target);
                target.RefreshView();
            }
        }
        if (model.spells.HasFlag(SPELLS.CONDITIONAL_FRIEND_BUFF))
        {
            if (target != null)
            {
                AttackBuff(this, target);
                target.RefreshView();
            }
        }
        if (model.spells.HasFlag(SPELLS.INCREASE_ENEMY_COST))
        {
            IncreaseEnemyHandCost(target);
        }
        if (model.spells.HasFlag(SPELLS.REDUCE_HAND_COST))
        {
            ReduceHandCost(target);
        }
        if (model.spells.HasFlag(SPELLS.DISCARD_FRIEND_HAND) || model.spells.HasFlag(SPELLS.DISCARD_ALL_FRIEND_HAND))
        {
            DiscardPlayerHandCard(target);
        }
        if (model.spells.HasFlag(SPELLS.DISCARD_ENEMY_HAND) || model.spells.HasFlag(SPELLS.DISCARD_ALL_ENEMY_HAND))
        {
            DiscardEnemyHandCard(target);
        }

        //gameManager.ReduceManaCost(model.cost, model.isPlayerCard);
        Destroy(this.gameObject);
    }


    public bool CanUseSpells()
    {
        bool canUse = false;
        if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARD) || model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS)
            || model.spells.HasFlag(SPELLS.DESTROY_ENEMY_CARD) || model.spells.HasFlag(SPELLS.CONDITIONAL_ENEMY_DEBUFF))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.spells.HasFlag(SPELLS.STEAL_ENEMY_CARD) || model.spells.HasFlag(SPELLS.SUMMON_SPECIFIC_UNIT))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0 && friendCards.Length <= 4)
            {
                canUse = true;
            }
        }

        if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) || model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO) || model.spells.HasFlag(SPELLS.DRAW_CARDS))
        {
            canUse = true;
        }
        if (model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARD) || model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS) || model.spells.HasFlag(SPELLS.CONDITIONAL_FRIEND_BUFF))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            if (friendCards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.spells.HasFlag(SPELLS.REDUCE_HAND_COST))
        {
            CardController[] handCards = gameManager.GetFriendHandTransform(this.model.isPlayerCard);
            if (handCards.Length > 1)
            {
                canUse = true;
            }
        }
        if (model.spells.HasFlag(SPELLS.INCREASE_ENEMY_COST))
        {
            CardController[] enemyHandCards = gameManager.GetEnemyHandTransform(this.model.isPlayerCard);
            if (enemyHandCards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.spells.HasFlag(SPELLS.DESTROY_ALL_FIELD_CARDS) || model.spells.HasFlag(SPELLS.SWAP_HP_ATK))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            if (friendCards.Length > 0 || enemyCards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.spells.HasFlag(SPELLS.RANDOM_ENEMY))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                canUse = true;
            }
            else
            {
                return false;
            }
        }
        if (model.spells.HasFlag(SPELLS.RANDOM_FRIEND))
        {
            CardController[] enemyCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                canUse = true;
            }
            else
            {
                return false;
            }
        }
        if (model.spells.HasFlag(SPELLS.DISCARD_ENEMY_HAND) || model.spells.HasFlag(SPELLS.DISCARD_ALL_ENEMY_HAND))
        {
            CardController[] cards = GameManager.instance.GetEnemyHandTransform(model.isPlayerCard);
            if (cards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.spells.HasFlag(SPELLS.DISCARD_FRIEND_HAND) || model.spells.HasFlag(SPELLS.DISCARD_ALL_FRIEND_HAND))
        {
            CardController[] cards = GameManager.instance.GetFriendHandTransform(model.isPlayerCard);
            if (cards.Length > 0)
            {
                canUse = true;
            }
        }
        return canUse;
    }

    /// <summary>
    /// 敵への攻撃エフェクト
    /// </summary>
    /// <param name="target"></param>
    /// <param name="isDefense"></param>
    public void attackEffect(CardController target, bool isDefense)
    {
        DG.Tweening.Sequence seq = DOTween.Sequence();
        GameManager.instance.isAttacking = true;
        seq.Append(transform
            .DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd)
            .OnUpdate(() =>
            {
                float y = transform.localEulerAngles.y;
                // Unityでは-90度が270度として表現されることがあるので360でmod取る
                if (y >= 90 && y <= 270)
                {
                    view.maskPanel.SetActive(true);  // 裏面
                }
                else
                {
                    view.maskPanel.SetActive(false); // 表面
                }
            })
        );
        seq.Play();
        Transform trans = effect.AttackEffect(model.attackEffect, transform);
        switch (model.attackType)
        {
            case ATTACKTYPE.THROW:
                StartThrow(trans, 5, transform.position, target.transform.position, model.attackTime, target, isDefense);
                break;

            case ATTACKTYPE.DIRECT:
                DirectAttack(trans, target.transform, target, isDefense, model.attackTime / 60);
                break;

            case ATTACKTYPE.SPAWN:
                StartCoroutine(SpawnEffect(trans, target.transform, target, isDefense, model.attackTime / 60));
                break;
        }
    }

    public IEnumerator SpawnEffect(Transform effect, Transform targetPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.position = targetPos.position;
        yield return new WaitForSeconds(attackTime);
        // 通常攻撃(DirectAttack)・アビリティ(DirectAttackAbility)・
        // スペル(DirectSpellAttack)はいずれもヒットエフェクトを出すのに、
        // 通常攻撃のSPAWN版だけ抜けていたので合わせる
        hitEffect(targetPos);
        GameManager.instance.isAttacking = !isDefense;
        CheckAttackParticle(effect);
        // 破壊者(isDestroyer)はDestroys()が既に同期でダメージ無効を含めた結果を確定させている。
        // ここで通常ダメージを重ねて適用すると、ダメージ無効を1回消費した後の
        // 2発目が素通りしてしまう（無効化が意味をなさなくなる）ため、破壊者はスキップする。
        // ダメージ無効で吸収される場合は0を表示する。Attack()を通すとフラグが消費されて
        // 判別できなくなるため、必ず適用前に控えること。
        int damageText = GetAttackDamageText(enemy);
        if (!model.isDestroyer)
        {
            model.Attack(enemy);
        }
        enemy.RefreshView();
        effect.SetParent(GameManager.instance.uiParticlesManager.transform);
        GameObject textObj = GameManager.instance.GetTextPool();
        StartCoroutine(GameManager.instance.GenDamageText(textObj, damageText, targetPos));
        if (model.hitAudio != null)
            audioSource.PlayOneShot(model.hitAudio);
    }

    public void DirectAttack(Transform effect, Transform endPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.DOMove(endPos.position, attackTime)
            .OnComplete(() =>
            {
                hitEffect(endPos);
                GameManager.instance.isAttacking = !isDefense;
                CheckAttackParticle(effect);
                // ダメージ無効の判定はAttack()より前に行う（SpawnEffect側のコメント参照）
                int damageText = GetAttackDamageText(enemy);
                // 破壊者はDestroys()が既に結果を確定させているため、重複適用をスキップする
                // （SpawnEffect側のコメント参照）
                if (!model.isDestroyer)
                {
                    model.Attack(enemy);
                }
                enemy.RefreshView();
                effect.SetParent(GameManager.instance.uiParticlesManager.transform);
                GameObject textObj = GameManager.instance.GetTextPool();
                StartCoroutine(GameManager.instance.GenDamageText(textObj, damageText, endPos));
                if (model.hitAudio != null)
                    audioSource.PlayOneShot(model.hitAudio);
            });
    }
    public void StartThrow(Transform target, float height, Vector3 start, Vector3 end, float duration, CardController enemyCC, bool isDefense, bool destroyOnComplete = true)
    {
        // 中点を求める
        Vector3 half = end - start * 0.50f + start;
        half.y += Vector3.up.y + height;

        StartCoroutine(LerpThrow(target, start, half, end, duration, destroyOnComplete, enemyCC, isDefense));
    }

    IEnumerator LerpThrow(Transform target, Vector3 start, Vector3 half, Vector3 end, float duration, bool destroyOnComplete, CardController enemyCC, bool isDefense)
    {
        float startTime = Time.timeSinceLevelLoad;
        float rate = 0f;
        Transform targetPos = target;
        while (true)
        {
            if (rate >= 1.0f)
            {
                target.position = end;

                if (destroyOnComplete)
                {
                    if (model.hitAudio != null)
                        audioSource.PlayOneShot(model.hitAudio);

                    hitEffect(targetPos);
                    GameManager.instance.isAttacking = !isDefense;
                    CheckAttackParticle(target);
                    // ダメージ無効の判定はAttack()より前に行う（SpawnEffect側のコメント参照）
                    int damageText = GetAttackDamageText(enemyCC);
                    // 破壊者はDestroys()が既に結果を確定させているため、重複適用をスキップする
                    // （SpawnEffect側のコメント参照）
                    if (!model.isDestroyer)
                    {
                        model.Attack(enemyCC);
                    }
                    enemyCC.RefreshView();
                    target.SetParent(GameManager.instance.uiParticlesManager.transform);
                    GameObject textObj = GameManager.instance.GetTextPool();
                    StartCoroutine(GameManager.instance.GenDamageText(textObj, damageText, targetPos));
                }
                yield break;
            }
            float diff = Time.timeSinceLevelLoad - startTime;
            rate = diff / (duration / 60f);
            target.position = CalcLerpPoint(start, half, end, rate);

            yield return null;
        }
    }


    Vector3 CalcLerpPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        var a = Vector3.Lerp(p0, p1, t);
        var b = Vector3.Lerp(p1, p2, t);
        return Vector3.Lerp(a, b, t);
    }

    //アビリティエフェクト
    public void AbilityEffect(CardController target, bool isDefense)
    {
        Transform trans = null;
        GameManager.instance.isAttacking = true;
        if (model.attackType != ATTACKTYPE.NONE)
        {
            trans = effect.AttackEffect(model.summonAbilityEffect, transform);
        }
        switch (model.attackType)
        {
            case ATTACKTYPE.THROW:
                StartThrowAbility(trans, 5, transform.position, target.transform.position, model.attackTime, target, isDefense);
                break;

            case ATTACKTYPE.DIRECT:
                DirectAttackAbility(trans, target.transform, target, isDefense, model.attackTime / 60);
                break;

            case ATTACKTYPE.SPAWN:
                StartCoroutine(SpawnEffectAbility(trans, target.transform, target, isDefense, model.attackTime / 60));
                break;

            case ATTACKTYPE.NONE:
                UseAbilitiesTo(target);
                break;
        }
    }

    public IEnumerator SpawnEffectAbility(Transform effect, Transform targetPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.position = targetPos.position;
        yield return new WaitForSeconds(attackTime);
        GameManager.instance.isAttacking = !isDefense;
        CheckAttackParticle(effect);
        // ダメージ・回復の数字はEffectAttack()/EffectHeal()から出るので、ここでは出さない
        UseAbilitiesTo(enemy);
        enemy.RefreshView();
        effect.SetParent(GameManager.instance.uiParticlesManager.transform);
        if (model.hitAudio != null)
            audioSource.PlayOneShot(model.hitAudio);

    }
    public void DirectAttackAbility(Transform effect, Transform endPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.DOMove(endPos.position, attackTime)
            .OnComplete(() =>
            {
                hitEffect(endPos);
                GameManager.instance.isAttacking = !isDefense;
                CheckAttackParticle(effect);
                // ダメージ・回復の数字はEffectAttack()/EffectHeal()から出るので、ここでは出さない
                UseAbilitiesTo(enemy);
                enemy.RefreshView();
                effect.SetParent(GameManager.instance.uiParticlesManager.transform);
                if (model.hitAudio != null)
                    audioSource.PlayOneShot(model.hitAudio);
            });
    }
    public void StartThrowAbility(Transform target, float height, Vector3 start, Vector3 end, float duration, CardController enemyCC, bool isDefense, bool destroyOnComplete = true)
    {
        // 中点を求める
        Vector3 half = end - start * 0.50f + start;
        half.y += Vector3.up.y + height;

        StartCoroutine(LerpThrowAbility(target, start, half, end, duration, destroyOnComplete, enemyCC, isDefense));
    }

    IEnumerator LerpThrowAbility(Transform target, Vector3 start, Vector3 half, Vector3 end, float duration, bool destroyOnComplete, CardController enemyCC, bool isDefense)
    {
        float startTime = Time.timeSinceLevelLoad;
        float rate = 0f;
        while (true)
        {
            if (rate >= 1.0f)
            {
                target.position = end;

                if (destroyOnComplete)
                {
                    hitEffect(enemyCC.transform);
                    GameManager.instance.isAttacking = !isDefense;
                    CheckAttackParticle(target);
                    // ダメージ・回復の数字はEffectAttack()/EffectHeal()から出るので、ここでは出さない
                    UseAbilitiesTo(enemyCC);
                    enemyCC.RefreshView();
                    target.SetParent(GameManager.instance.uiParticlesManager.transform);
                    if (model.hitAudio != null)
                        audioSource.PlayOneShot(model.hitAudio);
                }
                yield break;
            }
            float diff = Time.timeSinceLevelLoad - startTime;
            rate = diff / (duration / 60f);
            target.position = CalcLerpPoint(start, half, end, rate);

            yield return null;
        }
    }

    //ヒーローへの攻撃

    public void attackEffectHero(Transform target, bool isDefense)
    {
        DG.Tweening.Sequence seq = DOTween.Sequence();
        GameManager.instance.isAttacking = true;
        seq.Append(transform
            .DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd)
            .OnUpdate(() =>
            {
                float y = transform.localEulerAngles.y;
                // Unityでは-90度が270度として表現されることがあるので360でmod取る
                if (y >= 90 && y <= 270)
                {
                    view.maskPanel.SetActive(true);  // 裏面
                }
                else
                {
                    view.maskPanel.SetActive(false); // 表面
                }
            })
        );
        seq.Play(); Transform trans = effect.AttackEffect(model.attackEffect, transform);
        switch (model.attackType)
        {
            case ATTACKTYPE.THROW:
                StartThrowHero(trans, 5, transform.position, target.position, model.attackTime, isDefense);
                break;

            case ATTACKTYPE.DIRECT:
                DirectAttackHero(trans, target.transform, isDefense, model.attackTime / 60);
                break;

            case ATTACKTYPE.SPAWN:
                StartCoroutine(SpawnEffectHero(trans, target.transform, isDefense, model.attackTime / 60));
                break;
        }
    }

    public IEnumerator SpawnEffectHero(Transform effect, Transform targetPos, bool isDefense, float attackTime)
    {
        effect.position = targetPos.position;
        yield return new WaitForSeconds(attackTime);
        // DirectAttackHero・LerpThrowHeroはヒットエフェクトを出すのに、
        // ヒーロー攻撃のSPAWN版だけ抜けていたので合わせる
        hitEffect(targetPos);
        GameManager.instance.isAttacking = !isDefense;
        CheckAttackParticle(effect);
        if (model.isPlayerCard)
        {
            gameManager.enemy.heroHp -= model.at;
        }
        else
        {
            gameManager.player.heroHp -= model.at;
        }
        Transform targetHero = null;
        if (model.isPlayerCard)
            targetHero = gameManager.enemyHero;
        else
            targetHero = gameManager.playerHero;

        if (model.hitAudio != null)
            audioSource.PlayOneShot(model.hitAudio);
        GameObject textObj = GameManager.instance.GetTextPool();
        StartCoroutine(GameManager.instance.GenDamageText(textObj, model.at, targetHero));

        gameManager.uiManager.ShowHeroHP(gameManager.player.heroHp, gameManager.enemy.heroHp);
        GameManager.instance.CheckHeroHP();
        effect.SetParent(GameManager.instance.uiParticlesManager.transform);
    }

    public void DirectAttackHero(Transform effect, Transform endPos, bool isDefense, float attackTime)
    {
        effect.DOMove(endPos.position, attackTime)
            .OnComplete(() =>
            {
                hitEffect(endPos);
                GameManager.instance.isAttacking = !isDefense;
                CheckAttackParticle(effect);
                if (model.isPlayerCard)
                {
                    gameManager.enemy.heroHp -= model.at;
                }
                else
                {
                    gameManager.player.heroHp -= model.at;
                }
                Transform targetHero = null;
                if (model.isPlayerCard)
                    targetHero = gameManager.enemyHero;
                else
                    targetHero = gameManager.playerHero;

                if (model.hitAudio != null)
                    audioSource.PlayOneShot(model.hitAudio);
                GameObject textObj = GameManager.instance.GetTextPool();
                StartCoroutine(GameManager.instance.GenDamageText(textObj, model.at, targetHero));

                gameManager.uiManager.ShowHeroHP(gameManager.player.heroHp, gameManager.enemy.heroHp);
                GameManager.instance.CheckHeroHP();
                effect.SetParent(GameManager.instance.uiParticlesManager.transform);
            });
    }
    public void StartThrowHero(Transform target, float height, Vector3 start, Vector3 end, float duration, bool isDefense, bool destroyOnComplete = true)
    {
        // 中点を求める
        Vector3 half = end - start * 0.50f + start;
        half.y += Vector3.up.y + height;

        StartCoroutine(LerpThrowHero(target, start, half, end, duration, destroyOnComplete, isDefense));
    }

    IEnumerator LerpThrowHero(Transform target, Vector3 start, Vector3 half, Vector3 end, float duration, bool destroyOnComplete, bool isDefense)
    {
        float startTime = Time.timeSinceLevelLoad;
        float rate = 0f;
        Transform targetPos = target;
        while (true)
        {
            if (rate >= 1.0f)
            {
                target.position = end;

                if (destroyOnComplete)
                {
                    hitEffect(targetPos);
                    GameManager.instance.isAttacking = !isDefense;
                    CheckAttackParticle(target);
                    if (model.isPlayerCard)
                    {
                        gameManager.enemy.heroHp -= model.at;
                    }
                    else
                    {
                        gameManager.player.heroHp -= model.at;
                    }
                    Transform targetHero = null;
                    if (model.isPlayerCard)
                        targetHero = gameManager.enemyHero;
                    else
                        targetHero = gameManager.playerHero;

                    if (model.hitAudio != null)
                        audioSource.PlayOneShot(model.hitAudio);
                    GameObject textObj = GameManager.instance.GetTextPool();
                    StartCoroutine(GameManager.instance.GenDamageText(textObj, model.at, targetHero));
                    gameManager.uiManager.ShowHeroHP(gameManager.player.heroHp, gameManager.enemy.heroHp);
                    GameManager.instance.CheckHeroHP();
                    target.SetParent(GameManager.instance.uiParticlesManager.transform);
                }
                yield break;
            }
            float diff = Time.timeSinceLevelLoad - startTime;
            rate = diff / (duration / 60f);
            target.position = CalcLerpPoint(start, half, end, rate);

            yield return null;
        }
    }

    /// <summary>
    /// ヒーローへの効果（DAMAGE_ENEMY_HERO / HEAL_FRIEND_HERO）をHPに適用し、
    /// 数字を出す対象ヒーロー・数値・回復かどうかを返す。
    /// ATTACKTYPE（THROW/DIRECT/SPAWN）ごとに同じ処理を3か所へ書き写していた結果、
    /// アビリティ版のフラグがTHROWでしか見られておらず、
    /// DIRECT/SPAWNではHPが変わらないのに数字だけ出ていたためここへ集約した。
    /// </summary>
    void ApplyHeroEffect(out Transform targetHero, out int amount, out bool isHeal)
    {
        bool isDamage = model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) ||
                        model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO);
        isHeal = !isDamage && (model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO) ||
                               model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO));
        amount = model.effectDmg;
        targetHero = null;

        if (isDamage)
        {
            if (model.isPlayerCard)
            {
                gameManager.enemy.heroHp -= amount;
                targetHero = gameManager.enemyHero;
            }
            else
            {
                gameManager.player.heroHp -= amount;
                targetHero = gameManager.playerHero;
            }
        }
        else if (isHeal)
        {
            if (model.isPlayerCard)
            {
                gameManager.player.heroHp += amount;
                targetHero = gameManager.playerHero;
            }
            else
            {
                gameManager.enemy.heroHp += amount;
                targetHero = gameManager.enemyHero;
            }
        }
    }

    //スペルでのヒーローへの攻撃
    public void attackSpellEffectHero(Transform target, bool isDefense)
    {
        DG.Tweening.Sequence seq = DOTween.Sequence();
        GameManager.instance.isAttacking = true;
        seq.Append(transform
            .DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd)
            .OnUpdate(() =>
            {
                float y = transform.localEulerAngles.y;
                // Unityでは-90度が270度として表現されることがあるので360でmod取る
                if (y >= 90 && y <= 270)
                {
                    view.maskPanel.SetActive(true);  // 裏面
                }
                else
                {
                    view.maskPanel.SetActive(false); // 表面
                }
            })
        );
        seq.Play(); Transform trans = effect.AttackEffect(model.attackEffect, transform);
        switch (model.attackType)
        {
            case ATTACKTYPE.THROW:
                StartSpellThrowHero(trans, 5, transform.position, target.position, model.attackTime, isDefense);
                break;

            case ATTACKTYPE.DIRECT:
                DirectSpellAttackHero(trans, target.transform, isDefense, model.attackTime / 60);
                break;

            case ATTACKTYPE.SPAWN:
                StartCoroutine(SpawnSpellEffectHero(trans, target.transform, isDefense, model.attackTime / 60));
                break;
        }
    }

    public IEnumerator SpawnSpellEffectHero(Transform effect, Transform targetPos, bool isDefense, float attackTime)
    {
        effect.position = targetPos.position;
        yield return new WaitForSeconds(attackTime);
        // DirectSpellAttackHero・LerpThrowSpellHeroはヒットエフェクトを出すのに、
        // スペルのヒーロー攻撃のSPAWN版だけ抜けていたので合わせる
        hitEffect(targetPos);
        GameManager.instance.isAttacking = !isDefense;
        CheckAttackParticle(effect);
        ApplyHeroEffect(out Transform targetHero, out int amount, out bool isHeal);

        if (model.hitAudio != null)
            audioSource.PlayOneShot(model.hitAudio);
        ShowEffectText(targetHero, amount, isHeal);

        gameManager.uiManager.ShowHeroHP(gameManager.player.heroHp, gameManager.enemy.heroHp);
        GameManager.instance.CheckHeroHP();
        effect.SetParent(GameManager.instance.uiParticlesManager.transform);
        if (model.abilities.HasFlag(ABILITIES.NONE))
        {
            yield break;
        }
        Destroy(this.gameObject);
    }

    public void DirectSpellAttackHero(Transform effect, Transform endPos, bool isDefense, float attackTime)
    {
        effect.DOMove(endPos.position, attackTime)
            .OnComplete(() =>
            {
                hitEffect(endPos);
                GameManager.instance.isAttacking = !isDefense;
                CheckAttackParticle(effect);
                ApplyHeroEffect(out Transform targetHero, out int amount, out bool isHeal);

                if (model.hitAudio != null)
                    audioSource.PlayOneShot(model.hitAudio);
                ShowEffectText(targetHero, amount, isHeal);

                gameManager.uiManager.ShowHeroHP(gameManager.player.heroHp, gameManager.enemy.heroHp);
                GameManager.instance.CheckHeroHP();
                effect.SetParent(GameManager.instance.uiParticlesManager.transform);
                // ここは無条件にDestroyしていたため、アビリティでヒーローを対象にした
                // フォロワー（スペルではないカード）まで消えてしまっていた。
                // 使い切りのスペルカードだけを破棄する
                if (IsSpell)
                {
                    Destroy(this.gameObject);
                }
            });
    }
    public void StartSpellThrowHero(Transform target, float height, Vector3 start, Vector3 end, float duration, bool isDefense, bool destroyOnComplete = true)
    {
        // 中点を求める
        Vector3 half = end - start * 0.50f + start;
        half.y += Vector3.up.y + height;

        StartCoroutine(LerpThrowSpellHero(target, start, half, end, duration, destroyOnComplete, isDefense));
    }

    IEnumerator LerpThrowSpellHero(Transform target, Vector3 start, Vector3 half, Vector3 end, float duration, bool destroyOnComplete, bool isDefense)
    {
        float startTime = Time.timeSinceLevelLoad;
        float rate = 0f;
        Transform targetPos = target;
        while (true)
        {
            if (rate >= 1.0f)
            {
                target.position = end;

                if (destroyOnComplete)
                {
                    hitEffect(targetPos);
                    GameManager.instance.isAttacking = !isDefense;
                    CheckAttackParticle(target);
                    ApplyHeroEffect(out Transform targetHero, out int amount, out bool isHeal);

                    if (model.hitAudio != null)
                        audioSource.PlayOneShot(model.hitAudio);
                    ShowEffectText(targetHero, amount, isHeal);

                    gameManager.uiManager.ShowHeroHP(gameManager.player.heroHp, gameManager.enemy.heroHp);
                    GameManager.instance.CheckHeroHP();
                    target.SetParent(GameManager.instance.uiParticlesManager.transform);
                    if (model.abilities.HasFlag(ABILITIES.NONE))
                    {
                        yield break;
                    }
                    Destroy(this.gameObject);
                }
                yield break;
            }
            float diff = Time.timeSinceLevelLoad - startTime;
            rate = diff / (duration / 60f);
            target.position = CalcLerpPoint(start, half, end, rate);

            yield return null;
        }
    }

    //スペルエフェクト
    public void spellEffect(CardController target, bool isDefense)
    {
        GameManager.instance.isAttacking = true;
        Transform trans = null;
        if (model.attackType != ATTACKTYPE.NONE)
        {
            trans = effect.AttackEffect(model.attackEffect, transform);
        }
        switch (model.attackType)
        {
            // THROWのcaseが無く、attackTypeがTHROWのスペルカードは
            // エフェクトを生成するだけで効果も数字も出ないまま終わっていたので追加した
            case ATTACKTYPE.THROW:
                StartThrowSpell(trans, 5, transform.position, target.transform.position, model.attackTime, target, isDefense);
                break;

            case ATTACKTYPE.DIRECT:
                DirectSpellAttack(trans, target.transform, target, isDefense, model.attackTime / 60);
                break;

            case ATTACKTYPE.SPAWN:
                StartCoroutine(SpawnSpellEffect(trans, target.transform, target, isDefense, model.attackTime / 60));
                break;

            case ATTACKTYPE.NONE:
                UseSpellTo(target);
                break;
        }
    }

    public void StartThrowSpell(Transform target, float height, Vector3 start, Vector3 end, float duration, CardController enemyCC, bool isDefense, bool destroyOnComplete = true)
    {
        // 中点を求める
        Vector3 half = end - start * 0.50f + start;
        half.y += Vector3.up.y + height;

        StartCoroutine(LerpThrowSpell(target, start, half, end, duration, destroyOnComplete, enemyCC, isDefense));
    }

    IEnumerator LerpThrowSpell(Transform target, Vector3 start, Vector3 half, Vector3 end, float duration, bool destroyOnComplete, CardController enemyCC, bool isDefense)
    {
        float startTime = Time.timeSinceLevelLoad;
        float rate = 0f;
        while (true)
        {
            if (rate >= 1.0f)
            {
                target.position = end;

                if (destroyOnComplete)
                {
                    hitEffect(enemyCC.transform);
                    GameManager.instance.isAttacking = !isDefense;
                    CheckAttackParticle(target);
                    if (model.hitAudio != null)
                        audioSource.PlayOneShot(model.hitAudio);
                    target.SetParent(GameManager.instance.uiParticlesManager.transform);
                    // ダメージ・回復の数字はEffectAttack()/EffectHeal()から出る。
                    // UseSpellTo()は最後に自分自身をDestroyし、このコルーチンもそこで止まるため必ず最後に呼ぶ
                    UseSpellTo(enemyCC);
                    enemyCC.RefreshView();
                }
                yield break;
            }
            float diff = Time.timeSinceLevelLoad - startTime;
            rate = diff / (duration / 60f);
            target.position = CalcLerpPoint(start, half, end, rate);

            yield return null;
        }
    }


    // 破壊・手札破棄・コスト増減のように「ダメージでも回復でもない」効果で
    // 数字が出てしまわないよう、効果の種類で表示可否を判定していた（ShowsDamageNumber）が、
    // 数字の表示自体をEffectAttack()/EffectHeal()へ集約したため不要になった。
    // ＝ HPを実際に増減させたときだけ数字が出る。

    public void DirectSpellAttack(Transform effect, Transform endPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.DOMove(endPos.position, attackTime)
            .OnComplete(() =>
            {
                // 通常攻撃(DirectAttack)・アビリティ(DirectAttackAbility)・
                // スペルのヒーロー攻撃(DirectSpellAttackHero)はいずれもヒットエフェクトを
                // 出すのに、スペルのカード攻撃だけ抜けていたので合わせる
                hitEffect(endPos);
                GameManager.instance.isAttacking = !isDefense;
                //model.Attack(enemy);
                CheckAttackParticle(effect);
                effect.SetParent(GameManager.instance.uiParticlesManager.transform);
                if (model.hitAudio != null)
                    audioSource.PlayOneShot(model.hitAudio);
                // ダメージ・回復の数字はEffectAttack()/EffectHeal()から出るので、ここでは出さない。
                // UseSpellTo()は最後に自分自身をDestroyするので必ず最後に呼ぶ
                UseSpellTo(enemy);
                enemy.RefreshView();
            });
    }

    public IEnumerator SpawnSpellEffect(Transform effect, Transform targetPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.position = targetPos.position;
        yield return new WaitForSeconds(attackTime);
        GameManager.instance.isAttacking = !isDefense;
        //model.Attack(enemy);
        CheckAttackParticle(effect);
        if (model.hitAudio != null)
            audioSource.PlayOneShot(model.hitAudio);
        effect.SetParent(GameManager.instance.uiParticlesManager.transform);
        // ダメージ・回復の数字はEffectAttack()/EffectHeal()から出るので、ここでは出さない。
        // UseSpellTo()は最後に自分自身をDestroyし、このコルーチンもそこで止まるため必ず最後に呼ぶ
        UseSpellTo(enemy);
        enemy.RefreshView();
    }

    public void hitEffect(Transform target)
    {
        // model.hitEffect（ヒットエフェクトのParticleSystem）が未設定のカードでは、
        // EffectController.HitEffect内のInstantiate(null)でエラーになるため、
        // 全ATTACKTYPE共通でここでガードする（表示しないだけで済ませる）
        if (model.hitEffect == null) return;

        Transform hitEffect = effect.HitEffect(model.hitEffect, target.transform);
        CheckAnyParticle(hitEffect);
    }

    public void destroyEffect(ParticleSystem destroyObj, Transform trans)
    {
        Transform destroyEffect = effect.DestroyEffect(destroyObj, trans);
        CheckAnyParticle(destroyEffect);
    }

    public void summonEffect(ParticleSystem summonObj, Transform trans)
    {
        Transform summonEffect = effect.SummonEffect(summonObj, trans);
        StartCoroutine(CheckDestroyParticle(summonEffect));
    }

    public void CardDisappearEffect(ParticleSystem summonObj, Transform trans)
    {
        Transform summonEffect = effect.CardDisappearEffect(summonObj, trans);
        StartCoroutine(CheckDestroyParticle(summonEffect));
    }

    public void CheckAttackParticle(Transform target)
    {
        if (target != null && target.childCount > 0)
        {
            // 最初の実際のパーティクルオブジェクトを探す
            Transform particleChild = null;
            for (int i = 0; i < target.childCount; i++)
            {
                var child = target.GetChild(i);
                if (!child.name.StartsWith("[generated]") &&
                    !child.GetComponent<UIParticleRenderer>())
                {
                    particleChild = child;
                    break;
                }
            }

            if (particleChild != null)
            {
                Destroy(particleChild.gameObject);
                target.SetParent(GameManager.instance.uiParticlesManager.transform);
            }
        }
    }
    public void CheckAnyParticle(Transform target)
    {
        if (target != null && target.childCount > 0)
        {
            foreach (Transform child in target)
            {
                GameObject.Destroy(child.gameObject, 0.5f);
            }
            target.SetParent(GameManager.instance.uiParticlesManager.transform);
        }
    }

    public IEnumerator CheckDestroyParticle(Transform target)
    {
        if (target != null && target.childCount > 0)
        {
            // 最初の実際のパーティクルオブジェクトを探す
            Transform particleChild = null;
            for (int i = 0; i < target.childCount; i++)
            {
                var child = target.GetChild(i);
                if (!child.name.StartsWith("[generated]") &&
                    !child.GetComponent<UIParticleRenderer>())
                {
                    particleChild = child;
                    break;
                }
            }

            if (particleChild != null)
            {
                Destroy(particleChild.gameObject, 0.5f);
                yield return new WaitForSeconds(0.5f);
                target.SetParent(GameManager.instance.uiParticlesManager.transform);
            }
        }
    }
}

/*　
 *　配列管理のエフェクトや音源
 *　
 *　場に出すときの整合性
 *　
 *　スペルエフェクト実装　[ランダムと敵分]
 *　攻撃で片方が死んだときの処理
 *　時間制限後の処理
 *　
 *　不具合　
 *済　EFFECT_SELECTIONのカードを場に出してターンが変わると手札に戻る
 *済　攻撃時のドラッグの矢印が表示されない（攻撃もできない）
 *済　EFFECT_SELECTIONで選択がエラーになる
 *済  ドラッグ中にターンが変更されても手札にカードが戻らない
 *済　敵のアビリティ後にカード召喚が早い
 *　　ターン終了時の整合性（一部動かせてしまう）
 *　　    ドロー中にカードをドラッグできてしまう
 *　
 *　デッキ編成画面
 *　アビリティの処理確認
 *　ターン変更の整合性
 *　
 *　
 * 演出
 * ドロー挙動（画面で停止してから手札へ）
 * 召喚挙動 （中央に拡大後エフェクトと共に場に出る）
 * 効果選択時は背景を薄暗くする　スペルは他の場所をクリックで中止して手札へ戻す
 * 選択時は効果をウィンドウで表示する
 * 敵スペル使用（回転しながら表示後停止、破棄）
 * 自分スペル使用（回転しながら表示後停止、破棄）
 * 敵の効果を表示パネル
 * 
 * 
 * フィールド
 * マップ（タイルマップ）
 * 何を配置するか
 * 
 * 
 * インゲーム
 * UIパーティクルの管理
 * 
 * 
 * カードを場に出した時の流れ
 * カードドロップ
 * 中央まで移動しながら一回転後拡大
 * エフェクトと共に場に出る
 * カードの効果発動
 * 
 * 
 * 
 * 
 * 
*/