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
5. **`CanUseSpells()` / `CanUseAbilities()` の既存の判定内容**
   プレイヤー側も使っています。AI用の価値判定は**上乗せ**する形にしてください。
6. **既存 MonoBehaviour の `[SerializeField]` の削除・リネーム**
   Unityの .meta / シーンの参照が切れます。追加は可、削除・改名は不可。

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

Score(attacker a, defender d):
    kill    = a.at >= d.hp   （a が DESTROY_ATTACKED_TARGET を持つなら常に true）
    survive = d.at <  a.hp   （a が DAMAGE_NULLIFY_ONCE   を持つなら常に true）

    kill &&  survive →  100 + Threat(d)
    kill && !survive →   50 + Threat(d) - Threat(a)
   !kill &&  survive →   10 + a.at
   !kill && !survive →  -50

Score(attacker a, ヒーロー):
    a.at * faceWeight
    faceWeight = 1.0 が基準。以下で加算：
        相手ヒーローHP <= 5              → +1.5
        BoardAdvantage() > 0（盤面有利）  → +0.8
```

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

ターン開始直後（`AI.cs` の最初の `WaitForSeconds(1)` の後、召喚ループの前）に一度だけ計算します。

```
到達可能打点 =
    (プレイヤー盤面に SHIELD 持ちがいない場合)
        Σ 自陣の canAttack カードの at
    (SHIELD 持ちがいる場合)
        Σ 自陣の canAttack かつ PIERCE 持ちカードの at
  + Σ (手札にある SPELLS.DAMAGE_ENEMY_HERO 持ちのうち、マナ内で撃てるものの effectDmg)

if (到達可能打点 >= gameManager.player.heroHp)
    → isLethalTurn = true
```

`isLethalTurn` が true のとき：
- 召喚フェーズでは **バーンスペル（`DAMAGE_ENEMY_HERO`）を最優先で全て使う**
- 攻撃フェーズでは `faceWeight` を極端に大きく（例: 1000f）して**必ずヒーローを殴る**

**注意**：`DOUBLE_ACTION` 持ちは2回攻撃できますが、初回は数えない安全側の見積もりにしてください
（過大評価してリーサルを外すより、見逃すほうが安全）。

マナの計算は `gameManager.enemy.manaCost` を使います（`defaultManaCost` ではない）。
バーンスペルを複数使う場合、合計コストがマナを超えないよう貪欲に選んでください。

#### 完了条件
- プレイヤーHPが残り少ないとき、敵AIが確実に詰めてくること
- リーサルが成立しないとき、従来通りの評価（段階2）で動くこと

---

### 段階4：スペル/アビリティの対象選択（`[0]` の全廃）

`AI.cs` の以下をすべて置き換えます。

| 場所 | 現在 | 置き換え後 |
|---|---|---|
| `AI.cs:175` | `GetEnemyFieldCards(...)[0]` | `AIEvaluator.SelectDamageTarget(card)` |
| `AI.cs:178` | `GetFriendFieldCards(...)[0]` | `AIEvaluator.SelectHealTarget(card)` |
| `AI.cs:284` | `GetEnemyFieldCards(...)[0]` | `AIEvaluator.SelectDamageTarget(card)` |
| `AI.cs:289` | `GetFriendFieldCards(...)[0]` | `AIEvaluator.SelectBuffTarget(card)` |

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

#### 完了条件
- 敵AIが除去スペルを最大の脅威に撃つこと
- 盤面が空のときに例外が出ないこと

---

### 段階5：出すカードの選択（マナ最大化）

`AI.cs:49-52` の `Array.Find`（先頭から貪欲）を置き換えます。

```csharp
// AIEvaluator
public static List<CardController> ChoosePlayPlan(CardController[] hand, int mana, int freeSlots);
```

**アルゴリズム：部分集合の全探索**（手札は最大7枚程度なので 2^7 = 128 通り。計算量の問題なし）

```
制約:
    Σ cost <= mana
    モンスターの枚数 <= freeSlots  （freeSlots = 5 - 自陣のカード数）
    スペルは CanUseSpells() が true のもののみ候補に入れる
最大化:
    Σ Value(card)

Value(モンスター) = Threat(card) + (INIT_ATTACKABLE ? card.at : 0)
Value(スペル)     = 段階6まで暫定で card.model.cost * 1.0f
                    （= 高コストスペルを優先。段階6で本実装に差し替える）
```

**手札が8枚以上の場合は、コスト効率（`Value / max(cost,1)`）上位8枚に絞ってから全探索**して、
`2^n` が爆発しないようにガードを入れてください。

返したリストは**この順で実行**します：
1. 除去スペル（`DAMAGE_ENEMY_CARD` / `DAMAGE_ENEMY_CARDS` / `DESTROY_ENEMY_CARD`）
2. モンスター
3. その他のスペル（バフ / ドロー / ヒーロー系）

**`AI.cs` の召喚ループの構造（`while` + 待機ループ + `WaitForSeconds`）は維持し、
「次に出す1枚を返す」形で `ChoosePlayPlan` の結果を先頭から消費してください。**
毎ループで再計算すると盤面変化で不整合が起きるため、
**ターン開始時に1回だけ計算し、リストを消費する形**にします。
ただし、消費前に「まだマナが足りるか」「まだ盤面に空きがあるか」は毎回チェックしてください
（スペルの効果で盤面が変わるため）。

#### 完了条件
- マナ5・手札[3,3,5] のとき、3ではなく5を出すこと
- 盤面が5体埋まっているとき、モンスターを出そうとしないこと（スペルは使う）
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

`Value(スペル)` を以下に差し替えます。`CanUseSpells()` が false のものは候補に入れません（既存判定は維持）。

| フラグ | Value | 0にする条件（＝使わない） |
|---|---|---|
| `DAMAGE_ENEMY_CARD` | 倒せる相手がいれば `Threat(その相手)`、いなければ `effectDmg * 0.5f` | 相手盤面が空 |
| `DESTROY_ENEMY_CARD` | `Threat(最大脅威)` | 最大 `Threat` < 6（雑魚に温存） |
| `DAMAGE_ENEMY_CARDS` | `Σ Threat(effectDmgで倒せる相手)` | 倒せるのが1体以下 |
| `DESTROY_ALL_FIELD_CARDS` | `Σ Threat(相手盤面) - Σ Threat(自盤面)` | **上式が 0 以下（＝盤面有利なら撃たない）** |
| `HEAL_FRIEND_CARD` / `HEAL_FRIEND_CARDS` | `Σ min(effectHeal, maxHp - hp)` | 上式が2未満（傷が浅い） |
| `DAMAGE_ENEMY_HERO` | リーサル時は 9999。それ以外は `effectDmg * (プレイヤーHP <= 5 ? 2.0f : 0.8f)` | なし |
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

#### 完了条件
- 全快の味方に回復スペルを撃たなくなること
- 盤面有利なときに `DESTROY_ALL_FIELD_CARDS` を撃たなくなること
- 相手手札0枚のときにハンデスを撃たなくなること

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