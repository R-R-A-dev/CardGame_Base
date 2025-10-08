using UnityEngine;
using UnityEngine.EventSystems;

public class DeckDropZone : MonoBehaviour, IDropHandler
{
    CardDragHandler cardDragHandler;
    public void OnDrop(PointerEventData eventData)
    {
        OutGameCardList card = eventData.pointerDrag.GetComponent<OutGameCardList>();
        if (card != null)
        {
            GameObject droppedCard = card.GetComponent<CardDragHandler>().GetHoldCard();
            cardDragHandler = droppedCard.GetComponent<CardDragHandler>();
            if (cardDragHandler != null && cardDragHandler.isDeck == false)
            {
                //一覧からデッキへドロップ
                cardDragHandler.isDeck = true;
                DeckBuilderManager.Instance.AddCardToDeck(droppedCard);
            }
            else if (cardDragHandler != null && cardDragHandler.isDeck == true)
            {
                //デッキからデッキへドロップ
                DeckBuilderManager.Instance.deckBuilderUI.PoolCard(droppedCard);
                //オブジェクトプール
            }

        }
    }
}
