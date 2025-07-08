using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class AttackedCard : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardController attacker = eventData.pointerDrag.GetComponent<CardController>();
        CardController defender = GetComponent<CardController>();
        if (attacker == null || defender == null)
        {
            return;
        }
        if (attacker.model.isPlayerCard == defender.model.isPlayerCard)
        {
            return;
        }
        //　シールドカード以外は攻撃できない
        CardController[] enemyFieldCards = GameManager.instance.GetEnemyFieldCards(attacker.model.isPlayerCard);
        if (Array.Exists(enemyFieldCards, card => card.model.ability == ABILITY.SHIELD) && defender.model.ability != ABILITY.SHIELD)
        {
            return;
        }

        if (attacker.model.canAttack)
        {
            GameManager.instance.CardsBattle(attacker, defender);
        }

    }
}
