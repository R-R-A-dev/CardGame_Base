# 敵AI強化 実装指示書

対象ファイル: `Assets/Scripts/InGame/AI.cs` とその周辺
目的: 敵AIの意思決定を「配列の先頭固定」から「盤面評価によるスコアリング」に置き換える

---

## 0. 最初に必ずやること

実装を始める前に、以下のファイルを**全文**読んでください。要約や grep だけで進めないでください。

```
Assets/Scripts/InGame/AI.cs
Assets/Scripts/InGame/CardModel.cs
Assets/Scripts/InGame/CardEntity.cs        ← ABILITIES / SPELLS の enum 定義
Assets/Scripts/InGame/CardController.cs    ← 1969行ある。特に CanUseSpells / CanUseAbilities / UseSpellTo / UseAbilitiesTo
Assets/Scripts/InGame/GameManager.cs       ← GetXxxFieldCards / CardsBattle / AttackToHero / ReduceManaCost
Assets/Scripts/InGame/GamePlayerManager.cs
```

この指示書の記述と実コードが食い違っていたら、**実コードを正**としてください。
その上で「指示書のここが実際と違った」と報告してください。

---

## 1. 絶対に間違えるポイント（最重要）

### 1-1. `GetEnemyFieldCards` / `GetFriendFieldCards` の引数の意味

引数 `isPlayer` は **「返ってくるカードの持ち主」ではなく「問い合わせる側の視点（呼び出し元カードの `isPlayerCard`）」** です。

```csharp
// GameManager.cs
GetEnemyFieldCards(true)   → enemyFieldTransform  の子（= 敵AI側のカード）
GetEnemyFieldCards(false)  → playerFieldTransform の子（= プレイヤー側のカード）
GetFriendFieldCards(true)  → playerFieldTransform の子（= プレイヤー側のカード）
GetFriendFieldCards(false) → enemyFieldTransform  の子（= 敵AI側のカード）
```

**敵AIのカードは `model.isPlayerCard == false`** です。したがって AI 視点では：

| 欲しいもの | 正しい呼び方 |
|---|---|
| AI（自分）の盤面 | `GetFriendFieldCards(false)` |
| プレイヤー（相手）の盤面 | `GetEnemyFieldCards(false)` |
| AI（自分）の手札 | `GetFriendHandTransform(false)` |
| プレイヤー（相手）の手札 | `GetEnemyHandTransform(false)` |

**ただし既存の AI.cs は逆の慣習で書かれています。**

```csharp
// AI.cs:33, AI.cs:51 — 「プレイヤー視点」で呼んで自陣を取っている（結果は正しいが紛らわしい）
CardController[] fieldCardList = gameManager.GetEnemyFieldCards(true);   // = AI自陣
```

→ **新しく書くコードでは必ず下記のラッパーだけを使い、`GetEnemyFieldCards` / `GetFriendFieldCards` を直接呼ばないでください。**

```csharp
// AIEvaluator.cs 内に定義する（AI視点固定のラッパー）
static CardController[] SelfField() => Alive(GameManager.instance.GetFriendFieldCards(false));
static CardController[] OppField()  => Alive(GameManager.instance.GetEnemyFieldCards(false));
static CardController[] SelfHand()  => GameManager.instance.GetFriendHandTransform(false);
static CardController[] OppHand()   => GameManager.instance.GetEnemyHandTransform(false);
```

### 1-2. ダメージ量のフィールドが効果ごとに違う

リーサル計算をここで間違えると全体が破綻します。実コードで確認済みの対応表：

| 行為 | 使われる値 | 確認箇所 |
|---|---|---|
| フォロワーの通常攻撃（カードへ） | `model.at` | `CardModel.Attack()` |
| フォロワーの通常攻撃（ヒーローへ） | `model.at` | `CardController.LerpThrowHero` ほか |
| スペルのヒーローへのダメージ | **`model.effectDmg`** | `CardController.cs:1589 / 1641 / 1713` |
| スペル/アビリティのカードへのダメージ | **`model.effectDmg`** | `CardModel.EffectDmg()` |
| `EffectHeal()` による回復 | `model.effectHeal` | `CardModel.EffectHeal()` |
| `Heal()` による回復 | **`model.at`** | `CardModel.Heal()` |

`DAMAGE_ENEMY_HERO` のバーン打点は `at` ではなく **`effectDmg`** です。

### 1-3. 破壊演出中のカードが取得結果に混ざる

`GetComponentsInChildren<CardController>()` は `Destroy()` 予約済みのオブジェクトも返します。
`hp <= 0` / `isAlive == false` のカードが混ざるため、**すべての取得結果を必ずフィルタしてください。**

```csharp
static CardController[] Alive(CardController[] src)
{
    return System.Array.FindAll(src, c => c != null && c.model != null && c.model.isAlive && c.model.hp > 0);
}
```

これを怠ると「死んだカードを攻撃対象に選ぶ」「死んだカードで攻撃しようとして
`CardController.Attack()` が早期 return し、`SetCanAttack(false)` されずに攻撃ループが無限化する」
という不具合が起きます。

### 1-4. 盤面の上限は5体

`Length > 4` で新規モンスター配置がブロックされます（`CardMovement.cs:30`, `CardModel.cs:172`, `AI.cs:51`）。
スペルは盤面が埋まっていても使えます。**この条件式の意味を変えないでください。**

---

## 2. 触ってよいもの / 触ってはいけないもの

### 触ってよい

- `Assets/Scripts/InGame/AI/AIEvaluator.cs`（**新規作成**）
- `Assets/Scripts/InGame/AI/AIProfile.cs`（**新規作成**）
- `Assets/Scripts/InGame/AI.cs`（判断部分の差し替えのみ）
- `Assets/Scripts/InGame/CardModel.cs`（`maxHp` フィールド追加のみ。段階6で必要）

### 絶対に触ってはいけない

1. **`AI.cs` 内の `yield return new WaitForSeconds(...)` の値・位置・個数**
   演出タイミングの調整済み定数です。1つも増減・変更しないでください。
2. **`while (GameManager.instance.isAttacking || GameManager.instance.isSummoning)` の待機ループ**
   演出の同期に必須です。
3. **`gameManager.timeCount > 0` のループ条件**
   ターン制限時間のガードです。
4. **プレイヤー側の処理**
   `CardController.OnFiledAbilities()` / `ActAbility()`、`AttackedCard.cs`、`AttackedHero.cs`、
   `DropPlace.cs`、`CardMovement.cs`、`CardClickManager.cs` は**変更しないでください**。
   （プレイヤーの操作感が変わってしまうため）

   **※例外：`ActAbility()` は、常時パッシブ系アビリティ（`DESTROY_ATTACKED_TARGET` /
   `DAMAGE_NULLIFY_ONCE` / `DOUBLE_ACTION` / `STATS_UP_ON_ATTACK` など）単体のカードで
   `isAttacking` が true のまま戻らず、ドラッグ操作とターン終了がフリーズする既存バグが
   見つかったため、段階2の後に別件として修正済みです（`actionTaken` フラグ）。
   この修正の続き以外の目的では、引き続き変更しないでください。**
5. **`CanUseSpells()` / `CanUseAbilities()` の既存の判定内容**
   プレイヤー側も使っています。AI用の価値判定は**上乗せ**する形にしてください。
6. **既存 MonoBehaviour の `[SerializeField]` の削除・リネーム**
   Unityの .meta / シーンの参照が切れます。追加は可、削除・改名は不可。

   **※例外：`CardController.cs` の `SpawnEffect` / `DirectAttack` / `LerpThrow`
   （戦闘エフェクト共通処理。プレイヤー・AI双方のカードが使う）は、
   `DESTROY_ATTACKED_TARGET` の攻撃が `DAMAGE_NULLIFY_ONCE`（未消費）を貫通して
   `at` ダメージが素通りするゲームルール上の不整合が見つかったため、
   段階2の後に別件として修正済みです（`if (!model.isDestroyer)` ガード）。
   これは「プレイヤー側の処理」ではなく両陣営共通の戦闘ルールの修正であり、
   AIEvaluator.cs の `kill`/`survive`/`zeroDamage` の式もこの修正後の挙動に
   合わせて更新済みです。この修正の続き以外の目的では、引き続き変更しないでください。**

   **※例外3：`CardController.cs` の `DirectSpellAttack` / `SpawnSpellEffect`
   （スペルがカードに当たったときのダメージ数字表示）は、
   `DESTROY_ENEMY_CARD`（破壊）が「ダメージ」ではないのに `effectDmg` の数字を表示し、
   `DAMAGE_NULLIFY_ONCE` に吸収されたときも数字だけ出て
   「効いたのに死なない」ように見える問題があったため、
   ユーザー承認のうえ表示条件を修正しました（詳細は段階4の 4-D）。
   これは両陣営共通の表示ルールの変更です。
   この修正の続き以外の目的では、引き続き変更しないでください。**

### Unity固有の制約

- **あなたはUnityでコンパイル・実行できません。** 型・メソッド名は実コードから正確に写してください。
  推測でAPIを使わないでください。
- `AIEvaluator` / `AIProfile` は **MonoBehaviour でも ScriptableObject でもないプレーンな C# クラス**にしてください。
  `.asset` ファイルの作成や Inspector への割り当てが必要になる設計は禁止です。
- `new CardController()` のような MonoBehaviour の直接 new は禁止です（既存の `AI.cs:112` にあるが、これは削除対象）。
- `System.Linq` は使用可（既存コードでも使用済み）。ターン制の処理なのでアロケーションは問題になりません。

---

## 3. 実装段階

**必ず1段階ずつ実装し、その都度コミットしてください。** まとめて実装しないでください。
各段階の「完了条件」を満たさない限り次に進まないでください。

### 進捗（新しいセッションはここを最初に見ること）

| 段階 | 状態 |
|---|---|
| 段階1：バグ修正 | **完了・コミット済み** |
| 段階2：`AIEvaluator` + 攻撃対象スコアリング | **完了・コミット済み**（レビュー指摘の修正も反映済み） |
| 段階3：リーサル判定（3-B の決着チェック含む） | **完了・コミット済み** |
| 段階4：対象選択（4-B/4-C/4-D/4-E 含む） | **完了・コミット済み** |
| 段階5：出すカードの選択（5-B/5-C 含む） | **コミット済み**。ただし **5-D の指摘が未対応** ← **次はここ** |
| 段階6：スペル価値関数 + `maxHp` | 未着手（5-D の修正後） |
| 段階7：難易度パラメータ | 未着手 |
| 段階8：クリーンアップ | 未着手 |

**この指示書に書かれている `AI.cs:NNN` などの行番号は、書かれた時点のものです。
編集のたびにズレるので、行番号ではなく「どのメソッドの、どのフラグの分岐か」で
場所を特定してください。**

### ★コミット前に必ず確認すること（検証用の値の混入）

動作確認のために書き換えた値が、**実際に2回コミットに混入しました**。
`git add -A` や `git commit -a` は使わず、**変更したファイルを個別に `git add`** してください。

コミット前に必ず `git status` を見て、以下が含まれていないか確認すること。

| ファイル | 検証で書き換わりやすい値 | 本来の値 |
|---|---|---|
| `Assets/Scripts/InGame/GamePlayerManager.cs` | `manaCost` / `defaultManaCost` | どちらも `10` |
| `Assets/Scripts/InGame/GameManager.cs` | `SettingInitHand` の draw 回数 | `2` |
| `Assets/Resources/CardEntityList/Card*.asset` | hp / at / cost / abilities / spells | 検証用に書き換えないこと |
| `Assets/Scenes/Field.unity` | `DeckBuilderUI.debugMode` / `debugOwnedCardCounts` | `debugMode: 0` |

**特に `debugMode: 1` が入ると、プレイヤーがデッキ編集画面を開いたときに
`BeginDeckEdit()` ではなく `DebugBeginDeckEdit()` が呼ばれ、
セーブデータが一切読まれずデッキ空・全カード所持の状態で始まります。**

---

### 段階1：バグ修正（先にこれだけをやる）

#### 1-A. 守護（SHIELD）フィルタが機能していない

`AI.cs:107-127` で `defender` を**フィルタ前に確定**しており、絞り込んだ `playerFieldCardList` が
一切使われていません。結果、敵AIは守護を無視して常に左端のカードを攻撃します。

修正方針：
```
1. 相手盤面を取得（Aliveフィルタ済み）
2. attacker が PIERCE を持たない かつ 相手盤面に SHIELD 持ちが存在する
   → 攻撃候補を SHIELD 持ちのみに絞る
3. 絞り込んだ「あと」に defender を決める
```

#### 1-B. `AI.cs:112` の `CardController card = new CardController();` を削除

未使用のデッドコードです。

#### 1-C. 死亡カードのフィルタ

`AI.cs` 内のすべての `GetComponentsInChildren<CardController>()` / `GetXxxFieldCards()` の結果に
1-3 の `Alive()` を通してください。攻撃者選択（`enemyCanAttackCardList`）にも適用します。

#### 1-D. `Random.Range` の off-by-one

`AI.cs:225`, `AI.cs:239`, `AI.cs:353` の `UnityEngine.Random.Range(0, xxx.Length - 1)` は
最後の要素が絶対に選ばれず、`Length == 0` のとき `Range(0, -1)` で例外になります。

```csharp
// 修正後
if (xxx.Length == 0) { /* 対象なし。効果を発動しない */ }
else target = xxx[UnityEngine.Random.Range(0, xxx.Length)];
```

**★注意：`targets` 配列を先に確保してから空チェックするだけでは不十分です。**
`targets = new CardController[hand.Length]` を空チェックの前に実行すると、
手札0枚のとき `targets` は「非nullで長さ0」になります。
この状態は後段の `if (target != null || targets != null)` を通過してしまい、
`target` が null のまま `card.AbilityEffect(null, true)` が呼ばれて
`target.transform` で NullReferenceException になります（`isAttacking` が true のまま固まる）。

対象が0件のときは **`targets` にも配列を代入しない**、もしくは後段のガードを
`if (target != null || (targets != null && targets.Length > 0))` にしてください。

**`CardController.cs` 側（421, 427, 433, 439, 988行目付近）にも同じ不具合がありますが、
そちらはプレイヤー側の処理なので今回は触らないでください。** 報告だけしてください。

#### 完了条件
- `AI.cs` 以外のファイルが変更されていないこと
- 守護持ちのカードがプレイヤー盤面にあるとき、敵AIが必ず守護カードを攻撃すること
- 演出（`WaitForSeconds`）に一切変更がないこと

---

### 段階2：`AIEvaluator` の骨組み + 攻撃対象のスコアリング

#### 2-A. `Assets/Scripts/InGame/AI/AIEvaluator.cs` を新規作成

```csharp
public static class AIEvaluator
{
    // 1-1 のラッパー群（SelfField / OppField / SelfHand / OppHand / Alive）
    // 脅威度
    public static float Threat(CardController c);
    // 盤面優劣
    public static float BoardAdvantage();
    // 攻撃の割り当て（段階2）
    public static AttackPlan NextAttack(CardController[] attackers);
}

public struct AttackPlan
{
    public CardController attacker;
    public CardController defender;  // null ならヒーローへの攻撃
}
```

#### 2-B. 脅威度スコア

```
Threat(c) = c.model.at * 1.2f + c.model.hp * 0.8f
          + (SHIELD                   ? 3f      : 0)
          + (DESTROY_ATTACKED_TARGET  ? 4f      : 0)
          + (DOUBLE_ACTION            ? c.at    : 0)
          + (DAMAGE_NULLIFY_ONCE      ? 2f      : 0)
          + (STATS_UP_ON_ATTACK       ? 2f      : 0)
          + (PIERCE                   ? 1f      : 0)
          + (HEAL_BY_DAMAGE           ? 1f      : 0)
```

判定は `c.model.abilities.HasFlag(ABILITIES.XXX)` を使ってください。

#### 2-C. 攻撃の割り当て

```
攻撃候補 defenders:
    相手盤面に SHIELD 持ちがいる かつ attacker が PIERCE を持たない
        → SHIELD 持ちのみ（ヒーローは候補に含めない）
    それ以外
        → 相手盤面の全カード + ヒーロー
```

**前提（`CardController.cs` の仕様。段階2完了後に修正済み）**：
`DAMAGE_NULLIFY_ONCE` 未消費の相手への攻撃は、`DESTROY_ATTACKED_TARGET`（破壊者）の攻撃も含めて
**「1回の攻撃イベント」として丸ごと無効化される**。以前は破壊者の攻撃だけ
「破壊試行(`Destroys()`)が無効化を消費した後、通常ダメージ(`model.Attack()`)が別枠で素通りする」
という抜け穴があったが、`SpawnEffect`/`DirectAttack`/`LerpThrow` 内の `model.Attack(enemy)` 呼び出しを
`if (!model.isDestroyer)` で囲み、破壊者の場合はこの重複適用をスキップするよう修正済み。
そのため `kill`/`survive`/`zeroDamage` の判定は以下のようにシンプルになる。

```
Score(attacker a, defender d):
    zeroDamage = d が DAMAGE_NULLIFY_ONCE を「まだ消費していない」
        → a の攻撃（破壊者でも）はダメージが1も通らない

    kill = !zeroDamage かつ (a が DESTROY_ATTACKED_TARGET を持つ または a.at >= d.hp)

    survive = 次の順で判定する
        1. a が DAMAGE_NULLIFY_ONCE を「まだ消費していない」
             → survive = true（d の反撃も破壊者含めて丸ごと無効化される）
        2. d が DESTROY_ATTACKED_TARGET を持つ（かつ a の無効化は既に消費済み/持たない）
             → survive = false（無効化が無いので破壊は必ず通る）
        3. それ以外
             → survive = (d.at < a.hp)

    kill  &&  survive                →  100 + Threat(d)
    kill  && !survive                →   50 + Threat(d) - Threat(a)
   !kill  &&  survive && zeroDamage  →  -10        ← 何も起きない。ヒーロー攻撃に負けるべき
   !kill  &&  survive                →   10 + a.at ← 削りダメージは実際に入る
   !kill  && !survive                →  -50

Score(attacker a, ヒーロー):
    a.at * faceWeight
    faceWeight = 1.0 が基準。以下で加算：
        相手ヒーローHP <= 5              → +1.5
        BoardAdvantage() > 0（盤面有利）  → +0.8
```

**「消費していない」の判定には静的な `abilities` フラグではなく、
ランタイムの `model.isDamageNullifyOnce` / `model.isDestroyer` を使ってください。**
`DAMAGE_NULLIFY_ONCE` は `CardModel.Damage()` で1回使うと `isDamageNullifyOnce` が false になりますが、
`abilities` のフラグは立ったままです。フラグで判定すると使用済みのカードを
「まだ無敵」と誤認して過大評価します。
（根拠：`Destroys()`（破壊者の攻撃）は常に `Damage()` を呼ぶため、無効化未消費なら必ずここで
フラグが消費される。`SpawnEffect`/`DirectAttack`/`LerpThrow` 内の通常ダメージ適用は
`model.isDestroyer` なら重複適用を避けるためスキップされるよう修正済みなので、
破壊者の攻撃でも「無効化未消費なら結果は不発で確定」となる）

**`zeroDamage` について補足**：ダメージが通らない攻撃でも、相手の
`isDamageNullifyOnce` は消費されます（`Damage()` はダメージを捨てる際にフラグを落とす）。
そのため「弱いカードで無効化を剥がし、別のカードで仕留める」という戦術には本来価値がありますが、
**段階2の `NextAttack` は1手ずつ選ぶ貪欲法なので、この先読みは意図的に実装しません。**
必要になったら別段階で扱ってください。

`NextAttack` は「まだ `canAttack == true` の攻撃可能カード」の全組み合わせから
最大スコアの1組を返します。スコアが全て負ならヒーローへ（守護がなければ）、
それも不可なら `attacker` を返しつつ `SetCanAttack(false)` させて次へ進めます。

#### 2-D. `AI.cs` の攻撃ループを差し替え

`AI.cs:105-142` の以下2行だけを置き換えます。

```csharp
CardController attacker = enemyCanAttackCardList[0];       // ← 削除
CardController defender = GetFirstZeroOrLess(playerFieldCardList); // ← 削除
```
↓
```csharp
AttackPlan plan = AIEvaluator.NextAttack(enemyCanAttackCardList);
CardController attacker = plan.attacker;
CardController defender = plan.defender;
```

そして `if (playerFieldCardList.Length > 0 && defender != null)` の条件を
**`if (defender != null)`** に変えます（= `defender` が null ならヒーローへ攻撃）。

**`WaitForSeconds` の行、`StartCoroutine(gameManager.CardsBattle(...))`、
`gameManager.AttackToHero(attacker)`、`gameManager.CheckHeroHP()` の呼び出しは
そのまま維持してください。**

#### 完了条件
- 敵AIが有利トレード（一方的に倒せる相手）を優先すること
- 敵AIが不利な突撃（倒せず自分だけ死ぬ）をしなくなること
- 守護がないとき、盤面にカードが残っていても敵AIがヒーローを殴る選択をすること
- `GetFirstZeroOrLess` が未使用になるが、**削除せず残す**（他から呼ばれていないか確認してから判断）

---

### 段階3：リーサル判定

**★1ターンに2回計算します。** 1回目だけでは足りないことが動作確認で判明したためです（後述）。

```
到達可能打点 =
    (プレイヤー盤面に SHIELD 持ちがいない場合)
        Σ 自陣の canAttack カードの at
    (SHIELD 持ちがいる場合)
        Σ 自陣の canAttack かつ PIERCE 持ちカードの at
  + (includeHandBurn が true のときのみ)
    Σ (手札にある SPELLS.DAMAGE_ENEMY_HERO 持ちのうち、マナ内で撃てるものの effectDmg)

if (到達可能打点 >= gameManager.player.heroHp)
    → isLethalTurn = true
```

| 計算タイミング | 場所 | `includeHandBurn` | 目的 |
|---|---|---|---|
| 1回目：ターン開始直後 | 最初の `WaitForSeconds(1)` の後、`SettingCanAttackView` の直後、召喚ループの前 | **true** | 召喚フェーズでバーンスペルを優先するかを決める |
| 2回目：攻撃フェーズ直前 | 召喚ループの後、攻撃ループの `while` の前 | **false** | 実際にヒーローを殴り切れるかを最新の盤面で決める |

**2回目が必須な理由（動作確認で発覚した不具合）**：
1. **`INIT_ATTACKABLE`（速攻）持ちを召喚したケース**
   `CardMovement.cs:277` で速攻持ちは召喚した瞬間に `SetCanAttack(true)` されるため、
   **そのターン中に攻撃できます**。しかしターン開始時点の計算では場にいないので打点に数えられず、
   `isLethalTurn = false` のままになります。その結果、攻撃フェーズで段階2の通常スコアになり、
   有利トレード（`100 + Threat`）がヒーロー攻撃（`at * faceWeight` = 数十）に勝ってしまい、
   **勝てるターンなのに敵AIがプレイヤーのカードを殴る**という挙動になります。
2. **バーンスペルを撃った後のHP減少が反映されない**
   1回目の計算は「これから撃つバーン」を打点に含めた見積もりです。実際に撃った後は
   `player.heroHp` が減っているので、盤面打点だけで足りるかを計算し直すほうが正確です。

**2回目で `includeHandBurn` を false にする理由**：召喚フェーズが終わった後は
もうスペルを撃てないため、手札のバーンを打点に数えると過大評価になります。

`isLethalTurn` が true のとき：
- 召喚フェーズでは **バーンスペル（`DAMAGE_ENEMY_HERO`）を最優先で全て使う**
- 攻撃フェーズでは `faceWeight` を極端に大きく（例: 1000f）して**必ずヒーローを殴る**

**注意**：`DOUBLE_ACTION` 持ちは2回攻撃できますが、初回は数えない安全側の見積もりにしてください
（過大評価してリーサルを外すより、見逃すほうが安全）。

マナの計算は `gameManager.enemy.manaCost` を使います（`defaultManaCost` ではない）。
バーンスペルを複数使う場合、合計コストがマナを超えないよう貪欲に選んでください。

**既知の割り切り（不具合ではないので直さないこと）**：`at == 0` のカードは
`ScoreAgainstHero` が `0 * 1000 = 0` になるため、リーサルターンでもヒーローではなく
相手カードを攻撃します。打点0なのでどちらを殴っても勝敗に影響せず、
また `at > 0` のカードのヒーロー攻撃スコア（`at * 1000`）が必ず上回るので
リーサル自体は阻害しません。

#### 3-B. 決着後に行動を続けてしまう問題（段階3で同時に修正）

ヒーローへのダメージは**非同期で適用されます**。
`CastSpellOf` は `StartCoroutine` で起動され、その中でさらに `WaitForSeconds(0.9f)` を挟んでから
`attackSpellEffectHero()` を呼び、実際に `player.heroHp` が減るのは
`SpawnSpellEffectHero` / `DirectSpellAttackHero` / `LerpThrowSpellHero` の中（さらに `attackTime` 後）です。

そのため `AI.cs` の既存のチェック

```csharp
StartCoroutine(CastSpellOf(selectCard));
...
if (GameManager.instance.player.heroHp <= 0 || ...) yield break;   // ← まだHPが減っていない
```

は**必ず素通りします**。結果、バーンスペルで勝敗が決まって結果演出が出た後も、
敵AIが手札のスペルを撃ち続けます。

修正方針：**召喚ループと攻撃ループそれぞれの先頭**、
`while (isAttacking || isSummoning)` の待機ループを抜けた直後に、決着チェックを追加してください。

```csharp
while (GameManager.instance.isAttacking || GameManager.instance.isSummoning)
{
    yield return null;
    continue;
}
// 決着済みなら以降の行動を止める
if (GameManager.instance.player.heroHp <= 0 || GameManager.instance.enemy.heroHp <= 0)
    yield break;
```

この位置なら、直前の行動（スペル・攻撃）のダメージ適用が完了してから判定されます
（`attackSpellEffectHero` は開始時に `isAttacking = true` にし、ダメージ適用時に false へ戻すため）。
**既存の `yield break` チェックは残したままで構いません**（重複しても害はありません）。

#### 完了条件
- プレイヤーHPが残り少ないとき、敵AIが確実に詰めてくること
- **速攻持ちを召喚して初めてリーサルが成立するターンでも、ヒーローを殴り切ること**
- 守護がいるときは、リーサルでも守護を無視しないこと
- リーサルが成立しないとき、従来通りの評価（段階2）で動くこと
- **勝敗が決まった後、敵AIが追加でスペルを撃ったり攻撃したりしないこと**

---

### 段階4：スペル/アビリティの対象選択（`[0]` の全廃）

`AI.cs` の以下をすべて置き換えます。

**★行番号は段階3完了時点の実測値です。編集で必ずズレるので、行番号ではなく
「どのメソッドの、どのフラグの分岐か」で場所を特定してください。**

| 場所 | 現在 | 置き換え後 |
|---|---|---|
| `AI.cs:206`（`CastAbilityOf`） | `GetEnemyFieldCards(...)[0]` | `DESTROY_ENEMY_CARD`/`STEAL_ENEMY_CARD`なら`SelectDestroyTarget(card)`、それ以外は`SelectDamageTarget(card)` |
| `AI.cs:209`（`CastAbilityOf`） | `GetFriendFieldCards(...)[0]` | `AIEvaluator.SelectHealTarget(card)` |
| `AI.cs:334`（`CastSpellOf`） | `GetEnemyFieldCards(...)[0]` | `DESTROY_ENEMY_CARD`/`STEAL_ENEMY_CARD`なら`SelectDestroyTarget(card)`、それ以外は`SelectDamageTarget(card)` |
| `AI.cs:339`（`CastSpellOf`） | `GetFriendFieldCards(...)[0]` | `HEAL_FRIEND_CARD`なら`SelectHealTarget(card)`、`CONDITIONAL_FRIEND_BUFF`なら`SelectBuffTarget(card)` |

**実装時の補足（実コードとの差分）**：
- `AI.cs:206` / `AI.cs:334` の分岐は `DESTROY_ENEMY_CARD` / `STEAL_ENEMY_CARD` も同じ `if` に
  入っているため、フラグで分岐して使い分けます（実装済み）。
- `AI.cs:339` の分岐は `HEAL_FRIEND_CARD` と `CONDITIONAL_FRIEND_BUFF` が同じ `if` に
  入っており、当初の表では `SelectBuffTarget` 一本化としていましたが、それだと
  `HEAL_FRIEND_CARD` 持ちの回復スペルにバフ用の選択（`hp`最大）が使われてしまいます。
  `CastAbilityOf` 側と同じくフラグで分岐し、`HEAL_FRIEND_CARD` → `SelectHealTarget`、
  `CONDITIONAL_FRIEND_BUFF` → `SelectBuffTarget` に修正して実装しています。
- `CastSpellOf` の同じ`if`には `SWAP_HP_ATK && EFFECT_SELECTION_ENEMY` も含まれますが、
  専用の選択ルールを定義していなかったため、`DESTROY_ENEMY_CARD`/`STEAL_ENEMY_CARD`で
  なければ`SelectDamageTarget`を使う扱いにしています（現状のテストカードには
  `SWAP_HP_ATK` を持つものがなく実害なし。将来このフラグ用のカードを追加する際は
  専用の選択ルールを検討してください）。

#### 4-B. 同時に潰す例外リスク（段階1で残っていた分）

段階1では `DISCARD_ENEMY_HAND` / `DISCARD_FRIEND_HAND` にだけ空チェックを入れましたが、
以下は**未対応のまま残っています**。すべて「対象0件のときに例外で落ちて `isAttacking` が
true のまま固まる」パターンなので、段階4で一緒に直してください。

| 場所 | 内容 | 起きること |
|---|---|---|
| `AI.cs:295` / `AI.cs:300`（`CastAbilityOf` の `RANDOM_ENEMY` / `RANDOM_FRIEND`） | 空チェックなしで `array[Random.Range(0, array.Length)]` | 盤面0体のとき `Random.Range(0,0)` が 0 を返し `array[0]` で `IndexOutOfRangeException` |
| `AI.cs:432` / `AI.cs:437`（`CastSpellOf` の `RANDOM_ENEMY` / `RANDOM_FRIEND`） | 同上 | 同上 |
| `AI.cs:426`（`CastSpellOf` の `DISCARD_FRIEND_HAND`） | `targets = new CardController[hand.Length - 1];` の直後に無条件で `target = targets[0];` | 手札がそのスペル1枚だけのとき `targets.Length == 0` となり `IndexOutOfRangeException` |

`Random.Range(int min, int max)` は `max` を含まず、`min == max` のときは例外を投げずに
`min` を返します。**したがって「長さ0の配列」は `Random.Range` では防げず、
必ず呼び出し前に `Length == 0` を判定してください。**

**実装時に追加で見つかったリスク（4-Bには元々含まれていなかった分）**：
`CastSpellOf` 末尾の `else` 節が `card.spellEffect(target, true)` を**無条件**で呼んでいます。
`SelectDamageTarget` 等が `null` を返せるようになったことで、ここで `target == null` のまま
呼ばれると `spellEffect` 内の `target.transform` で `NullReferenceException` になり、
`isAttacking` が true のまま固まります（`spellEffect` は関数の一番最初で
`isAttacking = true` にするため）。`if (target != null)` でガードして修正済みです。

選択ルール：

- **`SelectDamageTarget`**（`DAMAGE_ENEMY_CARD` / `CONDITIONAL_ENEMY_DEBUFF`）
  1. `effectDmg >= 相手hp` で倒せる相手の中で `Threat` 最大
  2. 倒せる相手がいなければ `Threat` 最大
- **`SelectDestroyTarget`**（`DESTROY_ENEMY_CARD` / `STEAL_ENEMY_CARD`）
  `Threat` 最大
- **`SelectHealTarget`**（`HEAL_FRIEND_CARD`）
  `maxHp - hp`（傷の深さ）が最大の味方。段階6で `maxHp` を追加するまでは
  **暫定的に `hp` が最小の味方**を返してください
- **`SelectBuffTarget`**（`CONDITIONAL_FRIEND_BUFF`）
  `canAttack == true` の味方のうち `hp` 最大（生き残りやすい＝バフが無駄にならない）

**対象が0体のときは必ず `null` を返し、呼び出し側で効果を発動しないようにしてください。**
現状は空配列に `[0]` でアクセスして例外になる可能性があります。

#### 4-C. 対象がいないスペルを最初から出さない（段階4の動作確認で発覚）

**症状**：`DESTROY_ENEMY_CARD` + `EFFECT_SELECTION_ENEMY` のスペルで、
「ダメージ表示が出ない」「2枚目でダメージだけ出て相手が死なない」が発生。

**根本原因：`CanUseSpells()` と `Select系` で見ている盤面が違う。**

| | 使っている取得方法 | 破壊演出中（`hp==0`/`isAlive==false`）のカード |
|---|---|---|
| `CardController.CanUseSpells()` | `GetEnemyFieldCards(...)` を**そのまま** | **対象として数える** |
| `AIEvaluator.SelectDestroyTarget` 等（段階4） | `OppField()` = `Alive()` 済み | 除外する |

そのため「`CanUseSpells()` は true（＝AIがそのスペルを選ぶ）→ `Select系` は `null` を返す」
という食い違いが起きます。こうなると `CastSpellOf` は次の状態で終了します。

1. `MoveLeftSpell` でスペルカードが画面中央へ移動する（演出だけ進む）
2. `ReduceManaCost` が呼ばれ、**マナだけ消費される**
3. `target == null` なので `spellEffect` がスキップされる
4. `spellEffect` → `UseSpellTo` が呼ばれないため、その末尾にある
   **`Destroy(this.gameObject)` も実行されず、スペルカードが消えずに残る**
5. カードは手札に残ったままなので、次のループで再び選ばれうる

**修正方針（AI側のみ。`CanUseSpells()` は変更しない）**：

`AIEvaluator` に「そのスペルが実際に効果を発揮できるか」を判定するメソッドを追加し、
`AI.cs` の `selectableHandCardList` の絞り込み条件に **`CanUseSpells()` との AND** で足します。

```csharp
// AIEvaluator
// 効果の対象が1つでも欠けているスペルは選ばない（＝カード自体を出さない）
public static bool HasValidSpellTarget(CardController card);
```

判定ルール（**対象が必要な効果が1つでも成立しないなら false**。
`EFFECT_SELECTION_*` との組み合わせかどうかで区別せず、どちらの場合もカード自体を出さない）：

| 必要な対象 | 対象のフラグ |
|---|---|
| 相手盤面に1体以上（`OppField()`） | `DAMAGE_ENEMY_CARD` / `DAMAGE_ENEMY_CARDS` / `DESTROY_ENEMY_CARD` / `CONDITIONAL_ENEMY_DEBUFF` / `STEAL_ENEMY_CARD` / `RANDOM_ENEMY` / `SWAP_HP_ATK` |
| 自盤面に1体以上（`SelfField()`） | `HEAL_FRIEND_CARD` / `HEAL_FRIEND_CARDS` / `CONDITIONAL_FRIEND_BUFF` / `RANDOM_FRIEND` |
| どちらかの盤面に1体以上 | `DESTROY_ALL_FIELD_CARDS` |
| 相手手札が1枚以上（`OppHand()`） | `INCREASE_ENEMY_COST` / `DISCARD_ENEMY_HAND` / `DISCARD_ALL_ENEMY_HAND` |
| 自手札が**自分以外に**1枚以上（`SelfHand()`） | `REDUCE_HAND_COST` / `DISCARD_FRIEND_HAND` / `DISCARD_ALL_FRIEND_HAND` |
| 自盤面に空きがある（4体以下） | `STEAL_ENEMY_CARD` / `SUMMON_SPECIFIC_UNIT` |
| 対象不要（常に true） | `DAMAGE_ENEMY_HERO` / `HEAL_FRIEND_HERO` / `DRAW_CARDS` / `HEAL_BY_DAMAGE` / `EFFECT_SELECTION_ENEMY` / `EFFECT_SELECTION_FRIEND` |

**★盤面の判定には必ず `OppField()` / `SelfField()`（`Alive()` 済み）を使ってください。**
`GetEnemyFieldCards` を直接使うと `CanUseSpells()` と同じ「死にかけのカードを数える」問題が
再発し、この修正の意味がなくなります。

**★手札の判定に `Alive()` を通さないでください**（スペルは `hp==0` のため消えます。1-3参照）。
`REDUCE_HAND_COST` / `DISCARD_FRIEND_HAND` 系は**そのスペルカード自身を除いて**数えること
（`CastSpellOf` が `hand.Length - 1` で自分を除外しているため）。

#### 完了条件
- 敵AIが除去スペルを最大の脅威に撃つこと
- 盤面が空のときに例外が出ないこと
- 手札がそのスペル1枚だけのときに `DISCARD_FRIEND_HAND` で例外が出ないこと
- 例外で `isAttacking` が true のまま固まらないこと
- **対象がいないスペルを敵AIが選ばないこと（マナだけ消費してカードが画面に residual として残らないこと）**

#### 4-D. 破壊スペルのダメージ数字表示（4-C後の動作確認で判明）

**報告された症状**：`DESTROY_ENEMY_CARD` + `EFFECT_SELECTION_ENEMY` のスペルで
「1枚目はダメージ表示が出ない」「2枚目はダメージだけ出て相手が死なない」。

**調査結果：どちらもロジックのバグではなく、表示の問題だった。**

**(1) 1枚目で数字が出ず、2枚目で出る理由 → ダメージテキストのプールの有無**

`DirectSpellAttack` / `SpawnSpellEffect` は、対象を破壊した**後**に
破壊済みの Transform を渡してダメージテキストを出そうとしている。

```csharp
UseSpellTo(enemy);          // ← CheckAlive()が Destroy(enemy.gameObject) を同期実行
...
GameObject textObj = GameManager.instance.GetTextPool();
GameManager.instance.StartCoroutine(GenDamageText(textObj, model.effectDmg, endPos));  // endPos は破壊済み
```

`GameManager.GetTextPool()` は**プールが空だと `null` を返す**。
`GenDamageText` は `text == null` のとき `Addressables.InstantiateAsync` で
**`yield return handle` を挟む**ため1フレーム以上待ち、その間に対象が実際に破棄されて
`cardTransform.position` で `MissingReferenceException` になる。

| | `GetTextPool()` | フレーム待機 | 結果 |
|---|---|---|---|
| 1枚目（プールが空） | `null` | **待つ** | 対象が破棄済みになり例外 → **表示されない** |
| 2枚目以降（プールに在庫あり） | オブジェクト | 待たない | 同フレーム内なので Transform が有効 → **表示される** |

**(2) 「死ななかった」は仕様通り（バグではない）**

対象が `DAMAGE_NULLIFY_ONCE`（未消費）を持っていたことを確認済み。
`Destroys()` → `CardModel.Destroy()` → `Damage(hp)` と辿るため、
`Damage()` 先頭の無効化チェックで**破壊が丸ごと吸収される**。
これは「無効化は1イベントとして扱う」という既存の決定と整合している。

問題は、**ダメージ数字が「実際にダメージが入ったか」と無関係に
`model.effectDmg != 0` だけで出る**ため、無効化されたのに数字だけ出て
「効いたのに死なない」ように見えてしまうこと。

**修正方針（ユーザー決定：破壊は数字を出さない）**

`DESTROY_ENEMY_CARD` は「ダメージ」ではないので、ダメージ数字を出さないようにする。
対象は**スペルがカードに当たる2箇所のみ**：

| 場所 | 現在の条件 | 修正後 |
|---|---|---|
| `CardController.DirectSpellAttack` の `OnComplete` 内 | `if (model.effectDmg != 0)` | `if (model.effectDmg != 0 && !model.spells.HasFlag(SPELLS.DESTROY_ENEMY_CARD))` |
| `CardController.SpawnSpellEffect` | `if (model.effectDmg != 0)` | 同上 |

**※`SpawnSpellEffect` の表示値のバグも同時に直すこと。**
表示条件は `model.effectDmg` を見ているのに、実際に表示している数値が `model.at` になっている
（`DirectSpellAttack` 側は `effectDmg` で統一されている）。`model.effectDmg` に揃える。

**※`CardController.cs` はプレイヤーと共通だが、これは「破壊は数字を出さない」という
両陣営共通の表示ルールの変更であり、ユーザー承認済みの例外**（2. の例外3として記載）。

#### 4-E. `GenDamageText` の `MissingReferenceException`（実機で例外を確認）

4-D の調査で「表示されないだけ」と書いた現象は、実際には**例外が出ていた**ことが
ユーザーの実行ログで確認されました。

```
MissingReferenceException: The object of type 'UnityEngine.RectTransform' has been destroyed
UnityEngine.Transform.get_position ()
GameManager+<GenDamageText>d__112.MoveNext () (at Assets/Scripts/InGame/GameManager.cs:1254)
```

`GameManager.cs:1254` は `text.transform.position = cardTransform.position;` です。

**原因**：`GenDamageText` は `text == null`（＝テキストプールが空）のとき
`Addressables.InstantiateAsync` で **`yield return handle` を挟んでから**
`cardTransform.position` を読みます。その待機の間に、
呼び出し元が `UseSpellTo` / `UseAbilitiesTo` → `CheckAlive()` → `Destroy(gameObject)` で
**対象カードを同期的に破棄している**ため、再開時には Transform が存在しません。

**影響範囲は破壊スペルだけではありません。** `CheckAlive()` を同期で呼ぶ経路すべてが対象で、
`DAMAGE_ENEMY_CARD` のスペルやアビリティでも「そのダメージで相手が死んだ」場合に同じ例外が出ます。

| メソッド | 直前に対象を破棄しうる処理 | テキストのアンカー |
|---|---|---|
| `DirectSpellAttack` | `UseSpellTo` | `endPos` |
| `SpawnSpellEffect` | `UseSpellTo` | `targetPos` |
| `SpawnEffectAbility` | `UseAbilitiesTo` | `targetPos` |
| `DirectAttackAbility` | `UseAbilitiesTo` | `endPos` |
| `LerpThrowAbility` | `UseAbilitiesTo` | `targetPos` |

※通常攻撃（`SpawnEffect`/`DirectAttack`/`LerpThrow`）は `CheckAlive()` が
`GameManager.CardsBattle` 側で待機を挟んでから呼ばれるため、この問題は起きません。

**修正方針：`GameManager.GenDamageText` 側だけを直す（呼び出し元5箇所は変更しない）**

`Transform` を受け取る既存のオーバーロードを、**yield する前に座標を確定させてから**
`Vector3` 版へ委譲する形にします。これで呼び出し元を1つも変えずに全経路が直ります。

```csharp
// 既存シグネチャは維持（呼び出し元の変更不要）
public IEnumerator GenDamageText(GameObject text, int damage, Transform cardTransform)
{
    if (cardTransform == null) yield break;
    // ★yieldする前に座標を確定させる。
    //   この後に対象カードが破棄されても、既に値を持っているので例外にならない
    yield return StartCoroutine(GenDamageText(text, damage, cardTransform.position));
}

// 新規追加：座標で受け取る版
public IEnumerator GenDamageText(GameObject text, int damage, Vector3 position)
{
    // 既存の中身をそのまま移し、cardTransform.position を position に置き換える
}
```

`GenHealText` も同じ構造ですが、そちらは `Instantiate` を同期で行っており
`yield` を挟まないため現状は例外になりません（将来 Addressables 化する場合は要注意）。

#### 参考：今回の調査で見つかった別件（今は直さない・報告のみ）

- `UseSpellTo` は各分岐で `return` しており、`return` した場合は末尾の
  `Destroy(this.gameObject)`（スペルカード自身の破棄）に到達しない。
- `spellEffect` の `switch` は `ATTACKTYPE.THROW` を処理していない。
  THROW のスペルカードを作ると `UseSpellTo` が呼ばれず、
  `isAttacking` が true のまま固まる（現状 THROW のスペルカードが無いため未発生）。
- ダメージテキストを破壊済み Transform に紐づけている構造自体は残る。
  `DESTROY_ENEMY_CARD` 以外でも「対象が死ぬ攻撃」では同じ理由で
  数字が出たり出なかったりしうる（プールの在庫次第）。根本的に直すなら
  `GenDamageText` に Transform ではなく座標(`Vector3`)を渡す形にする必要がある。

---

### 段階5：出すカードの選択（マナ最大化）

`AI.cs` の召喚ループにある `selectCard` の決定部分（`Array.Find` を2回使って
「リーサル時はバーンスペル優先 → それ以外は先頭から貪欲」で選んでいる箇所）を置き換えます。

```csharp
// AIEvaluator
public static List<CardController> ChoosePlayPlan(CardController[] hand, int mana, int freeSlots);
```

**アルゴリズム：部分集合の全探索**（手札は最大7枚程度なので 2^7 = 128 通り。計算量の問題なし）

```
制約:
    Σ cost <= mana
    モンスターの枚数 <= freeSlots  （freeSlots = 5 - 自陣のカード数）
    スペルは CanUseSpells() が true かつ HasValidSpellTarget() が true のもののみ候補に入れる
最大化:
    Σ Value(card) + Σ cost * MANA_WEIGHT     ← ★マナ消費ボーナス（MANA_WEIGHT = 1.0f）

Value(モンスター) = Threat(card) + (INIT_ATTACKABLE ? card.at : 0)
Value(スペル)     = 段階6まで暫定で card.model.cost * 1.0f
                    （= 高コストスペルを優先。段階6で本実装に差し替える）
```

**★マナ消費ボーナスの意図（動作確認を受けて追加）**：
`Value` は `Threat` ベースで**コストを一切見ない**ため、これが無いと
「3コストの強いカード」と「5コストの弱いカード」で必ず前者が選ばれ、マナが余ります。
かといってコストを最優先にすると「5コストの1/1」より弱い「3コストの3/3」を
選ばなくなってしまいます。

そこで**基本は盤面価値（`Value`）を優先しつつ、僅差ならマナを使い切るほうを選ぶ**ように、
選んだ組み合わせの合計コストに `MANA_WEIGHT = 1.0f` を掛けて加算します。

```
例：マナ5
  3コスト(Value 9)  → 9 + 3 = 12
  5コスト(Value 10) → 10 + 5 = 15  ← 僅差なので高コストが選ばれる

  3コスト(Value 48) → 48 + 3 = 51  ← 明らかに強いのでこちらが選ばれる
  5コスト(Value 5)  → 5 + 5 = 10
```

※コスト効率で上位8枚に絞る前処理（`Value / max(cost,1)`）は
**1枚単位の足切り**なので、マナ消費ボーナスは加えないでください。

**手札が8枚以上の場合は、コスト効率（`Value / max(cost,1)`）上位8枚に絞ってから全探索**して、
`2^n` が爆発しないようにガードを入れてください。

返したリストは**この順で実行**します：
1. 除去スペル（`DAMAGE_ENEMY_CARD` / `DAMAGE_ENEMY_CARDS` / `DESTROY_ENEMY_CARD`）
2. モンスター
3. その他のスペル（バフ / ドロー / ヒーロー系）

**★同じグループ内は `Value` の降順（強いカードから先）に並べてください。**
手札順のままにすると、弱いカードが先に出てしまいます。
ターン制限時間（`timeCount`）で打ち切られた場合や、途中でスペルの効果によって
盤面が埋まった場合に、**強いカードが出せずに残る**のを避けるためです。

#### 5-D. コードレビューで見つかった不具合（段階5コミット後）

**(1) `CONDITIONAL_FRIEND_BUFF` がマナだけ消費して不発になる**

`HasValidSpellTarget()` の `CONDITIONAL_FRIEND_BUFF` 判定は `SelfField().Length == 0` しか見ていませんが、
実際に対象を選ぶ `SelectBuffTarget()` は **さらに `canAttack == true` で絞り込みます**。
この粒度の違いが、以下の順序で必ず踏まれます。

1. 自陣が空の状態でターンが始まる
2. プランが「モンスター + バフスペル」になる（実行順は グループ2 モンスター → グループ3 その他スペル）
3. モンスターを召喚する。**`SettingCanAttackView` はターン先頭で1回しか呼ばれない**ので、
   このモンスターは（速攻でなければ）`canAttack == false` のまま
4. バフスペルを撃つ時点で `SelfField()` は非空なので、`AI.cs` の再チェックも通過する
5. しかし `SelectBuffTarget()` は `canAttack == true` の味方がおらず `null` を返す

結果、`CastSpellOf` 末尾の `else` 節で **`ReduceManaCost()` だけ実行され、
`spellEffect()` はスキップ**されます。さらに `UseSpellTo` 末尾の
`Destroy(this.gameObject)` に到達しないため、**スペルカード自身が破棄されません**。
`MoveLeftSpell` の `HideCard()` は CanvasGroup の alpha を 0 にするだけで
GameObject を非アクティブにしないため、そのカードは alpha=0 のまま手札に残り、
`GetComponentsInChildren` で毎ターン拾われて再選択され、**毎ターン不発＋マナ消費を繰り返します**。

→ 4-C で潰したはずの「対象がいないスペルを出してマナだけ消費し、カードが画面に残る」が
そのまま再現しています。

**修正方針**：`HasValidSpellTarget()` の判定粒度を `Select系` に揃えてください。
`CONDITIONAL_FRIEND_BUFF` は「`canAttack == true` の味方が1体以上いるか」を条件にします。

**★同じ「判定粒度のズレ」は他にもあります。`HasValidSpellTarget()` の各条件が、
対応する `Select系` の絞り込みと一致しているかを全フラグで確認してください。**

**(2) `DESTROY_ALL_FIELD_CARDS` が自分の召喚直後に自陣を巻き込む**

実行順のグループ1（除去スペル）は
`DAMAGE_ENEMY_CARD` / `DAMAGE_ENEMY_CARDS` / `DESTROY_ENEMY_CARD` しか見ておらず、
**`DESTROY_ALL_FIELD_CARDS` がグループ3（その他スペル）に落ちます**。

そのため同じターンのプランに「モンスター + 全体破壊」が入ると、
**マナを払って召喚した直後に全体破壊を撃ち、自分の新しいモンスターごと壊します**
（`CastSpellOf` の対象は `enemys.Concat(friends)` で自陣も含むため）。
部分集合探索は `Value` を単純加算するだけで、この相互作用を考慮していません。

**修正方針**：`DESTROY_ALL_FIELD_CARDS` を**グループ1（モンスターより前）**に移してください。
盤面を流してから召喚するのが正しい順序です。

#### 5-C. 探索の実装で必ず守ること（動作確認で見つかった不具合）

**(1) `freeSlots` は必ず 0 以上にクランプする**

```csharp
if (monsterCount > freeSlots) continue;
```

`freeSlots` が負（自陣が6体以上）になると、`monsterCount == 0` の**空集合すら弾かれます**
（`0 > -1` は true）。その結果、有効な組み合わせが1つも無くなって空のプランが返り、
**スペルも含めて召喚フェーズで一切何も出さなくなります**。
`SUMMON_SPECIFIC_UNIT` / `STEAL_ENEMY_CARD` は上限チェックを迂回して盤面を増やせるため、
6体以上は実際に起こりえます。呼び出し側・受け取り側の両方でクランプしてください。

**(2) スコアが同点のときは「枚数が多いほう」を選ぶ**

現状は空集合（スコア0）を最初の基準にしており、`score > bestValue` で比較しています。
そのため **`Value` もコストも0になるカード（＝コスト0のスペル）だけのプランはスコア0となり、
空集合に勝てず永久に選ばれません**。
コスト0のカードは撃たない理由が無いので、**同点なら枚数が多い組み合わせを選ぶ**
タイブレークを入れてください。

**`AI.cs` の召喚ループの構造（`while` + 待機ループ + `WaitForSeconds`）は維持し、
「次に出す1枚を返す」形で `ChoosePlayPlan` の結果を先頭から消費してください。**
毎ループで再計算すると盤面変化で不整合が起きるため、
**ターン開始時に1回だけ計算し、リストを消費する形**にします。
ただし、消費前に「まだマナが足りるか」「まだ盤面に空きがあるか」は毎回チェックしてください
（スペルの効果で盤面が変わるため）。

#### 5-B. 段階3・段階4で入れた処理を壊さないこと（重要）

段階5は召喚ループの選択部分を丸ごと置き換えるため、以下を**必ず引き継いで**ください。

1. **リーサル時のバーンスペル最優先（段階3）**
   現在は `AIEvaluator.isLethalTurn` が true のとき `SPELLS.DAMAGE_ENEMY_HERO` 持ちを
   `Array.Find` で先に選んでいます。`ChoosePlayPlan` に置き換えた後も、
   **リーサル時はバーンスペルが最優先で全て使われる**ようにしてください
   （例：`isLethalTurn` のとき `Value(DAMAGE_ENEMY_HERO持ち)` を極端に大きくする、
   あるいは実行順のリストの先頭に固定する）。
2. **`HasValidSpellTarget` による除外（段階4-C）**
   候補に入れる条件は `CanUseSpells() && HasValidSpellTarget(card)` です。
   これを落とすと「対象がいないスペルを出してマナだけ消費し、カードが画面に残る」
   不具合が再発します。
3. **`while` の継続条件と候補の絞り込み条件を一致させること**
   現在この2つは同じ式になっています。片方だけ変えると
   「ループは回るが選ばれるカードが無い」状態になります。
4. **盤面5体の上限（1-4）**
   既存の `Alive(...).Length > 4 && card.model.spells == SPELLS.NONE` の意味
   （＝盤面が埋まっていてもスペルは使える）を変えないでください。
5. **決着チェック（段階3-B）**
   待機ループ直後の `heroHp <= 0` の `yield break` を消さないでください。

#### ★検証時の注意：敵のマナを変えるときは `defaultManaCost` を変えること

`GamePlayerManager.Init()` の `manaCost` だけを変えても**効きません**。
`GameManager.cs` は敵ターン開始時に毎回 `enemy.IncreaseManaCost()` を呼んでおり、

```csharp
public void IncreaseManaCost()
{
    defaultManaCost++;
    if (defaultManaCost > 10) defaultManaCost = 10;
    manaCost = defaultManaCost;   // ★ここで manaCost が上書きされる
}
```

となっているため、**AIが動く時点のマナは常に `defaultManaCost`（クランプ後）**です。
`Init()` で `manaCost = 5` としても、実際には10で動きます。

「マナ5で検証したい」場合は `defaultManaCost` 側を調整してください
（`IncreaseManaCost()` が先に `++` するため、初手を5にしたいなら `defaultManaCost = 4`）。

**この取り違えのせいで「3コストと5コストの両方が出せてしまい（3+5=8≦10）、
手札順で3コストが先に出ただけ」なのを「5コストが選ばれない」と誤認した実績があります。**

#### 完了条件
- **ステータスが同程度の3コストと5コストがあり、マナが5のとき、5コストを出すこと**
  （旧版では「マナ5・手札[3,3,5]のとき3ではなく5を出す」と書いていたが、
  `Value` はステータスだけで決まるため、3コスト側が明らかに強い場合は
  3コストが選ばれるのが正しい。ステータスを揃えて確認すること）
- **3コスト側が明らかに強い場合は、マナが余っても3コストを出すこと**（価値優先が壊れていない）
- **両方出せるマナがある場合は、強いほう（Valueが高いほう）から先に出すこと**
- 盤面が5体埋まっているとき、モンスターを出そうとしないこと（スペルは使う）
- リーサルターンにバーンスペルが最優先で使われること（段階3の挙動が維持されている）
- 対象がいないスペルを出さないこと（段階4-Cの挙動が維持されている）
- 無限ループしないこと（**必ずUnityで1試合通して確認してもらうこと**）

---

### 段階6：スペル価値関数 + `maxHp`（リスク高・最後に実施）

#### 6-A. `CardModel` に `maxHp` を追加

```csharp
public int maxHp;
```

設定箇所（**すべて漏れなく**）：
1. `CardModel(int cardID, bool isPlayerCard)` コンストラクタ内で `maxHp = hp;`
2. `targetCards[i]` の初期化ループ内で `targetCards[i].maxHp = targetCards[i].hp;`
3. `CardController.ApplyRoguelike()` は `hp` を書き換えるため、**その処理の後**に `model.maxHp = model.hp;`
4. `RecoveryHP(int point)` 内で、回復・バフで `hp` が `maxHp` を超えたら `maxHp = hp;` に追従

**重要：回復に上限（`hp` を `maxHp` で clamp する処理）は絶対に入れないでください。**
プレイヤー側のゲームバランスが変わってしまいます。
`maxHp` は **AIの評価にのみ使う参照値** です。

#### 6-B. スペル価値関数

**★最優先で直すこと：`Value` のスケール不一致（段階5の動作確認で判明）**

段階5の暫定実装では以下のようになっており、**桁が2つ近く違います**。

| | 式 | 実測レンジ |
|---|---|---|
| `Value(モンスター)` | `Threat(card) + (速攻 ? at : 0)` | だいたい 5〜50 |
| `Value(スペル)` | `card.model.cost * 1.0f`（暫定） | 最大でも 10 |

そのため**敵AIはほぼ常にモンスターを優先し、スペルが使われにくくなっています**。
下表で本実装に差し替える際は、**モンスターの `Threat` と同じ土俵に乗る大きさ**に
なっているかを必ず確認してください（例：除去スペルは `Threat(除去できる相手)` を
そのまま使うので自然に揃う）。


**★段階4-C との役割分担（重複させないこと）**

下表の「0にする条件（＝使わない）」列には
「相手盤面が空」「相手手札0枚」「自盤面が5体」など、
**すでに `HasValidSpellTarget()`（段階4-C）が弾いている条件**が含まれています。

- **「対象が物理的に存在するか」＝ `HasValidSpellTarget()` の責務**（候補にすら入れない）
- **「対象はいるが撃つ価値があるか」＝ `Value()` の責務**（例：`DESTROY_ENEMY_CARD` を雑魚に温存する、
  傷が浅い味方に回復を撃たない、盤面有利なら全体破壊を撃たない）

下表を実装する際は、**すでに 4-C が保証している条件を `Value()` 側で再実装しないでください**。
`Value()` は「候補に残っているカード」だけを評価すればよいので、
判定が二重になると、条件が食い違ったときに原因追跡が難しくなります。

`Value(スペル)` を以下に差し替えます。
候補の絞り込みは **`CanUseSpells()` かつ `HasValidSpellTarget()`**（段階5の実装）を維持してください。

| フラグ | Value | 0にする条件（＝使わない） |
|---|---|---|
| `DAMAGE_ENEMY_CARD` | 倒せる相手がいれば `Threat(その相手)`、いなければ `effectDmg * 0.5f` | 相手盤面が空 |
| `DESTROY_ENEMY_CARD` | `Threat(最大脅威)` | 最大 `Threat` < 6（雑魚に温存） |
| `DAMAGE_ENEMY_CARDS` | `Σ Threat(effectDmgで倒せる相手)` | 倒せるのが1体以下 |
| `DESTROY_ALL_FIELD_CARDS` | `Σ Threat(相手盤面) - Σ Threat(自盤面)` | **上式が 0 以下（＝盤面有利なら撃たない）** |
| `HEAL_FRIEND_CARD` / `HEAL_FRIEND_CARDS` | `Σ min(effectHeal, maxHp - hp)` | 上式が2未満（傷が浅い） |
| `DAMAGE_ENEMY_HERO` | **リーサル時は現在の実装（`10000f`）をそのまま維持**。それ以外は `effectDmg * (プレイヤーHP <= 5 ? 2.0f : 0.8f)` | なし |
| `HEAL_FRIEND_HERO` | 自HPが低いほど加点：`effectDmg * (自HP <= 4 ? 2.0f : 0.5f)` | 自HPが満タン付近 |
| `STEAL_ENEMY_CARD` | `Threat(最大脅威) * 2f` | 自盤面が5体で埋まっている |
| `DRAW_CARDS` | `effectDmg * 2f`（`effectDmg` = ドロー枚数） | 自手札が5枚以上 |
| `CONDITIONAL_ENEMY_DEBUFF` | `effectDmg * 1.0f` | 相手盤面が空 |
| `CONDITIONAL_FRIEND_BUFF` | `effectDmg * 1.0f` | 自盤面が空 |
| `SWAP_HP_ATK` | 相手の `at > hp` のカードがあれば `Threat` 差分 | 該当なし |
| `DISCARD_ENEMY_HAND` / `DISCARD_ALL_ENEMY_HAND` | `相手手札枚数 * 1.5f` | 相手手札0枚 |
| `INCREASE_ENEMY_COST` | `相手手札枚数 * 1.0f` | 相手手札0枚 |
| `REDUCE_HAND_COST` | `自手札枚数 * 0.8f` | 自手札1枚以下 |
| `DISCARD_FRIEND_HAND` / `DISCARD_ALL_FRIEND_HAND` | **常に 0**（自分の首を絞めるだけ） | 常に |
| `SUMMON_SPECIFIC_UNIT` | `Σ Threat(targetCards)` | 自盤面が5体 |

**注意：`SPELLS` は `[Flags]` なので複数フラグを持つカードがあります。**
`if / else if` ではなく **すべてのフラグを走査して Value を合計**してください。

**★段階5で入れた仕組みを壊さないこと**
- `MANA_WEIGHT`（マナ消費ボーナス）と、スコア同点時の「枚数が多いほう」タイブレークは維持する
- リーサル時のバーンスペル最優先（実行順の先頭固定）は維持する
- 実行順のグループ分けと、グループ内 `Value` 降順ソートは維持する
- `Value(モンスター)` の式は変更しない（今回はスペル側のスケールを合わせるのが目的）

#### 完了条件
- 全快の味方に回復スペルを撃たなくなること
- 盤面有利なときに `DESTROY_ALL_FIELD_CARDS` を撃たなくなること
- 相手手札0枚のときにハンデスを撃たなくなること
- **盤面に空きがあるときでも、価値の高いスペルがモンスターに埋もれず選ばれること**
  （段階5で判明したスケール不一致が解消されていること）
- 段階3〜5の挙動（リーサル、対象なしスペルの除外、マナ最大化）が維持されていること

---

### 段階7：難易度パラメータ（当初案から変更）

**ScriptableObject にはしないでください。** `.asset` の作成とInspector割り当てが必要になり、
あなたの手では完結できません。プレーンな enum + static テーブルにします。

```csharp
// Assets/Scripts/InGame/AI/AIProfile.cs
public enum AIDifficulty { EASY, NORMAL, HARD }

public class AIProfile
{
    public float mistakeRate;   // この確率で最善手ではなくランダムな合法手を選ぶ
    public float faceWeight;    // ヒーローを狙う重み
    public bool  useLethalCheck;

    public static AIProfile Get(AIDifficulty d) { /* switch でハードコード */ }
}
```

適用方法：
1. `AI.cs` に `public AIDifficulty difficulty = AIDifficulty.NORMAL;` を追加
   （**public フィールドなので Inspector に露出するが、デフォルト値で動くため手作業は不要**）
2. `EnemyData`（ローグライク用SO）に `public AIDifficulty aiDifficulty = AIDifficulty.NORMAL;` を追加
   既存の `.asset` はデフォルト値でデシリアライズされるため、アセットの作り直しは不要
3. 戦闘開始時に `EnemyData` から `AI.difficulty` へ流し込む
   → **どこで `EnemyData` が戦闘シーンに渡っているかを `RoguelikeSession` / `GameManager.StartGame` 周辺で
     必ず実コードで確認してから実装してください。** 推測で書かないこと。

---

### 段階8：残りのクリーンアップ

- 段階1で残した `GetFirstZeroOrLess` が本当に未使用なら削除（**先に全プロジェクトを grep して確認**）
- `AI.cs` 冒頭の未使用 `using`（`static UnityEngine.Rendering.GPUSort` 等）の整理
- `CardController.cs` 側の `Random.Range(0, len - 1)` について、**修正はせず**、
  該当行を一覧にして報告

---

## 4. 各段階の検証手順

**あなたはUnityを実行できません。** 各段階の完了時に、以下のフォーマットで
「ユーザーに確認してもらうこと」を明示してください。

```
## 段階N 完了

### 変更ファイル
- xxx.cs (+12/-5)

### Unityで確認してほしいこと
1. Unityエディタでコンパイルエラーが出ないこと
2. （具体的な確認シナリオ。例：守護持ちカードを場に出して敵ターンを迎え、
    敵が守護カードを攻撃するか）
3. 演出のタイミングが従来と変わっていないこと

### 想定される副作用
- （あれば）
```

コンパイルエラーの可能性がある箇所（型が不確かなAPIを使った箇所）は、
**自分から先にリストアップして報告してください。**

---

## 5. コーディング規約

- コメントは日本語（既存コードに合わせる）
- 既存コードのインデント・命名（camelCase フィールド、PascalCase メソッド）に合わせる
- 既存のコメントアウトされたコードブロックは**削除しない**（作者が意図的に残している）
- `Debug.Log` は既存の `Debug.Log("Enemyのターン")` 以外に追加しない
  （デバッグ用に追加した場合は、その段階の完了時に必ず削除する）

---

## 6. やってはいけないこと（再掲・最重要）

1. `WaitForSeconds` の値・位置・個数を変えない
2. プレイヤー側の処理（`AttackedCard` / `AttackedHero` / `DropPlace` / `CardMovement` /
   `CardController.ActAbility`）を変えない
3. 複数の段階をまとめて実装しない
4. `.asset` / `.prefab` / `.unity` / `.meta` ファイルを作成・編集しない
5. 実コードを読まずに、この指示書の記述だけを根拠に実装しない
6. 回復処理に `maxHp` の上限を導入しない（ゲームバランスが変わる）