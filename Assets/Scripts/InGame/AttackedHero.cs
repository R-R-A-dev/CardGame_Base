using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class AttackedHero : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 他のカードの攻撃中・スペル使用中などは攻撃させない（AttackedCard.OnDrop()と同じ理由）
        if (GameManager.instance.IsPlayerActionLocked) return;

        CardController attacker = eventData.pointerDrag.GetComponent<CardController>();
        if (attacker == null)
        {
            return;
        }
        if (!attacker.model.abilities.HasFlag(ABILITIES.PIERCE))
        {
            //　敵フィールドにシールドカードがあれば攻撃できない
            CardController[] enemyFieldCards = GameManager.instance.GetEnemyFieldCards(attacker.model.isPlayerCard);
            if (Array.Exists(enemyFieldCards, card => card.model.abilities.HasFlag(ABILITIES.SHIELD)))
            {
                return;
            }
        }

        if (attacker.model.canAttack)
        {
            GameManager.instance.AttackToHero(attacker);
            GameManager.instance.CheckHeroHP();
        }

    }
}
