# 敵AI強化 実装指示書

対象: `Assets/Scripts/InGame/AI.cs` / `Assets/Scripts/InGame/AI/AIEvaluator.cs`
目的: 敵AIの意思決定を「配列の先頭固定」から「盤面評価によるスコアリング」に置き換える

段階1〜6は完了済みです。**「4. 確定した仕様」が現在の実装の正**であり、
そこに書かれたルールを壊さないことが最優先です。

---

## 0. 最初にやること

実装前に、触る範囲の実コードを読んでください。要約や grep だけで進めないでください。

```
Assets/Scripts/InGame/AI.cs
Assets/Scripts/InGame/AI/AIEvaluator.cs
Assets/Scripts/InGame/CardEntity.cs        ← ABILITIES / SPELLS の enum 定義
Assets/Scripts/InGame/CardController.cs    ← CanUseSpells / UseSpellTo / UseAbilitiesTo
Assets/Scripts/InGame/CardModel.cs
Assets/Scripts/InGame/GameManager.cs       ← GetXxxFieldCards / CardsBattle / AttackToHero
```

この指示書と実コードが食い違ったら、**実コードを正**とし、「指示書のここが違った」と報告してください。

**行番号は信用しないでください。** 編集のたびにズレます。
「どのメソッドの、どのフラグの分岐か」で場所を特定してください。

---

## 1. 絶対に間違えるポイント

### 1-1. `GetEnemyFieldCards` / `GetFriendFieldCards` の引数の意味

引数は **「返るカードの持ち主」ではなく「問い合わせる側の視点」** です。

```
GetEnemyFieldCards(true)   → 敵AI側のカード
GetEnemyFieldCards(false)  → プレイヤー側のカード
GetFriendFieldCards(true)  → プレイヤー側のカード
GetFriendFieldCards(false) → 敵AI側のカード
```

敵AIのカードは `model.isPlayerCard == false` です。
**新しく書くコードでは必ず `AIEvaluator` のラッパーだけを使ってください。**

```csharp
SelfField()  // AI自陣（Alive済み）
OppField()   // プレイヤー盤面（Alive済み）
SelfHand()   // AI手札（Alive通さない）
OppHand()    // プレイヤー手札（Alive通さない）
```

既存の `AI.cs` は逆の慣習（`GetEnemyFieldCards(true)` で自陣）で書かれた箇所が残っています。
結果は正しいので、置き換え対象でなければ触らないでください。

### 1-2. ダメージ量のフィールドが効果ごとに違う

| 行為 | 使われる値 |
|---|---|
| 通常攻撃（カードへ / ヒーローへ） | `model.at` |
| スペル・アビリティのダメージ（カードへ / ヒーローへ） | **`model.effectDmg`** |
| `EffectHeal()` による回復 | `model.effectHeal` |
| `Heal()` による回復 | **`model.at`** |
| `CONDITIONAL_ENEMY_DEBUFF` / `CONDITIONAL_FRIEND_BUFF` | `model.effectDmg`（**ATKの増減量**。ダメージではない） |
| `INCREASE_ENEMY_COST` / `REDUCE_HAND_COST` | `model.effectDmg`（**コストの増減量**。ダメージではない） |
| `DRAW_CARDS` | `model.effectDmg`（**ドロー枚数**） |

### 1-3. 破壊演出中のカードが取得結果に混ざる

`GetComponentsInChildren<CardController>()` は `Destroy()` 予約済みのオブジェクトも返します。
**盤面の取得結果は必ず `AIEvaluator.Alive()` を通してください。**

```csharp
Array.FindAll(src, c => c != null && c.model != null && c.model.isAlive && c.model.hp > 0)
```

**★手札には絶対に使わないでください。** スペルカードは `hp == 0` なので、
手札に適用すると敵がスペルを一切使えなくなります。

### 1-4. 盤面の上限は5体

`Length > 4` で新規モンスター配置がブロックされます。
**スペルは盤面が埋まっていても使えます。** この条件式の意味を変えないでください。

---

## 2. 触ってよいもの / 触ってはいけないもの

### 触ってよい

- `Assets/Scripts/InGame/AI/AIEvaluator.cs`
- `Assets/Scripts/InGame/AI/AIProfile.cs`（段階7で新規作成）
- `Assets/Scripts/InGame/AI.cs`（判断部分のみ）

### 絶対に触ってはいけない

1. **`AI.cs` の `yield return new WaitForSeconds(...)` の値・位置・個数**（演出タイミングの調整済み定数）
2. **`while (isAttacking || isSummoning)` の待機ループ**（演出の同期に必須）
3. **`gameManager.timeCount > 0` のループ条件**（ターン制限時間のガード）
4. **プレイヤー側の処理**
   `AttackedCard.cs` / `AttackedHero.cs` / `DropPlace.cs` / `CardMovement.cs` /
   `CardClickManager.cs` / `CardController.OnFiledAbilities()`
5. **`CanUseSpells()` / `CanUseAbilities()` の既存の判定内容**（プレイヤーも使う。AI用判定は上乗せする）
6. **既存 MonoBehaviour の `[SerializeField]` の削除・リネーム**（追加は可）
7. **回復処理に `maxHp` の上限（clamp）を導入しない**（ゲームバランスが変わる）

### すでに修正済みの箇所（＝勝手に元へ戻さないこと）

以下は**ユーザー承認のうえ意図的に変更済み**です。この修正の続き以外の目的では変更しないでください。

| ファイル・メソッド | 何をしたか | 理由 |
|---|---|---|
| `CardController.ActAbility()` | `actionTaken` フラグを追加 | パッシブ系アビリティ単体のカードで `isAttacking` が true のまま固まる既存バグ |
| `CardController.SpawnEffect` / `DirectAttack` / `LerpThrow` | `if (!model.isDestroyer)` ガード | 破壊者の攻撃が `DAMAGE_NULLIFY_ONCE` を貫通して `at` ダメージが素通りしていた |
| `CardController.ShowsDamageNumber()` と5つの呼び出し箇所 | ダメージ以外の効果では数字を出さない | 「効いたのに死なない」ように見える表示バグ（詳細は 4-6） |
| `GameManager.GenDamageText` | `Vector3` 版オーバーロードを追加し、`Transform` 版は yield 前に座標を確定させてから委譲 | 対象を破棄した直後に破棄済み Transform を渡して `MissingReferenceException` |
| `CardModel.maxHp` と4つの設定箇所 | AIの評価用の参照値として追加 | 段階6（回復の上限には**使わない**） |

### Unity固有の制約

- **あなたはUnityでコンパイル・実行できません。** 型・メソッド名は実コードから正確に写してください。
- `AIEvaluator` / `AIProfile` は **MonoBehaviour でも ScriptableObject でもないプレーンな C# クラス**にすること。
  `.asset` 作成や Inspector 割り当てが必要になる設計は禁止です。
- `new CardController()` のような MonoBehaviour の直接 new は禁止。
- `System.Linq` は使用可。

---

## 3. 進捗

| 段階 | 状態 |
|---|---|
| 段階1：バグ修正 | 完了・コミット済み |
| 段階2：`AIEvaluator` + 攻撃対象スコアリング | 完了・コミット済み |
| 段階3：リーサル判定 | 完了・コミット済み |
| 段階4：対象選択 | 完了・コミット済み |
| 段階5：出すカードの選択 | 完了・コミット済み |
| 段階6：スペル価値関数 + `maxHp` | 完了・コミット済み |
| 4-6：ダメージ数字の表示範囲 | 完了・コミット済み |
| 段階7：難易度パラメータ | **見送り（実装しない）** |
| **段階8：クリーンアップ** | **未着手 ← 次はここ（最後）** |

**段階7を見送った理由**：難易度は作らず統一する方針になったため（ユーザー判断）。
`AIProfile` / `AIDifficulty` は作りません。将来必要になったら
「4-1 の `faceWeight`」「4-2 のリーサル判定の有効/無効」「`NextAttack` のランダム化」の
3点に差し込む形になります。

### ★コミット前に必ず確認すること

動作確認のために書き換えた値が、**実際に2回コミットに混入しました**。
`git add -A` / `git commit -a` は使わず、**変更したファイルを個別に `git add`** してください。

| ファイル | 検証で書き換わりやすい値 | 本来の値 |
|---|---|---|
| `GamePlayerManager.cs` | `manaCost` / `defaultManaCost` | どちらも `10` |
| `GameManager.cs` | `SettingInitHand` の draw 回数 | `2` |
| `Assets/Resources/CardEntityList/Card*.asset` | hp / at / cost / abilities / spells | 検証用に書き換えない |
| `Assets/Scenes/Field.unity` | `DeckBuilderUI.debugMode` | `0` |

**`debugMode: 1` が入ると、プレイヤーがデッキ編集画面を開いたときに
セーブデータが読まれず、デッキ空・全カード所持の状態で始まります。**

### ★検証時の注意：敵のマナを変えるときは `defaultManaCost` を変えること

`GameManager` は敵ターン開始時に毎回 `enemy.IncreaseManaCost()` を呼び、
その中で `manaCost = defaultManaCost`（クランプ後）に上書きします。
**`Init()` の `manaCost` だけ変えても効きません。**
「マナ5で検証したい」なら `defaultManaCost = 4` にしてください（先に `++` されるため）。

---

## 4. 確定した仕様（段階1〜6の結果）

**ここが現在の実装の正です。以下のルールを壊さないでください。**

### 4-1. 攻撃対象の選び方（`NextAttack` / `ScoreAgainstCard` / `ScoreAgainstHero`）

```
Threat(c) = at*1.2 + hp*0.8
          + SHIELD 3 / DESTROY_ATTACKED_TARGET 4 / DOUBLE_ACTION +at
          + DAMAGE_NULLIFY_ONCE 2 / STATS_UP_ON_ATTACK 2 / PIERCE 1 / HEAL_BY_DAMAGE 1
```

守護がいて攻撃側が `PIERCE` を持たない場合、**対象は守護のみ**（ヒーローは候補外）。
これはリーサルターンでも同じです。

```
Score(attacker a, defender d):
    zeroDamage = d の DAMAGE_NULLIFY_ONCE が未消費
        → a の攻撃は破壊者でもダメージが1も通らない（1回の攻撃イベントとして丸ごと無効化）

    kill = !zeroDamage かつ (a が破壊者 または a.at >= d.hp)

    survive = 1. a の無効化が未消費 → true
              2. d が破壊者 → false
              3. それ以外 → (d.at < a.hp)

    kill && survive   →  100 + Threat(d)
    kill && !survive  →   50 + Threat(d) - Threat(a)
   !kill && survive && zeroDamage → -10   ← 何も起きない。ヒーロー攻撃に負けるべき
   !kill && survive  →   10 + a.at
   !kill && !survive →  -50

Score(attacker a, ヒーロー):
    リーサルターン → a.at * 1000
    それ以外       → a.at * faceWeight（1.0 + 相手HP<=5で+1.5 + 盤面有利で+0.8）
```

**判定には静的な `abilities` ではなくランタイムの `isDestroyer` / `isDamageNullifyOnce` を使うこと。**
`DAMAGE_NULLIFY_ONCE` は1回使うと `isDamageNullifyOnce` が false になりますが、
`abilities` のフラグは立ったままです。

### 4-2. リーサル判定（`CalculateLethalTurn`）

**1ターンに2回計算します。**

| タイミング | 場所 | `includeHandBurn` |
|---|---|---|
| ターン開始直後 | `SettingCanAttackView` の直後、召喚ループの前 | **true** |
| 攻撃フェーズ直前 | 召喚ループの後、攻撃ループの前 | **false** |

```
到達可能打点 = Σ 自陣の canAttack カードの at
               （相手に守護がいる場合は PIERCE 持ちの分だけ）
             + includeHandBurn なら Σ 手札のバーンスペルの effectDmg（マナ内で貪欲に）

isLethalTurn = 到達可能打点 >= player.heroHp
```

**2回目が必須な理由**：`INIT_ATTACKABLE`（速攻）持ちは召喚した瞬間に攻撃可能になるため、
ターン開始時点の計算では打点に数えられません。1回目だけだと
「勝てるターンなのにカードを殴る」が起きます。
2回目で `includeHandBurn = false` にするのは、召喚フェーズ後はもうスペルを撃てないためです。

**呼ぶたびに必ず `isLethalTurn = false` にリセットしてから再計算すること。**
忘れると一度リーサルが成立した後は永久にヒーローだけを殴り続けます。

`DOUBLE_ACTION` の2回目は数えません（安全側の見積もり）。

### 4-3. 決着後に行動を止める

ヒーローへのダメージは**非同期で適用されます**。
`StartCoroutine(CastSpellOf(...))` の直後に `heroHp` を見ても、まだ減っていません。

**召喚ループと攻撃ループの両方**で、`while (isAttacking || isSummoning)` の待機ループを
抜けた直後に決着チェックを入れてあります。この位置なら直前の行動のダメージ適用が完了しています。

```csharp
if (GameManager.instance.player.heroHp <= 0 || GameManager.instance.enemy.heroHp <= 0)
    yield break;
```

### 4-4. スペル/アビリティの対象選択

`AI.cs` の `CastAbilityOf` / `CastSpellOf` は、フラグに応じて `AIEvaluator` の選択関数を呼びます。

| 選択関数 | 対象フラグ | ルール |
|---|---|---|
| `SelectDamageTarget` | `DAMAGE_ENEMY_CARD` / `CONDITIONAL_ENEMY_DEBUFF` / `SWAP_HP_ATK` | `effectDmg` で倒せる相手の中で `Threat` 最大、いなければ全体で `Threat` 最大 |
| `SelectDestroyTarget` | `DESTROY_ENEMY_CARD` / `STEAL_ENEMY_CARD` | `Threat` 最大 |
| `SelectHealTarget` | `HEAL_FRIEND_CARD` | `hp` 最小の味方 |
| `SelectBuffTarget` | `CONDITIONAL_FRIEND_BUFF` | **`SelfField()` 全体**のうち `hp` 最大 |

**`SelectBuffTarget` を `canAttack` で絞らないこと。**
`AttackBuff()` は `at += effectDmg` という恒久的な効果で、そのターンに攻撃できるかは無関係です。

**対象が0体のときは必ず `null` を返し、呼び出し側で効果を発動しないこと。**

#### `HasValidSpellTarget()` — 対象がいないスペルを最初から出さない

`CanUseSpells()` は `GetEnemyFieldCards()` をそのまま使うため、**破壊演出中のカードも
対象として数えます**。一方 `Select系` は `Alive()` 済みの `OppField()` を使うのでズレます。
このズレを埋めるため、`AI.cs` の候補条件を `CanUseSpells() && HasValidSpellTarget()` にしています。

**★これを外すと「対象がいないスペルを出してマナだけ消費し、カードが画面に残り、
毎ターン再選択される」状態に戻ります。**

| 必要な対象 | フラグ |
|---|---|
| `OppField()` に1体以上 | `DAMAGE_ENEMY_CARD` / `DAMAGE_ENEMY_CARDS` / `DESTROY_ENEMY_CARD` / `CONDITIONAL_ENEMY_DEBUFF` / `STEAL_ENEMY_CARD` / `RANDOM_ENEMY` / `SWAP_HP_ATK` |
| `SelfField()` に1体以上 | `HEAL_FRIEND_CARD` / `HEAL_FRIEND_CARDS` / `CONDITIONAL_FRIEND_BUFF` / `RANDOM_FRIEND` |
| どちらかの盤面に1体以上 | `DESTROY_ALL_FIELD_CARDS` |
| `OppHand()` が1枚以上 | `INCREASE_ENEMY_COST` / `DISCARD_ENEMY_HAND` / `DISCARD_ALL_ENEMY_HAND` |
| 自手札が**自分以外に**1枚以上 | `REDUCE_HAND_COST` / `DISCARD_FRIEND_HAND` / `DISCARD_ALL_FRIEND_HAND` |
| 自盤面に空きがある（4体以下） | `STEAL_ENEMY_CARD` / `SUMMON_SPECIFIC_UNIT` |
| 対象不要 | `DAMAGE_ENEMY_HERO` / `HEAL_FRIEND_HERO` / `DRAW_CARDS` / `HEAL_BY_DAMAGE` / `EFFECT_SELECTION_*` |

**★`HasValidSpellTarget` の判定粒度は、対応する `Select系` の絞り込みと一致させること。**
片方だけ厳しい／緩いと、「対象ありと判定 → `Select系` が null → マナだけ消費」が起きます。

#### 空配列アクセスの防止

`Random.Range(int min, int max)` は `min == max` のとき例外を投げずに `min` を返します。
**長さ0の配列は `Random.Range` では防げません。** 必ず呼び出し前に `Length == 0` を判定してください。
（`RANDOM_ENEMY` / `RANDOM_FRIEND` / `DISCARD_FRIEND_HAND` の `targets[0]` で対応済み）

### 4-5. 出すカードの選択（`ChoosePlayPlan`）

ターン開始時に**1回だけ**計算し、返したリストをインデックスで先頭から消費します。
毎ループで再計算すると盤面変化で不整合が起きるためです。

```
候補:
    モンスター … 全部
    スペル     … CanUseSpells() かつ HasValidSpellTarget() かつ Value(card) > 0
    （手札が8枚超なら Value/max(cost,1) の上位8枚に絞ってから全探索）

制約:
    Σ cost <= mana
    モンスターの枚数 <= freeSlots（= 5 - 自陣の数。★0未満にクランプすること）

最大化:
    Σ Value + Σ cost * MANA_WEIGHT(1.0)
    （同点なら枚数が多いほうを選ぶ）

Value(モンスター) = Threat(card) + (INIT_ATTACKABLE ? card.at : 0)
Value(スペル)     = SpellValue(card)（4-6参照）
```

**★`Value(card) > 0` の条件を外さないでください。**
スコアが `Σ Value + Σ cost * MANA_WEIGHT` なので、**価値0のスペルでもコスト分の
ボーナスだけで空集合に勝ってしまいます**。これを外すと
「盤面有利なのに全体破壊を撃つ」「常に価値0の `DISCARD_FRIEND_HAND` を撃つ」が復活します。

**★`freeSlots` を0未満にクランプすること。** 負のままだと `monsterCount > freeSlots` で
空集合すら弾かれ、**召喚フェーズで一切何も出さなくなります**。

**★`MANA_WEIGHT` の意図**：`Value` はコストを見ないため、これが無いと
「3コストの強いカード」が常に「5コストの弱いカード」に勝ち、マナが余ります。
逆にコスト最優先にすると弱い高コストを掴みます。
**基本は盤面価値、僅差ならマナを使い切る**というバランスです。

#### 実行順

```
0. （リーサルターンのみ）バーンスペル（DAMAGE_ENEMY_HERO）
1. 除去スペル（DAMAGE_ENEMY_CARD / DAMAGE_ENEMY_CARDS / DESTROY_ENEMY_CARD / DESTROY_ALL_FIELD_CARDS）
2. モンスター
3. その他のスペル
   ※各グループ内は Value 降順
```

**★`DESTROY_ALL_FIELD_CARDS` をグループ1（モンスターより前）から動かさないでください。**
`CastSpellOf` の対象は `enemys.Concat(friends)` で自陣も含むため、
モンスターの後だと召喚した直後に自分のモンスターごと壊します。

#### 消費時の再チェックとループ終了性

消費の直前に「カードがまだ存在するか」「マナが足りるか」「盤面に空きがあるか」
「スペルの対象がまだいるか」を毎回チェックし、駄目ならスキップします
（直前のカードの効果で状況が変わるため）。

**ループは `playPlanIndex < playPlan.Count` で回し、本体の先頭で必ずインデックスを進めます。**
`playPlan` はターン開始時に固定した有限リストなので、最大 `Count` 回で必ず終了します。

#### ★対象を取らないスペル（`DRAW_CARDS` など）の扱い

**カードデータ側の必須条件：対象を取らないスペルは `attackType` を `NONE` にすること。**

`spellEffect` の `switch` は `DIRECT` / `SPAWN` のとき `target.transform` を触るため、
対象なしでは必ず NRE になります。`NONE` だけが `UseSpellTo(target)` を呼ぶ形で、
`UseSpellTo` は対象が要る効果ごとに `if (target == null) return;` を持っています。

そのうえで `CastSpellOf` 側は次の3点を満たす必要があります。

**(1) `spellEffect` のガードに `NONE` を通す**

```csharp
if (target != null || card.model.attackType == ATTACKTYPE.NONE)
    card.spellEffect(target, true);
```

`target != null` だけで弾くと、`DRAW_CARDS` は以下の連鎖で壊れます。

1. マナだけ消費される
2. `UseSpellTo` に到達しないので**カードが破棄されない**
   （破棄しているのは `UseSpellTo` → `DrawCard` の `Destroy(card.gameObject)` と、
   `UseSpellTo` 末尾の `Destroy(this.gameObject)`）
3. `MoveLeftSpell` は `DOFade` で薄くするだけなので、カードは手札の子のまま残る
4. 手札が3枚未満にならず**ドローも止まる**、かつ毎ターン再選択される
5. さらに `spellEffect` 冒頭の `isAttacking = true` を戻すのも
   `DrawCard` 側なので、**`isAttacking` が true のまま固まる**

**(2) `MoveLeftSpell` を二重に呼ばない**

`DRAW_CARDS` の分岐は `yield break` せず下まで流れるため、
分岐内の `StartCoroutine(card.movement.MoveLeftSpell(card));` と、
その後の共通処理にある同じ呼び出しの**2回**実行されます。
分岐内の呼び出しを削除し、共通処理側の1回だけにしてください
（ヒーロー狙いのスペルは `yield break` するので二重にならず、この問題は起きません）。

**(3) `transform` ではなく `card.transform` を操作する**

`CastSpellOf` は `AI` クラスのメソッドなので、素の `transform` は
**カードではなく AI の GameObject** を指します。
`DRAW_CARDS` の分岐にある `transform.SetParent(transform.parent.parent);` は
AI 自身を親から外してしまうため、`card.transform` を対象にし、
`parent.parent` が null のときは何もしないようにしてください。

### 4-6. スペルの価値（`SpellValue`）とダメージ数字の表示

`SPELLS` は `[Flags]` なので、**該当する全フラグの価値を合計**します（`if / else if` にしない）。

| フラグ | Value | 0にする条件 |
|---|---|---|
| `DAMAGE_ENEMY_CARD` | 倒せる相手がいれば `Threat(その相手)`、いなければ `effectDmg * 0.5` | — |
| `DESTROY_ENEMY_CARD` | `Threat(最大脅威)` | — |
| `DAMAGE_ENEMY_CARDS` | 倒せる相手がいれば `Σ Threat(倒せる相手)`、いなければ `effectDmg * 0.5 * 相手の数` | — |
| `DESTROY_ALL_FIELD_CARDS` | `Σ Threat(相手) - Σ Threat(自陣)` | 上式が0以下（盤面有利なら撃たない） |
| `HEAL_FRIEND_CARD` | `effectHeal * 0.8` | — |
| `HEAL_FRIEND_CARDS` | `effectHeal * 0.8 * SelfField().Length` | — |
| `DAMAGE_ENEMY_HERO` | リーサル時 `10000`、それ以外 `effectDmg * (相手HP<=5 ? 2.0 : 0.8)` | — |
| `HEAL_FRIEND_HERO` | `effectDmg * (自HP<=4 ? 2.0 : 0.5)` | 自HP >= 8（満タン付近） |
| `STEAL_ENEMY_CARD` | `Threat(最大脅威) * 2` | — |
| `DRAW_CARDS` | `effectDmg * 2` | 自手札5枚以上 |
| `CONDITIONAL_ENEMY_DEBUFF` / `CONDITIONAL_FRIEND_BUFF` | `effectDmg * 1.0` | — |
| `SWAP_HP_ATK` | `at > hp` の相手の中で最大の `0.4 * (at - hp)` | 該当なし |
| `DISCARD_ENEMY_HAND` / `DISCARD_ALL_ENEMY_HAND` | `相手手札枚数 * 1.5` | — |
| `INCREASE_ENEMY_COST` | `相手手札枚数 * 1.0` | — |
| `REDUCE_HAND_COST` | `自手札枚数 * 0.8` | — |
| `DISCARD_FRIEND_HAND` / `DISCARD_ALL_FRIEND_HAND` | **常に0** | 常に |
| `SUMMON_SPECIFIC_UNIT` | `Σ Threat(targetCards)` | — |

**★役割分担**：「対象が存在するか」は `HasValidSpellTarget()` の責務なので、
`SpellValue` 側で再実装しないこと（「相手盤面が空」等は上表の0条件に出てきません）。
`SpellValue` は「対象はいるが撃つ価値があるか」だけを判定します。

**★`SpellValue` が知らないフラグのスペルを、永久に手札で腐らせないこと。**

`ChoosePlayPlan` の候補条件が `Value > 0` なので、
**上表のどのフラグにも当てはまらないスペルは `Value = 0` となり、二度と出せなくなります。**
出せないカードは手札を占有し続け、手札が3枚未満にならないため**ドローも止まります**。
（現状 `RANDOM_DAMAGE` と `HEAL_BY_DAMAGE` が上表に無く、これに該当します）

対策：`SpellValue` の中で「どれか1つでもフラグを認識したか」を記録し、
**1つも認識しなかった場合は最低限の価値（`1f`）を返す**ようにしてください。

```
recognized = false
（各フラグの分岐で recognized = true にする）
...
if (!recognized) return 1f;   // 未知のフラグ。小さい値だが候補には残す
return total;
```

**★`DISCARD_FRIEND_HAND` / `DISCARD_ALL_FRIEND_HAND` は「認識したうえで価値0」**に
してください（`recognized = true` にするが加算しない）。
これらは「撃つと損だから撃たない」が正しいので、上の救済措置の対象にしてはいけません。

**★0にしていいのは「撃つと損」または「完全に無駄」のときだけです。**
`ChoosePlayPlan` は `Value > 0` を候補条件にしているため、
**0にした瞬間そのカードは永久に出せなくなります**。
「効率が悪いから温存したい」程度の理由で0にしないでください。値を小さくすれば、
実際に温存するかどうかは他の選択肢とのスコア比較が決めてくれます。

過去に `DESTROY_ENEMY_CARD` へ「最大 `Threat` < 6 なら0（雑魚に温存）」を入れた結果、
`at0/hp5`（`Threat` = 4.0）のような普通のカードしか場にいない場面で
**除去スペルが一切撃たれなくなりました。** 現在この閾値は撤廃済みです。

0にしてよい例：`DESTROY_ALL_FIELD_CARDS` で盤面有利（自陣を巻き込んで損）、
`DISCARD_FRIEND_HAND`（自分の首を絞めるだけ）、`DRAW_CARDS` で手札が溢れる、
`HEAL_FRIEND_HERO` で自HPが満タン付近。

**★回復は「HPを恒久的に上げるバフ」です。**
`RecoveryHP()` は `hp += point` で上限がありません（`maxHp` は AI の参照値であって回復の上限ではない）。
**全快の味方に撃っても無駄にならない**ので、傷の深さで価値を測らないでください。

**★`Value(スペル)` はモンスターの `Threat`（20〜50程度）と同じ土俵の大きさにすること。**
段階5の暫定実装（`cost * 1.0`、最大10）では、スペルが常にモンスターに負けて選ばれませんでした。

#### ダメージ数字を出すのは「ダメージ」だけ

`CardController.ShowsDamageNumber()` が `spells` と `abilities` の両方を見て判定します。
**以下を持つカードは数字を出しません**（`effectDmg` がダメージ以外の意味だから、
または破壊のように数値が存在しないから）。

```
DESTROY_ENEMY_CARD / DISCARD_ENEMY_HAND / DISCARD_ALL_ENEMY_HAND
DISCARD_FRIEND_HAND / DISCARD_ALL_FRIEND_HAND
INCREASE_ENEMY_COST / REDUCE_HAND_COST
```

呼び出しは5箇所（`SpawnEffectAbility` / `DirectAttackAbility` / `LerpThrowAbility` /
`DirectSpellAttack` / `SpawnSpellEffect`）で、条件は
`if (model.effectDmg != 0 && ShowsDamageNumber())` に統一されています。
**条件式を5箇所にコピペで散らさないでください**（過去にスペル側だけ直してアビリティ側が漏れました）。

### 4-7. `maxHp`（AIの評価用の参照値）

設定箇所は4つです。**1つでも漏れると回復系の評価が壊れます。**

1. `CardModel` コンストラクタ … `maxHp = cardEntity.hp;`
2. `targetCards[i]` の初期化ループ … `targetCards[i].maxHp = targetEntity.hp;`
3. `CardController.Init()` の `ApplyRoguelike()` の**後** … `model.maxHp = model.hp;`
4. `RecoveryHP()` … `if (hp > maxHp) maxHp = hp;`（追従のみ）

**★回復に上限（`hp` を `maxHp` で clamp）は絶対に入れないでください。**
プレイヤー側のゲームバランスが変わります。

### 4-8. 既知の割り切り（不具合ではないので直さないこと）

- **`at == 0` のカードはリーサルターンでも相手カードを攻撃します。**
  `ScoreAgainstHero` が `0 * 1000 = 0` になるためですが、打点0なので勝敗に影響しません。
- **`DESTROY_ENEMY_CARD` が `DAMAGE_NULLIFY_ONCE` に吸収されるのは仕様です。**
  「無効化は1回の攻撃イベントとして丸ごと防ぐ」というユーザー決定に基づきます。
- **ターン中にドローしたカードは、そのターンには出せません。**
  `playPlan` がターン開始時のスナップショットから作られるためです。
- **リーサル計算の手札バーン合計は貪欲法**なので、
  取得順によっては最適な組み合わせを見逃すことがあります（安全側の誤りです）。
- **`CardController.cs` の `Random.Range(0, len - 1)`（`ActAbility` 以外）は未修正です。**
  プレイヤー側の処理なので、段階8で報告のみ行います。
- **`SUMMON_SPECIFIC_UNIT` は `SpellValue` で加点されますが、`UseSpellTo` に
  スペル版の実装がありません**（実装があるのは `UseAbilitiesTo` 側だけ）。
  **スペルとして `SUMMON_SPECIFIC_UNIT` を持つカードは作らない方針**なので、
  現状のままとします（ユーザー決定）。もしスペル版を作る場合は、
  `UseSpellTo` に分岐を足すまで `SpellValue` から外してください。
- **`DRAW_CARDS` スペルは作る方針です**（ユーザー決定）。
  カード側は `attackType = NONE` にすること。4-5 の
  「★対象を取らないスペルの扱い」の3点が前提になります。
- **`CONDITIONAL_ENEMY_DEBUFF` / `CONDITIONAL_FRIEND_BUFF` は
  `ShowsDamageNumber()` の除外リストに入れません。**
  `effectDmg` は ATK の増減量ですが、**数字が出てよい**というユーザー判断です。
- **`Assets/Scenes/Field.unity` の `debugMode` は現在 `1` のままです。**
  検証中のため意図的に残しています。リリース前に `0` に戻してください。

---

## 5. これから実装する段階

### 段階8：クリーンアップ（最後）

- `AI.GetFirstZeroOrLess` が本当に未使用なら削除（**先に全プロジェクトを grep して確認**）
- `AI.cs` 冒頭の未使用 `using`（`static UnityEngine.Rendering.GPUSort` 等）の整理
- `CardController.cs` 側の `Random.Range(0, len - 1)` について、**修正はせず**該当行を一覧にして報告

---

## 6. 完了時の報告フォーマット

**あなたはUnityを実行できません。** 各段階の完了時に以下の形式で報告してください。

```
## 段階N 完了

### 変更ファイル
- xxx.cs (+12/-5)

### 実装内容
（何をどう変えたか）

### コンパイルエラーの可能性がある箇所
（型が不確かなAPIを使った箇所。無ければ「なし」）

### Unityで確認してほしいこと
1. コンパイルエラーが出ないこと
2. （具体的な確認シナリオ）
3. 演出のタイミングが従来と変わっていないこと

### 想定される副作用
（あれば）
```

あわせて以下も報告してください。

- `git diff --stat`
- `WaitForSeconds` の行が変更されていないことの確認結果
  （`git diff | grep -E "^[+-].*[Ww]ait[Ff]or[Ss]econds"` が空であること）
- 触ってはいけないファイルが変更されていないことの確認結果

---

## 7. コーディング規約

- コメントは日本語（既存コードに合わせる）
- 既存のインデント・命名（camelCase フィールド、PascalCase メソッド）に合わせる
- 既存のコメントアウトされたコードブロックは**削除しない**（作者が意図的に残している）
- `Debug.Log` は追加しない（デバッグで追加した場合は完了時に必ず削除する）
- 同じ条件式を複数箇所にコピペしない。判定はヘルパーに集約する

---

## 8. やってはいけないこと（再掲・最重要）

1. `WaitForSeconds` の値・位置・個数を変えない
2. プレイヤー側の処理を変えない（2章のリスト）
3. 複数の段階をまとめて実装しない
4. `.asset` / `.prefab` / `.unity` / `.meta` を作成・編集しない
5. 実コードを読まずに、この指示書の記述だけを根拠に実装しない
6. 回復処理に `maxHp` の上限を導入しない
7. 4章「確定した仕様」のルールを、明示的な指示なしに変更しない
