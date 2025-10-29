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
            if (card.GetComponent<CardDragHandler>().test)
            {
                card.GetComponent<CardDragHandler>().test = false;
                return;
            }
            GameObject droppedCard = card.GetComponent<CardDragHandler>().GetHoldCard();
            if (droppedCard == null) return;
            cardDragHandler = droppedCard.GetComponent<CardDragHandler>();
            cardDragHandler.dropSuccess = true;
            if (cardDragHandler != null && cardDragHandler.isDeck == false)
            {
                //一覧からデッキへドロップ
                OutGameCardList outGameCardList = droppedCard.GetComponent<OutGameCardList>();
                int cardNum = outGameCardList.No;
                DeckBuilderManager.Instance.AddCardToDeck(outGameCardList, cardNum, cardDragHandler);
                cardDragHandler.isDeck = true;
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
