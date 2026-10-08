using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using static Coffee.UIExtensions.UIParticleAttractor;
using static UnityEngine.GraphicsBuffer;

public class SpellDropManager : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardController spellCard = eventData.pointerDrag.GetComponent<CardController>();
        CardController target = GetComponent<CardController>();

        if (spellCard.gameObject.GetComponent<CanvasGroup>().blocksRaycasts == true)
            return;
        if (spellCard == null) return;
        if (GameManager.instance.isSummoning) return;
        if (spellCard.CanUseSpells())
        {
            if (!GameManager.instance.isPlayerTurn) return;
            spellCard.movement.isDraggable = false;
            
            //spellCard.UseSpellTo(target);
            //分岐 effetselectの分岐
            if (spellCard.model.spells.HasFlag(SPELLS.EFFECT_SELECTION_ENEMY) ||
                spellCard.model.spells.HasFlag(SPELLS.EFFECT_SELECTION_FRIEND))
            {
                GameManager.instance.isEffectSelectPhase = true;
                GameManager.instance.DisableButtonCards();
                StartCoroutine(spellCard.movement.PlayerSelectMoveOn());
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.RANDOM_FRIEND) ||
                spellCard.model.spells.HasFlag(SPELLS.RANDOM_ENEMY))
            {
                //ランダム発動　移動アニメーション
                CastSpell(spellCard, WaitAndContinue(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) || spellCard.model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
            {
                CastSpell(spellCard, WaitSpell(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS) || spellCard.model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS))
            {
                CastSpell(spellCard, CardsEffect(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.REDUCE_HAND_COST) || spellCard.model.spells.HasFlag(SPELLS.INCREASE_ENEMY_COST))
            {
                CastSpell(spellCard, HandCardEffect(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.DESTROY_ALL_FIELD_CARDS))
            {
                CastSpell(spellCard, AllDestroy(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.DISCARD_ALL_FRIEND_HAND) || spellCard.model.spells.HasFlag(SPELLS.DISCARD_ALL_ENEMY_HAND))
            {
                CastSpell(spellCard, DiscardAll(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.DISCARD_ENEMY_HAND) || spellCard.model.spells.HasFlag(SPELLS.DISCARD_FRIEND_HAND))
            {
                CastSpell(spellCard, DiscardRandom(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.DRAW_CARDS))
            {
                CastSpell(spellCard, DrawCard(spellCard));
            }
        }

    }

    /// <summary>
    /// 対象選択のないスペルを発動する（左上への移動演出＋効果）。
    /// 移動演出(isSummoning)が終わってから効果(isAttacking)が始まるまでの間は
    /// どちらのフラグも立たず攻撃できてしまうため、カードが破棄されるまで使用中として扱う。
    /// </summary>
    void CastSpell(CardController spellCard, IEnumerator effect)
    {
        GameManager.instance.castingSpell = spellCard;
        StartCoroutine(spellCard.movement.MoveLeftSpell(spellCard));
        StartCoroutine(effect);
    }

    private IEnumerator DrawCard(CardController card)
    {
        card.transform.SetParent(card.transform.parent.parent);
        yield return new WaitForSeconds(0.9f);
        GameManager.instance.ReduceManaCost(card.model.cost, card.model.isPlayerCard);
        card.DrawCard(card);

    }

    private IEnumerator WaitSpell(CardController card)
    {
        yield return new WaitForSeconds(0.9f);
        GameManager.instance.AttackToHeroSpell(card);
        //card.spellEffect(card, true);
    }

    private IEnumerator AllDestroy(CardController card)
    {
        yield return new WaitForSeconds(0.9f);
        CardController[] enemyCards = GameManager.instance.GetEnemyFieldCards(card.model.isPlayerCard);
        CardController[] friendCards = GameManager.instance.GetFriendFieldCards(card.model.isPlayerCard);
        GameManager.instance.ReduceManaCost(card.model.cost, card.model.isPlayerCard);
        for (int i = 0; i < enemyCards.Length; i++)
        {
            card.spellEffect(enemyCards[i], true);
        }
        for (int i = 0; i < friendCards.Length; i++)
        {
            card.spellEffect(friendCards[i], true);
        }
    }

    private IEnumerator HandCardEffect(CardController card)
    {
        CardController[] targets;
        yield return new WaitForSeconds(0.9f);
        GameManager.instance.ReduceManaCost(card.model.cost, card.model.isPlayerCard);
        if (card.model.spells.HasFlag(SPELLS.REDUCE_HAND_COST))
        {
            targets = GameManager.instance.GetFriendHandTransform(card.model.isPlayerCard);
            for (int i = 0; i < targets.Length; i++)
            {
                card.spellEffect(targets[i], true);
            }
        }
        else if (card.model.spells.HasFlag(SPELLS.INCREASE_ENEMY_COST))
        {
            targets = GameManager.instance.GetEnemyHandTransform(card.model.isPlayerCard);
            for (int i = 0; i < targets.Length; i++)
            {
                card.spellEffect(targets[i], true);
            }
        }
    }

    private IEnumerator DiscardAll(CardController card)
    {
        yield return new WaitForSeconds(0.9f);
        GameManager.instance.ReduceManaCost(card.model.cost, card.model.isPlayerCard);
        if (card.model.spells.HasFlag(SPELLS.DISCARD_ALL_ENEMY_HAND))
        {
            CardController[] targets = GameManager.instance.GetEnemyHandTransform(card.model.isPlayerCard);
            for (int i = 0; i < targets.Length; i++)
            {
                card.spellEffect(targets[i], true);
            }
        }
        else if (card.model.spells.HasFlag(SPELLS.DISCARD_ALL_FRIEND_HAND))
        {
            CardController[] targets = GameManager.instance.GetFriendHandTransform(card.model.isPlayerCard);
            for (int i = 0; i < targets.Length; i++)
            {
                card.spellEffect(targets[i], true);
            }
        }
    }

    private IEnumerator DiscardRandom(CardController card)
    {
        yield return new WaitForSeconds(0.9f);
        GameManager.instance.ReduceManaCost(card.model.cost, card.model.isPlayerCard);
        if (card.model.spells.HasFlag(SPELLS.DISCARD_ENEMY_HAND))
        {
            CardController[] targets = GameManager.instance.GetEnemyHandTransform(card.model.isPlayerCard);
            CardController target = targets[UnityEngine.Random.Range(0, targets.Length)];
            card.spellEffect(target, true);
        }
        else if (card.model.spells.HasFlag(SPELLS.DISCARD_FRIEND_HAND))
        {
            CardController[] targets = GameManager.instance.GetFriendHandTransform(card.model.isPlayerCard);
            CardController target = targets[UnityEngine.Random.Range(0, targets.Length)];
            card.spellEffect(target, true);
        }
    }

    private IEnumerator CardsEffect(CardController card)
    {
        CardController[] targets;
        yield return new WaitForSeconds(0.9f);
        GameManager.instance.ReduceManaCost(card.model.cost, card.model.isPlayerCard);
        if (card.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS))
        {
            targets = GameManager.instance.GetEnemyFieldCards(card.model.isPlayerCard);
            for (int i = 0; i < targets.Length; i++)
            {
                card.spellEffect(targets[i], true);
            }
        }
        else if (card.model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS))
        {
            targets = GameManager.instance.GetFriendFieldCards(card.model.isPlayerCard);
            for (int i = 0; i < targets.Length; i++)
            {
                card.spellEffect(targets[i], true);
            }
        }
    }
    private IEnumerator WaitAndContinue(CardController card)
    {
        yield return new WaitForSeconds(0.9f);
        CardController target = new CardController();
        CardController[] cards = null;
        GameManager.instance.ReduceManaCost(card.model.cost, card.model.isPlayerCard);
        if (card.model.spells.HasFlag(SPELLS.RANDOM_FRIEND))
        {
            cards = GameManager.instance.GetFriendFieldCards(card.model.isPlayerCard);
        }
        else if (card.model.spells.HasFlag(SPELLS.RANDOM_ENEMY))
        {
            cards = GameManager.instance.GetEnemyFieldCards(card.model.isPlayerCard);
        }
        target = cards[UnityEngine.Random.Range(0, cards.Length)];
        card.spellEffect(target, true);
    }
}
