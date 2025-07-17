using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class CardController : MonoBehaviour
{
    CardView view;                 // 見かけ(view)に関することを操作
    public CardModel model;        // データ(model)に関することを操作
    public CardMovement movement;  // 移動(movement)に関することを操作

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
        gameManager = GameManager.instance;
    }

    public void Init(int cardID, bool isPlayer)
    {
        model = new CardModel(cardID, isPlayer);
        view.SetCard(model);
    }

    public void Attack(CardController enemyCard)
    {
        model.Attack(enemyCard);
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
    }

    public void Defense(CardController enemyCard)
    {
        model.Attack(enemyCard);
        SetCanAttack(false);
    }

    public void Destroy(CardController enemyCard)
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


    public void DrawCard()
    {
        for (int i = 0; i < 2; i++)
        {
            GameManager.instance.DrawCard(model.isPlayerCard);
        }
    }

    public void CardToHand()
    {

    }

    public void SummonCard(CardController card)
    {
        Debug.Log(card.model.targetCards[0].name);
        Debug.Log(card.model.targetCards[1].at);
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
        CardController[] handCards = gameManager.GetFriendHandTransform(card.model.isPlayerCard);
        foreach (CardController handCard in handCards)
        {
            handCard.model.ReduceHandCost(card.model.at);
            handCard.RefreshView();
        }

    }

    public void IncreaseEnemyHandCost(CardController card)
    {
        CardController[] enemyHandCards = gameManager.GetEnemyHandTransform(card.model.isPlayerCard);
        foreach (CardController enemyHandCard in enemyHandCards)
        {
            enemyHandCard.model.IncreaseEnemyHandCost(card.model.at);
            enemyHandCard.RefreshView();
        }
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
        gameManager.ReduceManaCost(model.cost, model.isPlayerCard);
        model.isFieldCard = true;
        OnFiledAbilities();
    }

    public void OnFiledAbilities()
    {
        if (model.abilities.HasFlag(ABILITIES.INIT_ATTACKABLE))
        {
            SetCanAttack(true);
        }
        if (gameManager.isPlayerTurn)
        {
            if (model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION))
            {
                if (CanUseAbilities())
                {
                    gameManager.isEffectSelectPhase = true;
                    gameManager.DisableButtonCards();
                }
            }
            else if (CanUseAbilities())
            {
                UseAbilitiesTo();
            }
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
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            foreach (CardController enemyCard in enemyCards)
            {
                EffectAttack(enemyCard);
            }
            foreach (CardController enemyCard in enemyCards)
            {
                enemyCard.CheckAlive();
            }
        }
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO))
        {
            gameManager.AttackToHero(this);
            gameManager.CheckHeroHP();
        }
        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO))
        {
            gameManager.HealToHero(this);
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
            CardController[] friendsCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            foreach (CardController friendCard in friendsCards)
            {
                EffectHeal(friendCard);
            }
            EffectHeal(this);
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
            Destroy(target);
            target.CheckAlive();
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
            DrawCard();
        }
        if (model.abilities.HasFlag(ABILITIES.SUMMON_SPECIFIC_UNIT))
        {
            SummonCard(this);
        }
        if (model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST))
        {
            CardController[] handCards = gameManager.GetFriendHandTransform(this.model.isPlayerCard);
            if (handCards.Length > 0)
            {
                ReduceHandCost(this);
            }
            return;
        }
        if (model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST))
        {
            CardController[] enemyHandCards = gameManager.GetEnemyHandTransform(this.model.isPlayerCard);
            if (enemyHandCards.Length > 0)
            {
                IncreaseEnemyHandCost(this);
            }
            return;
        }
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_NULLIFY_ONCE))
        {
            model.isDamageNullifyOnce = true;
        }
        if (model.abilities.HasFlag(ABILITIES.DOUBLE_ACTION))
        {
            model.isDoubleAction = true;
        }
    }

    public bool CanUseAbilities()
    {
        bool canUse = false;
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARD) || model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARDS)
            || model.abilities.HasFlag(ABILITIES.DESTROY_ENEMY_CARD) || model.abilities.HasFlag(ABILITIES.STEAL_ENEMY_CARD))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                canUse = true;
            }
            canUse = false;
        }
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO) || model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO) || model.abilities.HasFlag(ABILITIES.DRAW_CARDS))
        {
            canUse = true;
        }
        if (model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARD) || model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARDS))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            canUse = true;
        }
        if (model.abilities.HasFlag(ABILITIES.SUMMON_SPECIFIC_UNIT))
        {
            canUse = true;
        }
        if (model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST))
        {
            CardController[] handCards = gameManager.GetFriendHandTransform(this.model.isPlayerCard);
            if (handCards.Length > 0)
            {
                canUse = true;
            }
            canUse = false;
        }
        if (model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST))
        {
            CardController[] enemyHandCards = gameManager.GetEnemyHandTransform(this.model.isPlayerCard);
            if (enemyHandCards.Length > 0)
            {
                canUse = true;
            }
            return false;
        }
        if (model.abilities.HasFlag(ABILITIES.DAMAGE_NULLIFY_ONCE))
        {
            canUse = true;
        }
        if (model.abilities.HasFlag(ABILITIES.DOUBLE_ACTION))
        {
            canUse = true;
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
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            foreach (CardController enemyCard in enemyCards)
            {
                EffectAttack(enemyCard);
            }
            foreach (CardController enemyCard in enemyCards)
            {
                enemyCard.CheckAlive();
            }
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
            CardController[] friendsCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            foreach (CardController friendCard in friendsCards)
            {
                EffectHeal(friendCard);
            }
        }
        if (model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
        {
            gameManager.HealToHero(this);
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
        if (model.spells.HasFlag(SPELLS.STEAL_ENEMY_CARD))
        {
            DrawCard();
        }
            gameManager.ReduceManaCost(model.cost, model.isPlayerCard);
            Destroy(this.gameObject);
        }
/*    public bool CanUseSpell()
    {
        switch (model.spells)
        {
            case SPELL.DAMAGE_ENEMY_CARD:
            case SPELL.DAMAGE_ENEMY_CARDS:
            case SPELL.STEAL_ENEMY_CARD:
                CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
                if (enemyCards.Length > 0)
                {
                    return true;
                }
                return false;
            case SPELL.DAMAGE_ENEMY_HERO:
            case SPELL.HEAL_FRIEND_HERO:
            case SPELL.DRAW_CARDS:
                return true;
            case SPELL.HEAL_FRIEND_CARD:
            case SPELL.HEAL_FRIEND_CARDS:
                CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
                if (friendCards.Length > 0)
                {
                    return true;
                }
                return false;
            case SPELL.NONE:
                return false;
        }
        return false;
    }*/

    public bool CanUseSpells()
    {
        bool canUse = false;
        if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARD) || model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS)
            || model.spells.HasFlag(SPELLS.DESTROY_ENEMY_CARD) || model.spells.HasFlag(SPELLS.STEAL_ENEMY_CARD))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(this.model.isPlayerCard);
            if (enemyCards.Length > 0)
            {
                canUse = true;
            }
        }
        if (model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) || model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO) || model.spells.HasFlag(SPELLS.DRAW_CARDS))
        {
            canUse = true;
        }
        if (model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARD) || model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(this.model.isPlayerCard);
            canUse = true;
        }
        if (model.spells.HasFlag(SPELLS.SUMMON_SPECIFIC_UNIT))
        {
            canUse = true;
        }
        if (model.spells.HasFlag(SPELLS.REDUCE_HAND_COST))
        {
            CardController[] handCards = gameManager.GetFriendHandTransform(this.model.isPlayerCard);
            if (handCards.Length > 0)
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
        return canUse;
    }
}
