using System.Collections.Generic;
using UnityEngine;

public class DeckBuilderManager : MonoBehaviour
{
    [SerializeField] public DeckBuilderUI deckBuilderUI;
    [SerializeField] public DeckStatisticsUI deckStatisticsUI;
    [SerializeField] public CardDetailUI cardDetailUI;
    public int deckNum = 0;

    public static DeckBuilderManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        dataSet();
    }
    private void Start()
    {

    }

    /// <summary>
    /// デッキへのカードの追加
    /// </summary>
    public void AddCardToDeck(OutGameCardList addCard, int cardNo, CardDragHandler cardDragHandler)
    {
        deckBuilderUI.AddDeckCard(addCard, cardNo, cardDragHandler);
        DeckBuilderManager.Instance.deckStatisticsUI.
    RefreshStatistics(DeckBuilderManager.Instance.deckNum);
    }

    /// <summary>
    /// デッキのカードの削除し一覧へ追加
    /// </summary>
    /// <param name="cardId"></param>
    public void RemoveCardFromDeck(OutGameCardList card, int cardNo)
    {
        deckBuilderUI.ReturnCardList(card, cardNo);
        DeckBuilderManager.Instance.deckStatisticsUI.
    RefreshStatistics(DeckBuilderManager.Instance.deckNum);
    }

    void dataSet()
    {
        CardEntity[] entitys = CardDatabase.LoadAllCards();
        for (int i = 0; i < entitys.Length; i++)
        {
            CardListData.Entities.Add(entitys[i]);
        }
    }
}

