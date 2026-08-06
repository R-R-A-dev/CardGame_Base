using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        SaveData saveData = SaveManager.LoadOrInitialize();
        List<int> ownedCardIds = SaveManager.Load().ownedCardCounts;
        // 例: 総所持数で初期化
        CardListData.PossessionCard = ownedCardIds;
    }
    //TODO：ゲーム内のデータの管理クラスを持つ、セーブデータを持つ方
    //TODO：ガチャの実装

    private void Start()
    {

    }



    private void OnEnable()
    {
        deckBuilderUI.StartDeckEdit(deckNum);
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

    public void OnDropZone()
    {
        deckBuilderUI.OnDropZone();
    }

    public void OffDropZone()
    {
        deckBuilderUI.OffDropZone();
    }

    public void GameStart()
    {
        GameSession.SelectedDeck = deckBuilderUI.GetDeck();
        for (int i = 0; i < GameSession.SelectedDeck.Count; i++)
        {
            //Debug.Log(GameSession.SelectedDeck[i]);
        }
        //SceneLoader.Instance.LoadScene("Game");
        SceneManager.LoadScene(1);
    }
}

