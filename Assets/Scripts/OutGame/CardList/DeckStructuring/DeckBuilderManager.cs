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
    public void AddCardToDeck(OutGameCardList addCard,int cardNo)
    {
        deckBuilderUI.AddDeckCard(addCard,cardNo);
    }

    /// <summary>
    /// デッキのカードの削除し一覧へ追加
    /// </summary>
    /// <param name="cardId"></param>
    void RemoveCardFromDeck(int cardId)
    {

    }
}

