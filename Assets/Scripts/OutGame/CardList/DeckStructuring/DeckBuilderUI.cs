using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DeckBuilderUI : MonoBehaviour
{
    [Header("ドロップゾーン")]
    [SerializeField] private DeckDropZone deckDropZone;
    [SerializeField] private CardListDropZone cardListDropZone;

    [Header("カード詳細")]
    [SerializeField] private CardDetailPanel cardDetailPanel;

    [Header("カード一覧・デッキ表示")]
    [SerializeField] GameObject cardPanel;
    [SerializeField] GameObject cardListContent;
    [SerializeField] GameObject deckContent;
    [SerializeField] CardListItem cardListItem;
    [SerializeField] DeckCardItem deckListItem;
    [SerializeField] GameObject cardPool;

    [Header("フィルター UI")]
    [SerializeField] private FilterPanelUI filterPanel;

    [Header("通常モード用UI")]
    [SerializeField] private GameObject normalGameStartButton; // 通常のゲームスタートボタン

    [Header("ローグライクモード用UI")]
    [SerializeField] private GameObject roguelikeDecideButton; // ローグライク用の決定ボタン

    [Header("CPU戦モード用UI")]
    [SerializeField] private GameObject cpuBattleDecideButton; // CPU戦用の決定ボタン

    [SerializeField] private GameObject deckEditPanel;         // このデッキ編集画面全体（非表示にする対象）

    [Header("デバッグ用（所持カード表示の上書き。セーブデータは変更しない）")]
    // ONにするとStartDeckEdit時にGameDataHolderのセーブデータを使わず、
    // デッキ一覧は空・所持カード一覧はdebugOwnedCardCountsから開始する
    [SerializeField] private bool debugMode = false;
    // 添字＝カードNo-1（0番目がカードNo.1）、値＝表示上の所持枚数
    [SerializeField] private List<int> debugOwnedCardCounts = new List<int>();


    private CardFilterSettings cardListFilter = new CardFilterSettings();
    private CardFilterSettings deckFilter = new CardFilterSettings();


    //設計を聞く
    void Start()
    {
        //DisplayDeck();
        //DisplayCardList();
        //RefreshAllCardUI();
        //PrepareDeck();
        //DeckBuilderManager.Instance.deckStatisticsUI.
        //    RefreshStatistics(DeckBuilderManager.Instance.deckNum);
        //SortCard();
    }

    public void StartDeckEdit(int deckNum)
    {
        DeckBuilderManager.Instance.deckNum = deckNum;

        bool isRoguelike = ModeConfigManager.Instance != null &&
                            ModeConfigManager.Instance.currentGameMode == GameMode.ROGUELIKE;

        if (debugMode)
        {
            // テストモード：セーブデータ（GameDataHolder.Data）は使わず、
            // デッキ一覧は空、所持カード一覧はdebugOwnedCardCountsから開始する
            GameDataHolder.Instance.DebugBeginDeckEdit(debugOwnedCardCounts);
        }
        else
        {
            GameDataHolder.Instance.BeginDeckEdit(deckNum, isRoguelike);
        }

        DisplayDeck();
        DisplayCardList();
        PrepareDeck();
        RefreshAllCardUI();
        DeckBuilderManager.Instance.deckStatisticsUI.
            RefreshStatistics(DeckBuilderManager.Instance.deckNum);
        SortCard();

        UpdateModeButtons();
    }

    // 現在のゲームモードに応じて、対応する決定ボタンだけを表示する
    private void UpdateModeButtons()
    {
        GameMode mode = ModeConfigManager.Instance != null
            ? ModeConfigManager.Instance.currentGameMode
            : GameMode.NONE;

        bool isRoguelike = mode == GameMode.ROGUELIKE;
        bool isCpuBattle = mode == GameMode.CPU_BATTLE;

        normalGameStartButton.SetActive(!isRoguelike && !isCpuBattle);
        roguelikeDecideButton.SetActive(isRoguelike);
        cpuBattleDecideButton.SetActive(isCpuBattle);
    }

    // ローグライクモードの決定ボタン押下時
    public void OnRoguelikeDecideButtonClick()
    {
        int deckIndex = DeckBuilderManager.Instance.deckNum;
        GameDataHolder.Instance.CommitDeckEdit(deckIndex, true);

        deckEditPanel.SetActive(false);
        DeckBuilderManager.Instance.OnDeckEditClosed?.Invoke();
    }

    // CPU戦モードの決定ボタン押下時
    public void OnCpuBattleDecideButtonClick()
    {
        int deckIndex = DeckBuilderManager.Instance.deckNum;
        GameDataHolder.Instance.CommitDeckEdit(deckIndex, false);

        deckEditPanel.SetActive(false);
        DeckBuilderManager.Instance.OnDeckEditClosed?.Invoke();
    }

    // ========================================
    // デバッグ用（GameMode未設定のままテスト対戦）
    // ========================================

    /// <summary>
    /// 外部（復帰処理）からこのデッキ編成画面パネルを表示するために公開
    /// </summary>
    public void ShowDeckEditPanel()
    {
        deckEditPanel.SetActive(true);
    }

    /// <summary>
    /// テスト対戦ボタン：GameModeやGameDataHolder（セーブデータ）には一切触れず、
    /// 編集中のデッキ同士で即座に対戦する。対戦後はこのデッキ編成画面へ直接戻る
    /// </summary>
    public void OnDebugTestBattleClick()
    {
        int deckIndex = DeckBuilderManager.Instance.deckNum;

        // セーブへの保存(CommitDeckEdit)は行わず、編集中のデッキをそのまま読み取って使う
        List<int> deck = GetDeck();
        GameSession.SelectedDeck = new List<int>(deck);
        GameSession.EnemyDeck = new List<int>(deck);

        GameSession.DebugReturnToDeckEdit = true;
        GameSession.DebugReturnDeckNum = deckIndex;

        SceneTransition.Load("Game");
    }

    /// <summary>
    /// デバッグ用ボタン：Inspectorで設定したdebugOwnedCardCounts（添字＝カードNo-1）の内容で
    /// 「所持カード一覧」の表示だけを上書きする。セーブデータ（GameDataHolder.Data）には触れない
    /// </summary>
    public void OnDebugApplyOwnedCardsClick()
    {
        GameDataHolder.Instance.DebugOverrideDisplayPossessionCard(debugOwnedCardCounts);

        // BeginDeckEditは呼ばない（呼ぶとセーブデータからDisplayPossessionCardが再構築され上書きが消えるため）
        DisplayCardList();
        PrepareDeck();
        RefreshAllCardUI();
        SortCard();
    }

    // ===== 表示用の一時変換のみに使う内部メソッド =====

    // IDそのままリスト → 添字方式（枚数ベース）に変換（DeckBuilderUI表示専用）
    private List<int> ConvertCardIdsToCountList(List<int> cardIds)
    {
        List<int> result = new List<int>();

        if (cardIds == null || cardIds.Count == 0)
            return result;

        int maxId = 0;
        foreach (int id in cardIds)
            if (id > maxId) maxId = id;

        result = new List<int>(new int[maxId]);

        foreach (int id in cardIds)
        {
            int index = id - 1;
            result[index]++;
        }

        return result;
    }

    // 添字方式（枚数ベース） → IDそのままリストに変換（保存直前のみ使用）
    private List<int> ConvertCountListToCardIds(List<int> countList)
    {
        List<int> result = new List<int>();

        if (countList == null) return result;

        for (int i = 0; i < countList.Count; i++)
        {
            int cardId = i + 1;
            int count = countList[i];

            for (int n = 0; n < count; n++)
                result.Add(cardId);
        }

        return result;
    }

    // ローグライクデッキの保存処理
    private void SaveRoguelikeDeck(List<int> deck)
    {
        DeckSaveData deckData = new DeckSaveData
        {
            deckName = $"ローグライクデッキ{DeckBuilderManager.Instance.deckNum}",
            cardIds = deck
        };

        // 既存の同番号デッキがあれば上書き、なければ追加
        var existingDeck = GameDataHolder.Instance.Data.roguelikeDecks
            .Find(d => d.deckName == deckData.deckName);

        if (existingDeck != null)
        {
            existingDeck.cardIds = deck;
        }
        else
        {
            GameDataHolder.Instance.Data.roguelikeDecks.Add(deckData);
        }

        GameDataHolder.Instance.SaveToFile();
    }

    void Update()
    {

    }

    /// <summary>
    /// カード一覧表示
    /// </summary>
    public void DisplayCardList()
    {
        //子オブジェクトの数以上なら削除
        cardListItem.CardSetUp(cardListContent.transform);
        //カードの一覧と所持カードのIDを比較して、所持しているカードを表示する
        //パネルと一緒に表示させる
    }

    /// <summary>
    /// デッキ表示表示
    /// </summary>
    public void DisplayDeck()
    {
        //子オブジェクトの数以上なら削除
        deckListItem.CardSetUp(deckContent.transform, DeckBuilderManager.Instance.deckNum);
    }

    /// <summary>
    /// フィルター変更時
    /// </summary>
    /// <param name="filterIndex"></param>
    public void OnFilterChanged(int filterIndex)
    {

    }

    /// <summary>
    /// 検索入力変更時
    /// </summary>
    /// <param name="keyword"></param>
    public void OnSearchInputChanged(string keyword)
    {

    }

    /// <summary>
    /// カード一覧からドラッグ＆ドロップでデッキに追加
    /// </summary>
    /// <param name="card"></param>
    public void AddDeckCard(OutGameCardList card, int cardNo, CardDragHandler cardDragHandler)
    {
        //デッキと所持一覧のリストに追加・削除
        deckListItem.AddDeckCard(cardNo);
        cardListItem.ReturnCardList(cardNo);


        //デッキと所持一覧のカードUIを更新
        foreach (OutGameCardList outGameCardList in cardListContent.GetComponentsInChildren<OutGameCardList>(true))
        {
            if (cardNo == outGameCardList.No)
            {
                card.RefreshCardView(false, cardNo,
                DeckBuilderManager.Instance.deckNum, outGameCardList);
                break;
            }
        }

        foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>(true))
        {
            if (cardNo == outGameCardList.No)
            {
                card.RefreshCardView(true, cardNo,
                DeckBuilderManager.Instance.deckNum, outGameCardList);
                break;
            }
        }
        //デッキに二枚目であればオブジェクトプールから取得して追加
        //一枚目であればそのまま移動
        if (GameDataHolder.Instance.EditingDeckCounts[cardNo - 1] >= 2)
        {
            DeckBuilderManager.Instance.deckBuilderUI.PoolCard(card.gameObject);
        }
        else if (GameDataHolder.Instance.EditingDeckCounts[cardNo - 1] == 1)
        {
            int newCardCost = card.Cost;

            // デッキ内のすべてのカードを取得
            OutGameCardList[] deckCards = deckContent.GetComponentsInChildren<OutGameCardList>();

            int insertIndex = deckCards.Length; // デフォルトは末尾

            // SortByCostと同じ基準（コスト順・同コストはカードNo順）で挿入位置を探す
            for (int i = 0; i < deckCards.Length; i++)
            {
                if (CompareCardOrder(newCardCost, cardNo, deckCards[i].Cost, deckCards[i].No) < 0)
                {
                    insertIndex = i;
                    break;
                }
            }
            // 指定した位置に Transform を挿入
            card.transform.SetParent(deckContent.transform);
            card.transform.SetSiblingIndex(insertIndex);

            foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
            {
                if (cardNo == outGameCardList.No)
                {
                    outGameCardList.DeckAddPanelOn();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// デッキからドラッグ＆ドロップでカード一覧に戻す
    /// </summary>
    /// <param name="card"></param>
    public void ReturnCardList(OutGameCardList card, int cardNo)
    {
        cardListItem.AddCardList(cardNo);
        deckListItem.ReturnListCard(cardNo);


        //デッキと所持一覧のカードUIを更新
        foreach (OutGameCardList outGameCardList in cardListContent.GetComponentsInChildren<OutGameCardList>(true))
        {
            if (cardNo == outGameCardList.No)
            {
                card.RefreshCardView(false, cardNo,
                DeckBuilderManager.Instance.deckNum, outGameCardList);
                break;
            }
        }

        foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>(true))
        {
            if (cardNo == outGameCardList.No)
            {
                card.RefreshCardView(true, cardNo,
                DeckBuilderManager.Instance.deckNum, outGameCardList);
                break;
            }
        }


        if (GameDataHolder.Instance.EditingDeckCounts[cardNo - 1] == 0)
        {
            foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
            {
                if (cardNo == outGameCardList.No)
                {
                    DeckBuilderManager.Instance.deckBuilderUI.PoolCard(outGameCardList.gameObject);
                    break;
                }
            }
        }
        DeckBuilderManager.Instance.deckBuilderUI.PoolCard(card.gameObject);
    }

    /// <summary>
    /// デッキと所持カードの枚数を更新して、カードUIを更新
    /// </summary>
    public void RefreshAllCardUI()
    {
        foreach (OutGameCardList outGameCardList in cardListContent.GetComponentsInChildren<OutGameCardList>())
            outGameCardList.RefreshView(false, DeckBuilderManager.Instance.deckNum);
        foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
            outGameCardList.RefreshView(true, DeckBuilderManager.Instance.deckNum);
    }

    /// <summary>
    /// 一覧からデッキ分を減算して準備
    /// </summary>
    void PrepareDeck()
    {
        cardListItem.PrepareDeck();
    }

    /// <summary>
    /// ソートの実行
    /// </summary>
    void SortCard()
    {
        // 所持カードリストをソート
        SortByCost(cardListContent.transform);

        // デッキリストをソート
        SortByCost(deckContent.transform);
    }

    /// <summary>
    /// 表示順の比較基準：コスト昇順、同コストならカードNo昇順。
    /// List.Sortは安定ソートではないため、コストだけで比較すると同コストのカードの
    /// 並びが実行ごとに変わってしまう。必ずカードNoでタイブレークする
    /// </summary>
    private static int CompareCardOrder(int costA, int noA, int costB, int noB)
    {
        int byCost = costA.CompareTo(costB);
        return byCost != 0 ? byCost : noA.CompareTo(noB);
    }

    /// <summary>
    /// 初期表示のカードをコスト順（同コストはカードNo順）にソート
    /// </summary>
    /// <param name="parent"></param>
    void SortByCost(Transform parent)
    {
        // 子オブジェクトからOutGameCardListを全部取得
        List<OutGameCardList> cards = new List<OutGameCardList>(parent.GetComponentsInChildren<OutGameCardList>());

        // コスト昇順、同コストならカードNo昇順
        cards.Sort((a, b) => CompareCardOrder(a.Cost, a.No, b.Cost, b.No));

        // 並び替えた順にHierarchy上の位置を更新
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.SetSiblingIndex(i);
        }
    }

    /// <summary>
    /// カードのオブジェクトプールから非アクティブなカードを取得
    /// </summary>
    /// <returns></returns>
    public GameObject GetCardPool()
    {
        if (cardPool.transform.childCount == 0)
            return null;

        // 子オブジェクトを順にチェック
        for (int i = 0; i < cardPool.transform.childCount; i++)
        {
            GameObject child = cardPool.transform.GetChild(i).gameObject;
            // 非アクティブならそれを返す
            if (!child.activeSelf)
            {
                return child;
            }
        }
        return null;
    }

    /// <summary>
    /// オブジェクトプールにカードを戻す
    /// </summary>
    /// <param name="card"></param>
    public void PoolCard(GameObject card)
    {
        card.SetActive(false);
        card.transform.SetParent(cardPool.transform);
    }

    /// <summary>
    /// 一覧からデッキに追加する際の目的地をコスト順に探して返す
    /// </summary>
    /// <param name="cardNo"></param>
    /// <param name="cost"></param>
    /// <returns></returns>
    public Vector3 GetCardPosToDeck(int cardNo, int cost)
    {
        //既にデッキにあるカードならその位置を返す 
        Vector3 movePos = Vector3.zero;
        foreach (OutGameCardList outGameCardList in deckContent.GetComponentsInChildren<OutGameCardList>())
        {
            if (cardNo == outGameCardList.No)
            {
                movePos = outGameCardList.transform.position;
                return movePos;
            }
        }

        float x = 110;
        float y = 673.5f;
        float interval = 220;
        //0番目の位置 x:110 y:673.5
        //間の距離 220

        //デッキ内にないカードなら挿入位置を探す
        OutGameCardList[] deckCards = deckContent.GetComponentsInChildren<OutGameCardList>();

        int insertIndex = deckCards.Length; // デフォルトは末尾
        if (deckCards.Length != 0)
        {
            // 実際の挿入処理(AddDeckCard)と同じ基準で探さないと、移動先と実際の並び位置がずれる
            for (int i = 0; i < deckCards.Length; i++)
            {
                if (CompareCardOrder(cost, cardNo, deckCards[i].Cost, deckCards[i].No) < 0)
                {
                    insertIndex = i;
                    return movePos = deckCards[i].transform.position;
                }
            }
        }

        //デッキにカードが一枚もない場合、または並び順が一番後ろの場合は最後尾に追加
        if (movePos == Vector3.zero && deckCards.Length != 0)
        {
            movePos = deckCards[deckCards.Length - 1].transform.position;
            movePos = new Vector2(movePos.x + interval, movePos.y);
            return movePos;
        }

        // 指定した位置に Transform を挿入
        //card.transform.SetParent(deckContent.transform);
        //card.transform.SetSiblingIndex(insertIndex);
        //インデックスから場所を取得して目的地を出す
        if (movePos == Vector3.zero && deckCards.Length == 0)
        {
            movePos = new Vector2(x, y);
        }
        return movePos;
    }

    /// <summary>
    /// デッキから一覧に戻す際の目的地を探して返す
    /// </summary>
    /// <param name="cardNo"></param>
    /// <returns></returns>
    public Vector3 GetCardPosToList(int cardNo)
    {
        Vector3 movePos = Vector3.zero;
        foreach (OutGameCardList outGameCardList in cardListContent.GetComponentsInChildren<OutGameCardList>(true))
        {
            if (cardNo == outGameCardList.No)
            {
                movePos = outGameCardList.transform.position;
                return movePos;
            }
        }
        return movePos;
    }

    /// <summary>
    /// ボタン押下後にカード一覧のフィルターパネルを開く
    /// </summary>
    public void OnOpenCardListFilter()
    {
        filterPanel.Open(this, false, cardListFilter);
    }

    /// <summary>
    /// ボタン押下後にデッキのフィルターパネルを開く
    /// </summary>
    public void OnOpenDeckFilter()
    {
        filterPanel.Open(this, true, deckFilter);
    }

    /// <summary>
    /// フィルター結果UI表示の更新
    /// </summary>
    /// <param name="newFilter"></param>
    /// <param name="forDeck"></param>
    public void ApplyFilter(CardFilterSettings newFilter, bool forDeck)
    {
        if (forDeck)
        {
            deckFilter = newFilter;
            deckListItem.RefreshFiltered(deckFilter);
        }
        else
        {
            cardListFilter = newFilter;
            cardListItem.RefreshFiltered(cardListFilter);
        }
    }

    /// <summary>
    /// ドロップゾーンのコンポーネントを切り替え
    /// </summary>
    public void OnDropZone()
    {
        cardListDropZone.enabled = true;
        deckDropZone.enabled = true;
    }

    /// <summary>
    /// ドロップゾーンのコンポーネントを切り替え
    /// </summary>
    public void OffDropZone()
    {
        cardListDropZone.enabled = false;
        deckDropZone.enabled = false;
    }


    /// <summary>
    /// デッキの一覧からデッキ情報を取得して返す
    /// </summary>
    public List<int> GetDeck()
    {
        List<int> deck = new List<int>();

        // 編集中のデッキ（添字＝カードNo-1、値＝枚数）からそのままデッキリストを作る。
        // 以前は枚数配列（カードNo順）と表示側の子オブジェクト（コスト順にソート済み）を
        // 同じ添字で突き合わせていたため、編成したものと別のカードが対戦に渡っていた
        List<int> editingDeckCounts = GameDataHolder.Instance.EditingDeckCounts;

        for (int i = 0; i < editingDeckCounts.Count; i++)
        {
            int cardNo = i + 1;
            for (int n = 0; n < editingDeckCounts[i]; n++)
                deck.Add(cardNo);
        }

        return deck;
    }
}
/*
    ドラッグアンドドロップじに数が合わなくなる時がある
    左右クリックをするとraycasttargetがおかしくなる
    フィルターパネル表示時に他の操作ができてしまう
*/