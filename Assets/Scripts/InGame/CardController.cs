using Coffee.UIExtensions;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
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

    GameManager gameManager;

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
        view.SetCard(model);
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
        model.Destroy(enemyCard);
    }

    public void EffectAttack(CardController enemyCard)
    {
        model.EffectDmg(enemyCard);
    }

    public void Steal(CardController target)
    {
        model.Steal(target);
    }


    public void DrawCard(CardController card)
    {
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
        model.EffectHeal(friendCard);
        friendCard.RefreshView();
    }

    public void Heal(CardController friendCard)
    {
        model.Heal(friendCard);
        friendCard.RefreshView();
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
        target.model.at -= card.model.effectDmg;
        if (target.model.at < 1)
        {
            target.model.at = 1;
        }
    }
    public void AttackBuff(CardController card, CardController target)
    {
        target.model.at += card.model.effectDmg;
    }

    public void RefreshView()
    {
        view.Refresh(model);
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
        OnFiledAbilities();
    }

    public void OnFiledAbilities()
    {
        SetAbility(this);
        if (gameManager.isPlayerTurn)
        {
            if (model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND) || model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY))
            {
                Debug.Log(CanUseAbilities());
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
        yield return new WaitForSeconds(1.5f);
        CardController target = null;
        CardController[] targets = null;
        Transform movePosition = null;

        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARDS))
            targets = gameManager.GetEnemyFieldCards(model.isPlayerCard);

        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARDS))
            targets = gameManager.GetFriendFieldCards(model.isPlayerCard);

        if (model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST) || model.abilities.HasFlag(ABILITIES.DISCARD_ALL_FRIEND_HAND))
            targets = gameManager.GetFriendHandTransform(model.isPlayerCard);

        if (model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST) || model.abilities.HasFlag(ABILITIES.DISCARD_ALL_ENEMY_HAND))
            targets = gameManager.GetEnemyHandTransform(model.isPlayerCard);

        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO) || model.abilities.HasFlag(ABILITIES.HEAL_BY_DAMAGE))
        {
            movePosition = gameManager.enemyHero;
            attackSpellEffectHero(movePosition, true);
        }

        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO))
        {
            movePosition = gameManager.playerHero;
            attackSpellEffectHero(movePosition, true);
        }

        if (model.abilities.HasFlag(ABILITIES.DISCARD_ENEMY_HAND))
        {
            CardController[] enemyCards = gameManager.GetEnemyHandTransform(this.model.isPlayerCard);
            target = enemyCards[UnityEngine.Random.Range(0, enemyCards.Length - 1)];
        }

        if (model.abilities.HasFlag(ABILITIES.DISCARD_FRIEND_HAND))
        {
            CardController[] friendCards = gameManager.GetFriendHandTransform(this.model.isPlayerCard);
            target = friendCards[UnityEngine.Random.Range(0, friendCards.Length - 1)];
        }

        if (model.abilities.HasFlag(ABILITIES.RANDOM_ENEMY))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            target = enemyCards[UnityEngine.Random.Range(0, enemyCards.Length - 1)];
        }

        if (model.abilities.HasFlag(ABILITIES.RANDOM_FRIEND))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            target = friendCards[UnityEngine.Random.Range(0, friendCards.Length - 1)];
        }


        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARDS) || model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARDS) ||
            model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST) || model.abilities.HasFlag(ABILITIES.DISCARD_ALL_ENEMY_HAND) ||
            model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST) || model.abilities.HasFlag(ABILITIES.DISCARD_ALL_FRIEND_HAND))
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
        }
        else if (model.abilities.HasFlag(ABILITIES.RANDOM_ENEMY) || model.abilities.HasFlag(ABILITIES.RANDOM_FRIEND) ||
                 model.abilities.HasFlag(ABILITIES.DISCARD_ENEMY_HAND) || model.abilities.HasFlag(ABILITIES.DISCARD_FRIEND_HAND))
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
        }
    }

    public void SetAbility(CardController card)
    {
        if (card.model.abilities.HasFlag(ABILITIES.INIT_ATTACKABLE))
        {
            SetCanAttack(true);
        }
        if (card.model.abilities.HasFlag(ABILITIES.DAMAGE_NULLIFY_ONCE))
        {
            card.model.isDamageNullifyOnce = true;
        }
        if (card.model.abilities.HasFlag(ABILITIES.DOUBLE_ACTION))
        {
            card.model.isDoubleAction = true;
        }
        if (card.model.abilities.HasFlag(ABILITIES.STATS_UP_ON_ATTACK))
        {
            card.model.isStatsUpOnAttack = true;
        }
        if (card.model.abilities.HasFlag(ABILITIES.DESTROY_ATTACKED_TARGET))
        {
            card.model.isDestroyer = true;
        }
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
            target.CheckAlive();
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
            target.CheckAlive();
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
            target.CheckAlive();
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
        if (model.abilities.HasFlag(ABILITIES.STEAL_ENEMY_CARD))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0 && friendCards.Length <= 4)
            {
                canUse = true;
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
            CardController[] handCards = gameManager.GetFriendHandTransform(this.model.isPlayerCard);
            if (handCards.Length > 1)
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

    public void CheckAlive()
    {
        if (model.isAlive)
        {
            RefreshView();
        }
        else
        {
            if (this == null) return;
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
            target.CheckAlive();
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
            target.CheckAlive();
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
            target.CheckAlive();
        }
        if (model.spells.HasFlag(SPELLS.RANDOM_DAMAGE))
        {
            if (target == null)
            {
                CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
                target = enemyCards[UnityEngine.Random.Range(0, enemyCards.Length - 1)];
            }
            EffectAttack(target);
            target.CheckAlive();
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
        GameManager.instance.isAttacking = !isDefense;
        CheckAttackParticle(effect);
        model.Attack(enemy);
        enemy.RefreshView();
        effect.SetParent(GameManager.instance.uiParticlesManager.transform);
        GameObject textObj = GameManager.instance.GetTextPool();
        StartCoroutine(GameManager.instance.GenDamageText(textObj, model.at, targetPos));

    }

    public void DirectAttack(Transform effect, Transform endPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.DOMove(endPos.position, attackTime)
            .OnComplete(() =>
            {
                hitEffect(endPos);
                GameManager.instance.isAttacking = !isDefense;
                CheckAttackParticle(effect);
                model.Attack(enemy);
                enemy.RefreshView();
                effect.SetParent(GameManager.instance.uiParticlesManager.transform);
                GameObject textObj = GameManager.instance.GetTextPool();
                StartCoroutine(GameManager.instance.GenDamageText(textObj, model.at, endPos));
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
                    hitEffect(targetPos);
                    GameManager.instance.isAttacking = !isDefense;
                    CheckAttackParticle(target);
                    model.Attack(enemyCC);
                    enemyCC.RefreshView();
                    target.SetParent(GameManager.instance.uiParticlesManager.transform);
                    GameObject textObj = GameManager.instance.GetTextPool();
                    StartCoroutine(GameManager.instance.GenDamageText(textObj, model.at, targetPos));
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
        UseAbilitiesTo(enemy);
        enemy.RefreshView();
        effect.SetParent(GameManager.instance.uiParticlesManager.transform);
        if (model.effectDmg != 0)
        {
            GameObject textObj = GameManager.instance.GetTextPool();
            StartCoroutine(GameManager.instance.GenDamageText(textObj, model.effectDmg, targetPos));
        }


    }
    public void DirectAttackAbility(Transform effect, Transform endPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.DOMove(endPos.position, attackTime)
            .OnComplete(() =>
            {
                hitEffect(endPos);
                GameManager.instance.isAttacking = !isDefense;
                CheckAttackParticle(effect);
                UseAbilitiesTo(enemy);
                enemy.RefreshView();
                effect.SetParent(GameManager.instance.uiParticlesManager.transform);
                if (model.effectDmg != 0)
                {
                    GameObject textObj = GameManager.instance.GetTextPool();
                    StartCoroutine(GameManager.instance.GenDamageText(textObj, model.effectDmg, endPos));
                }
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
        Transform targetPos = target;
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
                    UseAbilitiesTo(enemyCC);
                    enemyCC.RefreshView();
                    target.SetParent(GameManager.instance.uiParticlesManager.transform);
                    if (model.effectDmg != 0)
                    {
                        GameObject textObj = GameManager.instance.GetTextPool();
                        StartCoroutine(GameManager.instance.GenDamageText(textObj, model.effectDmg, targetPos));
                    }
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

    //スペルでのヒーローへの攻撃
    public void attackSpellEffectHero(Transform target, bool isDefense)
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
        GameManager.instance.isAttacking = !isDefense;
        CheckAttackParticle(effect);
        if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO))
        {
            if (model.isPlayerCard)
            {
                gameManager.enemy.heroHp -= model.effectDmg;
            }
            else
            {
                gameManager.player.heroHp -= model.effectDmg;
            }
        }
        else if (model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
        {
            if (model.isPlayerCard)
            {
                gameManager.player.heroHp += model.effectDmg;
            }
            else
            {
                gameManager.enemy.heroHp += model.effectDmg;
            }
        }
        Transform targetHero = null;
        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO) && model.isPlayerCard ||
                       model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO) && model.isPlayerCard)
            targetHero = gameManager.playerHero;
        else
            targetHero = gameManager.enemyHero;

        GameObject textObj = GameManager.instance.GetTextPool();
        GameManager.instance.StartCoroutine(GameManager.instance.GenDamageText(textObj, model.effectDmg, targetHero));

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
                if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO))
                {
                    if (model.isPlayerCard)
                    {
                        gameManager.enemy.heroHp -= model.effectDmg;
                    }
                    else
                    {
                        gameManager.player.heroHp -= model.effectDmg;
                    }
                }
                else if (model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
                {
                    if (model.isPlayerCard)
                    {
                        gameManager.player.heroHp += model.effectDmg;
                    }
                    else
                    {
                        gameManager.enemy.heroHp += model.effectDmg;
                    }
                }
                Transform targetHero = null;
                if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO) && model.isPlayerCard || model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO) && model.isPlayerCard ||
                    model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO) && !model.isPlayerCard || model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) && !model.isPlayerCard)
                    targetHero = gameManager.playerHero;
                else if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO) && !model.isPlayerCard || model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO) && !model.isPlayerCard ||
                    model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO) && model.isPlayerCard || model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) && model.isPlayerCard)
                    targetHero = gameManager.enemyHero;

                GameObject textObj = GameManager.instance.GetTextPool();
                GameManager.instance.StartCoroutine(GameManager.instance.GenDamageText(textObj, model.effectDmg, targetHero));

                gameManager.uiManager.ShowHeroHP(gameManager.player.heroHp, gameManager.enemy.heroHp);
                GameManager.instance.CheckHeroHP();
                effect.SetParent(GameManager.instance.uiParticlesManager.transform);
                Destroy(this.gameObject);
                if (model.abilities.HasFlag(ABILITIES.NONE))
                {
                    return;
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
                    if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) ||
                        model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO))
                    {
                        if (model.isPlayerCard)
                        {
                            gameManager.enemy.heroHp -= model.effectDmg;
                        }
                        else
                        {
                            gameManager.player.heroHp -= model.effectDmg;
                        }
                    }
                    else if (model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO) ||
                        model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO))
                    {
                        if (model.isPlayerCard)
                        {
                            gameManager.player.heroHp += model.effectDmg;
                        }
                        else
                        {
                            gameManager.enemy.heroHp += model.effectDmg;
                        }
                    }
                    Transform targetHero = null;
                    if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO) && model.isPlayerCard || model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO) && model.isPlayerCard ||
                        model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO) && !model.isPlayerCard || model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) && !model.isPlayerCard)
                        targetHero = gameManager.playerHero;
                    else if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO) && !model.isPlayerCard || model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO) && !model.isPlayerCard ||
                        model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO) && model.isPlayerCard || model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) && model.isPlayerCard)
                        targetHero = gameManager.enemyHero;

                    GameObject textObj = GameManager.instance.GetTextPool();
                    StartCoroutine(GameManager.instance.GenDamageText(textObj, model.effectDmg, targetHero));

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


    public void DirectSpellAttack(Transform effect, Transform endPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.DOMove(endPos.position, attackTime)
            .OnComplete(() =>
            {
                GameManager.instance.isAttacking = !isDefense;
                //model.Attack(enemy);
                CheckAttackParticle(effect);
                UseSpellTo(enemy);
                enemy.RefreshView();
                effect.SetParent(GameManager.instance.uiParticlesManager.transform);
                if (model.effectDmg != 0)
                {
                    GameObject textObj = GameManager.instance.GetTextPool();
                    GameManager.instance.StartCoroutine(GameManager.instance.GenDamageText(textObj, model.effectDmg, endPos));
                }
            });
    }

    public IEnumerator SpawnSpellEffect(Transform effect, Transform targetPos, CardController enemy, bool isDefense, float attackTime)
    {
        effect.position = targetPos.position;
        yield return new WaitForSeconds(attackTime);
        GameManager.instance.isAttacking = !isDefense;
        //model.Attack(enemy);
        UseSpellTo(enemy);
        CheckAttackParticle(effect);
        if (model.effectDmg != 0)
        {
            GameObject textObj = GameManager.instance.GetTextPool();
            GameManager.instance.StartCoroutine(GameManager.instance.GenDamageText(textObj, model.at, targetPos));
        }
        enemy.RefreshView();
        effect.SetParent(GameManager.instance.uiParticlesManager.transform);
    }

    public void hitEffect(Transform target)
    {
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