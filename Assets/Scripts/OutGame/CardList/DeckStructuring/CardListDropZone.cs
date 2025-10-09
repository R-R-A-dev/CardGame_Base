using UnityEngine;
using UnityEngine.EventSystems;

public class CardListDropZone : MonoBehaviour, IDropHandler
{
    CardDragHandler cardDragHandler;
    public void OnDrop(PointerEventData eventData)
    {
        OutGameCardList card = eventData.pointerDrag.GetComponent<OutGameCardList>();
        if (card != null)
        {
            GameObject droppedCard = card.GetComponent<CardDragHandler>().GetHoldCard();
            if(droppedCard == null) return;
            cardDragHandler = droppedCard.GetComponent<CardDragHandler>();
            if (cardDragHandler != null && cardDragHandler.isDeck == false)
            {
                //一覧から一覧へドロップ
                DeckBuilderManager.Instance.deckBuilderUI.PoolCard(droppedCard);
                //オブジェクトプール
            }
            else if (cardDragHandler != null && cardDragHandler.isDeck == true)
            {
                DeckBuilderManager.Instance.deckBuilderUI.PoolCard(droppedCard);
                //デッキから一覧へドロップ
                //オブジェクトプール
            }

        }
    }
}
