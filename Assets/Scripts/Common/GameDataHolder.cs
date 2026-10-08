using System.Collections.Generic;
using UnityEngine;

public class GameDataHolder : MonoBehaviour
{
    private static GameDataHolder _instance;

    // アクセス時に未生成なら自動生成する（Lazy初期化）。
    // GameDataHolderをアタッチしたオブジェクトが非活性（テスト構成でデッキ編集オブジェクトだけを
    // 活性にした場合など）でもAwake()が呼ばれないため、null事故を防ぐためにここで生成する。
    // シーン上の既存オブジェクトは親を持つ場合があり、そのままではDontDestroyOnLoadが使えないため、
    // 見つからない場合は新規のルートオブジェクトとして生成する（Dataはセーブファイルから読み直すため実害なし）
    public static GameDataHolder Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameDataHolder>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject(nameof(GameDataHolder));
                    _instance = obj.AddComponent<GameDataHolder>();
                }
                _instance.Initialize();
            }
            return _instance;
        }
    }

    private SaveData _data;

    // 呼ばれた時点でnullなら自動で読み込む（Lazy初期化）
    public SaveData Data
    {
        get
        {
            if (_data == null)
                LoadIntoMemory();
            return _data;
        }
        private set => _data = value;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            Initialize();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        DontDestroyOnLoad(gameObject);
        LoadIntoMemory();
    }

    public void LoadIntoMemory()
    {
        if (_data != null) return; // 二重読み込み防止
        _data = SaveManager.LoadOrInitialize();
        Debug.Log("セーブデータをメモリに読み込みました");
    }

    // 明示的に呼んだ時だけファイルへ書き込む
    public void SaveToFile()
    {
        SaveManager.Save(Data);
        Debug.Log("セーブデータをファイルに保存しました");
    }

    // ========================================
    // デッキ編集ブリッジ（添字方式のワーキングデータ ⇄ IDそのまま方式のセーブデータ）
    // ========================================

    // デッキ編集用ワーキングデータ（添字方式）。CardListData.Decksの代替
    public List<int> EditingDeckCounts { get; private set; }

    // 所持カードの表示用コピー（添字方式）。CardListData.PossessionCardの代替
    public List<int> DisplayPossessionCard { get; private set; }

    public void BeginDeckEdit(int deckNum, bool isRoguelike)
    {
        // 添字方式のリスト長はカード枚数ではなく「カードNoの最大値」で取る。
        // 枚数だとNoが歯抜けになった時に範囲外参照になる
        int cardTypeCount = CardDatabase.MaxCardNo;

        List<DeckSaveData> decks = isRoguelike ? Data.roguelikeDecks : Data.cpuBattleDecks;
        while (decks.Count <= deckNum)
            decks.Add(new DeckSaveData());

        EditingDeckCounts = ConvertCardIdsToCountList(decks[deckNum].cardIds, cardTypeCount);
        DisplayPossessionCard = ConvertCardIdsToCountList(
            CardOwnershipConverter.ToCardIdList(Data.ownedCardCounts), cardTypeCount);

        Debug.Log($"[BeginDeckEdit] deckNum={deckNum} isRoguelike={isRoguelike} " +
            $"savedCardIds.Count={decks[deckNum].cardIds.Count} EditingDeckCounts合計={SumOf(EditingDeckCounts)}");
    }

    // デバッグ用：セーブデータ（GameDataHolder.Data）を一切使わず、デッキ編集を開始する。
    // デッキ一覧は空、所持カード一覧は指定されたcountsから開始する
    public void DebugBeginDeckEdit(List<int> ownedCounts)
    {
        int cardTypeCount = CardDatabase.MaxCardNo;
        EditingDeckCounts = new List<int>(new int[cardTypeCount]);

        DebugOverrideDisplayPossessionCard(ownedCounts);

        Debug.Log("[DebugBeginDeckEdit] デッキ一覧を空、所持カード一覧をdebugOwnedCardCountsから設定しました（セーブデータ未使用）");
    }

    // デバッグ用：所持カードの表示用コピー（DisplayPossessionCard）のみを上書きする。
    // Data.ownedCardCounts（セーブデータ本体）には触れないため、ファイル保存には一切影響しない
    public void DebugOverrideDisplayPossessionCard(List<int> counts)
    {
        int cardTypeCount = CardDatabase.MaxCardNo;
        List<int> normalized = new List<int>(new int[cardTypeCount]);

        if (counts != null)
        {
            for (int i = 0; i < counts.Count && i < cardTypeCount; i++)
                normalized[i] = counts[i];

            // Inspectorのリストが短いままだと、その先のカードが所持0枚扱いで一覧に出ない
            if (counts.Count < cardTypeCount)
                Debug.LogWarning($"[DebugOverrideDisplayPossessionCard] 指定された所持枚数リストの要素数({counts.Count})が" +
                    $"カードNoの最大値({cardTypeCount})より少ないため、No.{counts.Count + 1}以降は所持0枚として扱われます");
        }

        DisplayPossessionCard = normalized;
        Debug.Log($"[DebugOverrideDisplayPossessionCard] 表示用の所持カードを上書きしました（セーブデータ非変更）");
    }

    public void CommitDeckEdit(int deckNum, bool isRoguelike)
    {
        List<DeckSaveData> decks = isRoguelike ? Data.roguelikeDecks : Data.cpuBattleDecks;
        while (decks.Count <= deckNum)
            decks.Add(new DeckSaveData());

        decks[deckNum].cardIds = ConvertCountListToCardIds(EditingDeckCounts);
        SaveToFile();

        Debug.Log($"[CommitDeckEdit] deckNum={deckNum} isRoguelike={isRoguelike} " +
            $"savedCardIds.Count={decks[deckNum].cardIds.Count}");
    }

    private int SumOf(List<int> counts)
    {
        int sum = 0;
        foreach (int c in counts) sum += c;
        return sum;
    }

    // IDそのままリスト → 添字方式（枚数ベース）に変換。sizeは必ず全カード種数を渡す
    private List<int> ConvertCardIdsToCountList(List<int> cardIds, int size)
    {
        List<int> result = new List<int>(new int[size]);
        if (cardIds == null) return result;

        foreach (int id in cardIds)
        {
            int index = id - 1;
            if (index >= 0 && index < result.Count)
                result[index]++;
        }
        return result;
    }

    // 添字方式（枚数ベース） → IDそのままリストに変換
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

    // ========================================
    // 所持カード関連
    // ========================================
    public void AddCard(int cardId)
    {
        CardOwnershipConverter.AddCard(Data.ownedCardCounts, cardId);
    }

    public bool RemoveCard(int cardId)
    {
        return CardOwnershipConverter.RemoveCard(Data.ownedCardCounts, cardId);
    }

    public int GetCardCount(int cardId)
    {
        return CardOwnershipConverter.GetCardCount(Data.ownedCardCounts, cardId);
    }

    public List<int> GetOwnedCardIds()
    {
        return CardOwnershipConverter.ToCardIdList(Data.ownedCardCounts);
    }

    // ========================================
    // 所持金関連
    // ========================================
    public int Gold => Data.gold;

    public void AddGold(int amount)
    {
        Data.gold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (Data.gold < amount) return false;
        Data.gold -= amount;
        return true;
    }

    // ========================================
    // パックアンロック関連
    // ========================================
    public bool IsPackUnlocked(int packId)
    {
        return Data.unlockedPackIds.Contains(packId);
    }

    public void UnlockPack(int packId)
    {
        if (!Data.unlockedPackIds.Contains(packId))
            Data.unlockedPackIds.Add(packId);
    }

    // ========================================
    // デッキ関連
    // ========================================
    public void AddCpuBattleDeck(DeckSaveData deck)
    {
        Data.cpuBattleDecks.Add(deck);
    }

    public void AddRoguelikeDeck(DeckSaveData deck)
    {
        Data.roguelikeDecks.Add(deck);
    }

    // ========================================
    // ローグライク進捗関連
    // ========================================
    public RoguelikeSaveData RoguelikeProgress => Data.roguelikeProgress;

    public void ResetRoguelikeProgress()
    {
        Data.roguelikeProgress = new RoguelikeSaveData();
    }

    // ========================================
    // ステージクリア関連
    // ========================================
    public void ClearLethalPuzzleStage(int stageId)
    {
        if (!Data.stageClearData.clearedLethalPuzzleStageIds.Contains(stageId))
            Data.stageClearData.clearedLethalPuzzleStageIds.Add(stageId);
    }

    public void ClearRoguelikeStage(string stageName)
    {
        if (!Data.stageClearData.clearedRoguelikeStageNames.Contains(stageName))
            Data.stageClearData.clearedRoguelikeStageNames.Add(stageName);
    }

    public void ClearTwoPickStage(int stageId)
    {
        if (!Data.stageClearData.clearedTwoPickStageIds.Contains(stageId))
            Data.stageClearData.clearedTwoPickStageIds.Add(stageId);
    }

    public bool IsLethalPuzzleStageCleared(int stageId)
    {
        return Data.stageClearData.clearedLethalPuzzleStageIds.Contains(stageId);
    }

    public bool IsRoguelikeStageCleared(string stageName)
    {
        return Data.stageClearData.clearedRoguelikeStageNames.Contains(stageName);
    }

    public bool IsTwoPickStageCleared(int stageId)
    {
        return Data.stageClearData.clearedTwoPickStageIds.Contains(stageId);
    }

    // ========================================
    // 設定関連
    // ========================================
    public GameSettings Settings => Data.settings;
}