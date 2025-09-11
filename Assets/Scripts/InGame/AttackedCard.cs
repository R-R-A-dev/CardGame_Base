using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AttackedCard : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardController attacker = eventData.pointerDrag.GetComponent<CardController>();
        CardController defender = GetComponent<CardController>();
        if (attacker == null || defender == null||!defender.model.isFieldCard)
        {
            return;
        }
        if (attacker.model.isPlayerCard == defender.model.isPlayerCard)
        {
            return;
        }
        if (!attacker.model.abilities.HasFlag(ABILITIES.PIERCE))
        {
            
            //　シールドカード以外は攻撃できない
            CardController[] enemyFieldCards = GameManager.instance.GetEnemyFieldCards(attacker.model.isPlayerCard);
            if (Array.Exists(enemyFieldCards, card => card.model.abilities.HasFlag(ABILITIES.SHIELD)) && !defender.model.abilities.HasFlag(ABILITIES.SHIELD))
            {
                return;
            }
        }
        if (attacker.model.canAttack)
        {
            BezierArrows.Instance.Hide();
            StartCoroutine(GameManager.instance.CardsBattle(attacker, defender));
        }

    }
}
