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
            else if (spellCard.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO)|| spellCard.model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
            {
                StartCoroutine(spellCard.movement.MoveLeftSpell(spellCard));
                StartCoroutine(WaitSpell(spellCard));
            }
        }
    }

    private IEnumerator WaitSpell(CardController card)
    {
        yield return new WaitForSeconds(0.9f);
        GameManager.instance.AttackToHeroSpell(card);
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

        target = cards[UnityEngine.Random.Range(0, cards.Length - 1)];
        Debug.Log(target);
        card.spellEffect(target, true);
    }
}
