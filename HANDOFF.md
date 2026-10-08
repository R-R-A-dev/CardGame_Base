# 引き継ぎドキュメント（Claude Code用）

## 最初にやってほしいこと

作業を始める前に、このプロジェクト内で以下のキーワードを検索して、
実際にどのクラスがどう実装されているか把握してください。

```
CardListData
GameDataHolder
DeckBuilderUI
DeckBuilderManager
DeckAndStageSelectUI
CardDragHandler
RoguelikeManager
SaveManager
SaveData
```

このドキュメントに書いてある「〜のはず」「〜という設計にした」という記述は、
チャットでのやり取りをもとにした**設計意図**であり、実際のコードと完全に一致していない
可能性があります。**必ず実ファイルを確認してから修正してください。**
特に `CardListData.Decks` / `CardListData.PossessionCard` への参照が、
まだ書き換え漏れで残っている箇所がないか、プロジェクト全体を grep してください。

---

## プロジェクト概要

Unity製カードゲーム。複数のゲームモードを持つ。

- CPU対戦モード
- 2Pick
- リーサルパズル
- ローグライクモード（Slay the Spire風、マップ上を進みながらデッキを強化）
- ガチャ（カードパック開封）

いずれも共通の `DeckBuilderUI` / `DeckBuilderManager` でデッキ編成を行う。

---

## 現在進行中の作業：セーブデータ設計の刷新

### 背景

以前は `CardListData`（static クラス）が以下を保持していた。

```csharp
public class CardListData
{
    static List<List<int>> decks;        // デッキの中身（添字=カードNo-1、値=枚数）
    static List<int> possessionCard;      // 所持カード（添字=カードNo-1、値=枚数）
    static List<CardEntity> entities;     // 全カードのマスターデータ
}
```

これをセーブ・ロード可能な形に作り直すため、以下の方針で移行中。

### 新しいデータ構造

**SaveData（ファイルに保存される実データ、IDそのまま形式）**

```csharp
[System.Serializable]
public class SaveData
{
    public List<int> ownedCardCounts = new();      // 添字方式（添字=カードID-1、値=所持枚数）
    public int gold = 0;
    public List<int> unlockedPackIds = new();
    public List<DeckSaveData> cpuBattleDecks = new();   // IDそのまま方式
    public List<DeckSaveData> roguelikeDecks = new();   // IDそのまま方式
    public RoguelikeSaveData roguelikeProgress = new();
    public StageClearData stageClearData = new();
    public GameSettings settings = new();
}

[System.Serializable]
public class DeckSaveData
{
    public string deckName;
    public List<int> cardIds = new(); // IDをそのまま並べたリスト（例: [2,2,5]）
}
```

**重要な形式の違い**

| データ | 形式 | 例 |
|---|---|---|
| `SaveData.ownedCardCounts` | 添字方式（添字=カードID-1、値=枚数） | `[0,2,0]` → ID2を2枚所持 |
| `DeckSaveData.cardIds`（cpuBattleDecks/roguelikeDecks） | IDそのまま方式 | `[2,2]` → ID2が2枚 |
| `CardListData.Decks` / `PossessionCard`（旧） | 添字方式 | `DisplayDeck`等の既存UIが前提とする形式 |

**`GameDataHolder`（新規、メモリ上のキャッシュ）**

- `SaveManager.Load()/Save()` でファイルI/Oを毎回行わず、
  起動時に一度だけ `SaveData` をメモリに読み込み、そこに対して読み書きする。
- `SaveToFile()` を明示的に呼んだ時だけファイルに書き込む。
- `DontDestroyOnLoad` のシングルトン。

```csharp
public class GameDataHolder : MonoBehaviour
{
    public static GameDataHolder Instance { get; private set; }
    public SaveData Data { get; } // Lazy初期化、null時は自動でLoadOrInitialize

    // デッキ編集用ワーキングデータ（添字方式）
    // CardListData.Decks の代替。BeginDeckEdit() でロードされる
    public List<int> EditingDeckCounts { get; private set; }

    // 所持カードの表示用コピー（本体 ownedCardCounts のコピー）
    // CardListData.PossessionCard の代替。BeginDeckEdit() でコピーされる
    public List<int> DisplayPossessionCard { get; private set; }

    public void BeginDeckEdit(int deckNum, bool isRoguelike);  // 編集開始時に呼ぶ
    public void CommitDeckEdit(int deckNum, bool isRoguelike); // 決定ボタンで呼ぶ
    public void SaveToFile();
}
```

### 移行の考え方

`DeckBuilderUI` などの既存UIロジックは「添字方式（`CardListData.Decks[deckNum][cardNo-1]`のような形）」
を前提に作られている。これを壊さずに新しいセーブ形式（IDそのまま方式）と橋渡しするため：

1. デッキ編集**開始時**（`GameDataHolder.BeginDeckEdit`）：
   `roguelikeDecks[X].cardIds`（IDそのまま） → `EditingDeckCounts`（添字方式）に変換してロード
2. 編集中は `EditingDeckCounts` / `DisplayPossessionCard`（どちらも添字方式）に対して直接増減
3. 決定ボタン押下時（`GameDataHolder.CommitDeckEdit`）：
   `EditingDeckCounts`（添字方式） → `cardIds`（IDそのまま）に変換して `SaveData` に書き戻し、`SaveToFile()`

**`CardListData.Decks` と `CardListData.PossessionCard` は廃止する方針。**
`CardListData.Entities`（全カードのマスターデータ）は引き続き使用する。

---

## 現在判明している未解決・要確認事項

### 1. `CardListData.Decks` / `PossessionCard` の参照が残っている箇所を洗い出す

実際に以下のクラスで書き換え漏れによるクラッシュが発生した実績がある。

- `OutGameCardList.RefreshView` / `RefreshCardView`
  → `EditingDeckCounts` / `DisplayPossessionCard` を参照するよう修正済み（要ファイル確認）
- `DeckStatisticsUI.RefreshStatistics`
  → `deck` 引数が渡されない場合に `CardListData.Decks[deckNum]` にフォールバックしていた。
    `GameDataHolder.Instance.EditingDeckCounts` にフォールバックするよう修正済み（要ファイル確認）
- `DeckCardItem` / `CardListItem`
  → `CardSetUp` / `AddDeckCard` / `ReturnListCard` 等で `CardListData.Decks` を直接触っている箇所が
    まだ残っている可能性がある。**要全文確認・grep。**

### 2. インデックス範囲外エラーの根本原因と対策パターン

`EditingDeckCounts` / `DisplayPossessionCard` は「デッキ内カードの最大ID」ではなく
**必ず全カード種類数（`CardDatabase.LoadAllCards().Length`）でサイズを確保**すること。
空デッキ・所持数0のカードがあっても範囲外にならないようにするため。

```csharp
// 良い例：sizeを外から必ず指定する
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
```

同様のリスト操作（`GetCardCount`, `AddCard`, `RemoveCard` 等）にも
範囲外アクセスのガードが入っているか確認すること。

### 3. デッキ枠の初期化

`SaveManager.CreateInitialSaveData()` で `cpuBattleDecks` / `roguelikeDecks` に
あらかじめ複数の空デッキ枠（3枠想定）を用意する方針にした。
`GameDataHolder.BeginDeckEdit` 側にも、枠が不足していた場合の自動補充ガードを入れている。
**この初期化コードが実際に反映されているか確認すること。**

### 4. `DeckBuilderManager` の編集開始フロー

以前は `OnEnable()` で `StartDeckEdit(deckNum)` を自動実行していたが、
`deckNum` が確定する前に走ってしまう二重初期化のバグがあったため、
**`OpenDeckEditForDeck(int targetDeckNum)` を唯一の入口とする設計に変更した。**

```csharp
public void OpenDeckEditForDeck(int targetDeckNum)
{
    deckNum = targetDeckNum;
    deckEditPanel.SetActive(true);
    deckBuilderUI.StartDeckEdit(deckNum);
}
```

`OnEnable()` 内の自動呼び出しは削除済みのはず。**残っていないか確認すること。**

### 5. ローグライクモードのデッキ編成UI分岐

`ModeConfigManager.Instance.currentGameMode == GameMode.ROGUELIKE` で判定し、
`DeckBuilderUI` 側で通常モード用スタートボタンとローグライク用決定ボタンの表示を切り替える設計。

```csharp
private bool IsRoguelikeMode =>
    ModeConfigManager.Instance != null &&
    ModeConfigManager.Instance.currentGameMode == GameMode.ROGUELIKE;
```

`DeckBuilderManager` には以下2つの決定ボタン用メソッドがある（用途が異なるボタンにそれぞれアタッチ）。

```csharp
public void OnRoguelikeDecideButtonClick()  // ローグライク用デッキ編集決定ボタン
public void OnCpuBattleDecideButtonClick()  // CPU戦用デッキ編集決定ボタン
```

2Pick・リーサルパズルはデッキ編成を行わないため、専用ボタンは不要。

### 6. `DeckAndStageSelectUI` のデッキ枚数チェック

決定ボタン（`DecideDeck` がアタッチされたボタン、`decideDeckButton` フィールド）は、
選択中デッキの枚数が40枚未満の場合は非活性にする。

```csharp
private const int MIN_DECK_SIZE = 40;

private int GetSelectedDeckSize()
{
    var decks = IsRoguelikeMode
        ? GameDataHolder.Instance.Data.roguelikeDecks
        : GameDataHolder.Instance.Data.cpuBattleDecks;
    if (selectedDeckId < 0 || selectedDeckId >= decks.Count) return 0;
    return decks[selectedDeckId].cardIds.Count;
}
```

デッキ編集画面から戻ってきた時に再チェックするため、
`DeckBuilderManager.OnDeckEditClosed`（Action）を購読している。

---

## 他システムの現状（参考・優先度低）

### ローグライクモード

- `RoguelikeManager` がマップ・戦闘・報酬画面などの進行を統括
- `NodeData`（SO）: 1マスの情報。`stageType` に応じて `EnemyData` / `RewardData` /
  `RestData` / `ShopData` / `TreasureData` / `DamageNodeData` / `CardLossData` を持つ
- `MapData` → `MapRowData`（層） → `NodeData`（候補ノード、Inspector上で `nodeCount` 分ランダム選出）
- マップのノードUIは動的生成せず、Hierarchy上に事前配置したボタンに割り当てる方式
- 戦闘は別シーン遷移（`RoguelikeSession` というstaticクラスでシーンをまたいでデータを渡す）
- `ParameterModifier`（バフ・デバフ）は HP系のみ `RoguelikeManager` が直接処理、
  それ以外（マナ・攻撃・防御・ドロー枚数）は戦闘シーン側で `RoguelikeSession.BattleModifiers` を参照して適用

### ガチャ機能

- `PackData`（SO）: `cardPool`（`GachaCardEntry` のリスト：cardId + rarity）、
  `rarityDropRates`（レアリティごとの排出率%）を持つ
- 抽選は2段階：①排出率%でレアリティを決定 → ②そのレアリティ内のカードから均等抽選
- `GachaCardItem`：カードのめくり演出（Y軸回転、SSRのみ追加でパンチスケール演出）
- `GachaCardDetailPanel`：めくり済みカードの詳細表示（共通パネル、`Open(CardController)` を直接呼ぶ方式）

---

## 命名・設計の慣習

- ScriptableObject は `[CreateAssetMenu]` を付け、`Roguelike/XxxData` のようなメニュー名にする
- UIの開閉は `Open()` / `Hide()` / `Close()` の組み合わせで統一
- 相互依存を避けるため、子UI→親UIへの通知は `System.Action` を使う
  （例: `TreasureCardSelectPanel.OnClosed`, `GachaCardSelectPanel.OnCardBuyConfirmed`）
- カード一覧の左クリック＝選択、右クリック＝詳細表示、というパターンが複数箇所にある
- Hierarchy上に事前配置したUI要素を使い回す設計を好む（`Instantiate` より `SetActive` 切り替え）

---

## お願いしたいこと

1. 上記の「未解決・要確認事項」を実ファイルで検証し、ズレがあれば実際のコードを正としてください
2. `CardListData.Decks` / `PossessionCard` への参照が全て置き換わっているか、プロジェクト全体を検索してください
3. 大きな設計変更（クラスの責務を変える、データ形式を変える等）をする前に、変更内容を一度提示してから進めてください
4. 何か不明な設計判断があれば、このドキュメントの記述より実装済みコードを優先して判断してください
