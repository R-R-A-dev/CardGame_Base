using UnityEngine;
using UnityEngine.EventSystems;

public class CardClickManager : MonoBehaviour, IPointerClickHandler
{

    public bool isClickable;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!GameManager.instance.isEffectSelectPhase) return;
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            CardController selectedCard = GetComponent<CardController>();
            if (selectedCard != null)
            {
                CardController droppedCard = DropPlace.droppedCard;
                if (droppedCard.CanUseAbilities())
                {
                    droppedCard.UseAbilitiesTo(selectedCard); 
                    GameManager.instance.isEffectSelectPhase = false;
                    GameManager.instance.EnableButtonCards();
                }
            }
        }
    }
}
