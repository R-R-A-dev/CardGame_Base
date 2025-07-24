using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardClickManager : MonoBehaviour, IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler
{

    public bool isClickable;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CardController descriptionCard = GetComponent<CardController>();
            GameManager.instance.showDescriptionClicked = true;
            
            GameManager.instance.uiManager.ShowDescriptionPanel(descriptionCard);

            //GameManager.instance.uiManager.CloseDescriptionPanel();
        }

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        GameManager.instance.isOnCard = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GameManager.instance.isOnCard = false;
    }
}

