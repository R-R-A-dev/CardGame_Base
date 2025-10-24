using UnityEngine;
using UnityEngine.EventSystems;

public class CardClickHandler : MonoBehaviour, IPointerDownHandler
{
    public bool selected = false;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] CardController card;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left &&
            GameManager.instance.isCardChange && card.model.isPlayerCard)
        {
            //手札交換の処理
            if (!selected)
            {
                canvasGroup.alpha = 0.5f;
                CardController card = GetComponent<CardController>();
                GameManager.instance.changedCardList.Add(card);
            }
            else
            {
                canvasGroup.alpha = 1f;
                CardController card = GetComponent<CardController>();
                GameManager.instance.changedCardList.Remove(card);
            }
            selected = !selected;
        }
    }


}
