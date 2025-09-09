using DG.Tweening.Core.Easing;
using System;
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

            }
        }

    }
}
