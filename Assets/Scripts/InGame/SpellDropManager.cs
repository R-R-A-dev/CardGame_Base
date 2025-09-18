using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using static Coffee.UIExtensions.UIParticleAttractor;

public class SpellDropManager : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardController spellCard = eventData.pointerDrag.GetComponent<CardController>();
        CardController target = GetComponent<CardController>();

        if (spellCard == null)
        {
            return;
        }
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
                StartCoroutine(spellCard.movement.MoveLeftSpell(spellCard));
                StartCoroutine(WaitAndContinue(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) || spellCard.model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
            {
                StartCoroutine(spellCard.movement.MoveLeftSpell(spellCard));
                StartCoroutine(WaitSpell(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS) || spellCard.model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS))
            {
                StartCoroutine(spellCard.movement.MoveLeftSpell(spellCard));
                StartCoroutine(CardsEffect(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.REDUCE_HAND_COST) || spellCard.model.spells.HasFlag(SPELLS.INCREASE_ENEMY_COST))
            {
                StartCoroutine(spellCard.movement.MoveLeftSpell(spellCard));
                StartCoroutine(HandCardEffect(spellCard));
            }
            else if (spellCard.model.spells.HasFlag(SPELLS.DESTROY_ALL_FIELD_CARDS))
            {
                StartCoroutine(spellCard.movement.MoveLeftSpell(spellCard));
                StartCoroutine(AllDestroy(spellCard));
            }
        }
        
    }

    private IEnumerator WaitSpell(CardController card)
    {
        yield return new WaitForSeconds(0.9f);
        GameManager.instance.AttackToHeroSpell(card);
    }

    private IEnumerator AllDestroy(CardController card)
    {
        yield return new WaitForSeconds(0.9f);
        CardController[] enemyCards = GameManager.instance.GetEnemyFieldCards(card.model.isPlayerCard);
        CardController[] friendCards = GameManager.instance.GetFriendFieldCards(card.model.isPlayerCard);
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

    private IEnumerator CardsEffect(CardController card)
    {
        CardController[] targets;
        yield return new WaitForSeconds(0.9f);
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
