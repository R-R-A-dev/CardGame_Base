using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropPlace : MonoBehaviour, IDropHandler
{
    public static CardController droppedCard;
    public enum TYPE
    {
        HAND,
        FIELD,
    }
    public TYPE type;
    public void OnDrop(PointerEventData eventData)
    {
        if (type == TYPE.HAND)
        {
            return;
        }
        if (GameManager.instance.isSummoning) return;
        CardController card = eventData.pointerDrag.GetComponent<CardController>();
        if (card != null)
        {
            if (!card.movement.isDraggable)
            {
                return;
            }
            CardController[] cards = GameManager.instance.GetFriendFieldCards(true);
            if (cards.Length != 0)
            {
                foreach (CardController c in cards)
                {
                    c.GetComponent<CanvasGroup>().blocksRaycasts = true;
                }
            }
            if (card.IsSpell)
            {
                droppedCard = card;
                //GameManager.instance.ReduceManaCost(droppedCard.model.cost, droppedCard.model.isPlayerCard);
                return;
            }
            if (card.model.isFieldCard)
            {
                return;
            }
            card.movement.defaultParent = this.transform;
            droppedCard = card;
            card.OnFiled();
            droppedCard.GetComponent<CanvasGroup>().blocksRaycasts = true;
            if (!(droppedCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND) ||
                droppedCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY)))
                droppedCard = null;

            if (droppedCard != null)
            {
                if ((droppedCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND) ||
                    droppedCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY)) && !droppedCard.CanUseAbilities())
                    droppedCard = null;
            }
        }
    }

}
