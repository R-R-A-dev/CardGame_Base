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

    // デッキ編集画面が決定ボタンで閉じられた時に呼ばれる
    public System.Action OnDeckEditClosed;

    void Awake()
    {
        Instance = this;
        dataSet();
    }
    //TODO：ガチャの実装

    private void Start()
    {

    }

    /// <summary>
    /// デッキ編集の唯一の開始入口
    /// </summary>
    public void OpenDeckEditForDeck(int targetDeckNum)
    {
        deckNum = targetDeckNum;
        deckBuilderUI.StartDeckEdit(deckNum);
    }

    // CPU戦モードはコード側からOpenDeckEditForDeckを呼ぶ箇所がなく、
    // シーン上のパネルSetActive(true)のみで開かれる（OnEnableが唯一の初期化トリガー）ため維持する。
    // ローグライク側はDeckAndStageSelectUIがOpenDeckEditForDeckを明示的に呼ぶため、
    // ここではその時点のdeckNumで再初期化されるだけで実害はない。
    private void OnEnable()
    {
        OpenDeckEditForDeck(deckNum);
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
        // CPU戦デッキをcpuBattleDecksへ保存
        GameDataHolder.Instance.CommitDeckEdit(deckNum, false);

        GameSession.SelectedDeck = deckBuilderUI.GetDeck();
        for (int i = 0; i < GameSession.SelectedDeck.Count; i++)
        {
            //Debug.Log(GameSession.SelectedDeck[i]);
        }
        //SceneLoader.Instance.LoadScene("Game");
        SceneManager.LoadScene(1);
    }
}

