using System.Collections.Generic;
using UnityEngine;

public class DeckBuilderManager : MonoBehaviour
{
    [SerializeField] public DeckBuilderUI deckBuilderUI;

    public static DeckBuilderManager Instance { get; private set; }

    void Awake() => Instance = this;
    private void Start()
    {

    }

    /// <summary>
    /// デッキへのカードの追加
    /// </summary>
    public void AddCardToDeck(GameObject addCard)
    {
        deckBuilderUI.AddDeckCard(addCard);
    }

    /// <summary>
    /// デッキへのカードの削除
    /// </summary>
    /// <param name="cardId"></param>
    void RemoveCardFromDeck(int cardId)
    {

    }
}

