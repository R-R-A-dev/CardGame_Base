using System.Collections.Generic;
using UnityEngine;

public class DeckBuilderManager : MonoBehaviour
{
    [SerializeField] public DeckBuilderUI deckBuilderUI;
    public int deckNum = 0;

    public static DeckBuilderManager Instance { get; private set; }

    void Awake() => Instance = this;
    private void Start()
    {

    }

    /// <summary>
    /// デッキへのカードの追加
    /// </summary>
    public void AddCardToDeck(OutGameCardList addCard,int cardNo,CardDragHandler cardDragHandler)
    {
        deckBuilderUI.AddDeckCard(addCard,cardNo, cardDragHandler);
    }

    /// <summary>
    /// デッキのカードの削除し一覧へ追加
    /// </summary>
    /// <param name="cardId"></param>
    public void RemoveCardFromDeck(OutGameCardList card, int cardNo)
    {
        deckBuilderUI.ReturnCardList(card,cardNo);
    }
}

