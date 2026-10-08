# 設定画面 実装指示書

対象: `Assets/Scenes/Field.unity`（メインメニュー） / `Assets/Scripts/Common/SaveData.cs` / 新規 `Assets/Scripts/OutGame/Settings/`
目的: メインメニューから設定画面へ遷移し、**BGM音量 / SE音量 / フルスクリーン / 解像度**を変更・保存できるようにする

**「2. 確定した仕様」が実装の正**です。「1. 現状調査の結果」は 2026-09-18 時点での実コード確認結果であり、
食い違いがあれば**実コードを正**として、指示書のどこが違ったかを報告してください。

**行番号は信用しないでください。** 「どのクラスの、どのメソッドか」で場所を特定してください。

---

## 0. 最初にやること

実装前に、以下の実ファイルを読んでください。要約や grep だけで進めないでください。

```
Assets/Scripts/OutGame/ModeSelection/ModeSelectionUI.cs        ← メインメニューのパネル切替
Assets/Scripts/OutGame/Gacha/GachaManager.cs                   ← 今回まねる遷移パターン（OpenGacha/CloseGacha）
Assets/Scripts/OutGame/ModeSelection/ModeButtonPressEffect.cs  ← ボタン押下演出
Assets/Scripts/OutGame/ModeSelection/PanelEntranceAnimator.cs  ← パネル登場演出
Assets/Scripts/Common/SaveData.cs                              ← GameSettings の定義
Assets/Scripts/Common/GameDataHolder.cs                        ← セーブデータのメモリキャッシュ
Assets/Scripts/Common/SaveManager.cs                           ← ファイルI/O
Assets/Scripts/GlobalAudioManager.cs                           ← ★未使用（後述）
Assets/Scripts/InGame/BattleAudioManager.cs
```

---

## 1. 現状調査の結果

### 1-1. メインメニューの実体

「メインメニュー画面」= `Field.unity` の **内側の `ModeSelectionPanel`** です。
同名のオブジェクトが入れ子になっているので注意してください。

```
ShakeObj
└ ModeSelectionPanel          ← 外側。ここに ModeSelectionUI がアタッチされている
  ├ ModeSelectionPanel        ← 内側。これが「メインメニュー画面」本体
  │ ├ TwoPickButton           ← ModeButtonPressEffect 付き
  │ ├ LeathalPuzzleButton     ← ModeButtonPressEffect 付き
  │ ├ RougeliteButton         ← ModeButtonPressEffect 付き
  │ ├ CpuButtleButton         ← ModeButtonPressEffect 付き
  │ ├ GachaButton             ← ModeButtonPressEffect 付き
  │ ├ Bg_Up
  │ ├ Btn_Back                ← Image のみ。ボタンではなく装飾
  │ ├ Btn_Back (2)            ← Image のみ。ボタンではなく装飾
  │ ├ GoldBar
  │ └ Title
  ├ TwoPickPanel
  ├ LethalPuzzleBG
  ├ CpuBattlePanel
  ├ RoguelikeSceneController
  └ GachaPanel
```

- 内側の `ModeSelectionPanel` には `PanelEntranceAnimator` が付いている。
  `slideTargets` が空なので**直下の子要素を自動収集する**設定。
  → **設定ボタンを子として追加すると、自動的に登場演出の対象に入る。**
- `Btn_Back` / `Btn_Back (2)` は名前に反して `Button` を持たない単なる `Image`（Cyberpunk パックの「ボタン背景」素材）。
  設定ボタンとして流用しないこと。

### 1-2. 既存の画面遷移パターンは2種類ある

| 方式 | 実装 | パネルのアクティブ状態 |
|---|---|---|
| サブパネル方式 | `ModeSelectionUI.PlayPressEffectThen()` → `ShowXxxPanel()` で `SetActive` 切替 | 切り替わる（`OnEnable` が走る） |
| ガチャ方式 | `GachaManager.OpenGacha()` / `CloseGacha()` | **常時アクティブのまま**、`CanvasGroup.alpha` と中身の `SetActive` で見せ消しする |

**今回は「ガチャ方式」を採用します**（後述 2-2）。`GachaManager` の以下の作法をそのまま踏襲してください。

```csharp
// 開く
gachaButtonEffect.Play(ShowGachaUI);   // 押下演出 → 完了後にパネル表示

private void ShowGachaUI()
{
    gachaUI.Open(...);
    bg.SetActive(true);
    header.SetActive(true);
    gachaEntrance.Play();              // 常時アクティブで OnEnable が走らないので明示再生
}

// 閉じる（戻るボタンの ModeButtonPressEffect 完了後に呼ばれる）
public void CloseGacha()
{
    gachaUI.Hide();
    bg.SetActive(false);
    header.SetActive(false);
    returnButtonEffect.ResetState();   // フェードアウトした alpha を戻す
    gachaButtonEffect.ResetState();    // メニュー側ボタンの拡大・フェードを戻す
    modeSelectionEntrance.Play();      // メニューも SetActive を経由しないので明示再生
}
```

### 1-3. セーブデータ側は「器だけある」状態

`SaveData.GameSettings` は定義済みだが、**プロジェクト全体で一度も参照されていない**。

```csharp
[System.Serializable]
public class GameSettings
{
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
}
```

- `GameDataHolder.Settings`（`=> Data.settings`）も呼び出し元 0 件。
- したがって「読み込み・保存の配線」から新規に作る必要がある。
- `GameDataHolder` は `Field.unity` に 1 個配置済み。`Game.unity` には無い（`Instance` の Lazy 初期化で自動生成される）。

### 1-4. ★最大の障壁：音量を集中制御する仕組みが無い

| クラス | 配置シーン | 呼び出し元 |
|---|---|---|
| `GlobalAudioManager` | **どのシーンにも無い** | **0件（完全な死にコード）** |
| `BattleAudioManager` | `Game.unity` のみ | `GameManager` / `CardMovement` / `CardController` / `CardClickManager` |
| `CardController.audioSource` | カード個別 | `audioSource.PlayOneShot(model.hitAudio)`（約15箇所） |

- `Field.unity` には **AudioSource が 254個 散在**しており、中央管理クラスが存在しない。
- **`AudioMixer` アセットはプロジェクトに1つも無い**（`find Assets -name "*.mixer"` → 0件）。
- つまり現状、`bgmVolume` / `sfxVolume` を代入する先が存在しない。

→ **音量の反映方式を先に決める必要がある。** 2-4 を参照。

### 1-5. その他、周辺の把握しておくべき事実

- シーンは `Field.unity` と `Game.unity` の 2 つだけ（Build Settings も同様）。
- `ModeSelectionUI.OnClickBackToTitle()` は `titleScene = "Title"` を読むが、**"Title" シーンは存在しない**。
  今回の作業対象外だが、触る場合は別タスクとして報告すること。
- `GachaManager.GetUnlockedPacks()` は `GameDataHolder` を経由せず `SaveManager.Load()` を直接呼んでいる。
  **設定はこの真似をしないこと**（後述 3-5 の注意）。
- Unity `6000.0.44f1`。`ProjectSettings.asset`: `defaultScreenWidth: 1920` / `defaultScreenHeight: 1080` /
  `allowFullscreenSwitch: 1` / `resizableWindow: 0`。
- CanvasScaler の `m_ReferenceResolution` は `800x600`。
- UI素材: `Assets/ThirdParty/UI/Cyberpunk RPG GUI Pack/Cyberpunk RPG GUI Resources/Prefabs/08Settings Canvas.prefab`
  に既製の設定画面がある（`Volume` / `Effect` スライダー、`Vibration` / `Language` / `Aspect Ratio`、`Btn_Close`）。
  メニューの `Bg_Up` / `Btn_Back` と同じパックなので**見た目はここから流用するのが最短**。
  ただし**丸ごと配置せず、必要なスライダー・トグル・閉じるボタンだけを取り出すこと**（Canvas が二重になるため）。

---

## 2. 確定した仕様

### 2-1. 設定項目

| 項目 | 型 | UI | 反映先 |
|---|---|---|---|
| BGM音量 | `float` 0〜1 | Slider | AudioMixer の `BgmVolume`（2-4） |
| SE音量 | `float` 0〜1 | Slider | AudioMixer の `SeVolume`（2-4） |
| フルスクリーン | `bool` | Toggle | `Screen.fullScreen` |
| 解像度 | `int`×2 | 左右送りボタン or Dropdown | `Screen.SetResolution` |

**今回は対象外**（将来足す余地だけ残す）: 言語、バイブレーション、演出速度、セーブデータ削除。

### 2-2. 遷移方式

**同シーン内のパネル切替（ガチャ方式）**。`Settings.unity` は作らない。

```
メインメニュー(内側 ModeSelectionPanel)
  └ SettingsButton を新規追加（ModeButtonPressEffect 付き）
       ↓ Play() で押下演出 → 完了後
     SettingsPanel（GachaPanel と同階層、外側 ModeSelectionPanel の子）を表示
       └ Btn_Close の ModeButtonPressEffect → 完了後 SettingsUI.Close()
```

### 2-3. スコープ

**`Field.unity` のメインメニューからのみ開ける。** `Game.unity`（バトル中）からは開けない。
ただし**反映ロジックは `SettingsApplier` に分離**し、後からバトル中のポーズ画面に載せられる形にすること。

### 2-4. 音量の反映方式（AudioMixer を新規作成する）

`AudioListener.volume` はマスターしか触れず BGM/SE を分離できないため、**AudioMixer を導入します。**

```
Assets/Audio/MainMixer.mixer（新規作成）
├ Master
├ BGM   … Volume を "BgmVolume" として Expose
└ SE    … Volume を "SeVolume"  として Expose
```

代入は dB なので、リニア値から変換すること。**0 を log に渡すと -∞ になるので必ずクランプする。**

```csharp
private const float MIN_VOLUME = 0.0001f;

private static float ToDecibel(float linear)
    => Mathf.Log10(Mathf.Max(linear, MIN_VOLUME)) * 20f;

mixer.SetFloat("BgmVolume", ToDecibel(settings.bgmVolume));
```

**出力先グループの割り当て対象**（`AudioSource.outputAudioMixerGroup`）:

| 対象 | グループ |
|---|---|
| `BattleAudioManager.bgmSource` | BGM |
| `BattleAudioManager.seSource` | SE |
| `GlobalAudioManager.bgmSource` / `seSource` | BGM / SE（※1-4 の通り現状シーンに未配置。配置するかは 3-1 で判断） |
| カードPrefabの `CardController.audioSource` | SE |

`Field.unity` の残り約250個の AudioSource は、**実際に鳴っているものだけを段階的に割り当てれば十分**です。
一括で触ろうとしないでください（シーンファイルの巨大な差分になります）。

---

## 3. 実装手順

段階ごとに区切り、**各段階の終わりに一度 Unity で再生確認してから次へ進むこと。**

### 段階1: 音の出口を確定させる（★ここが一番の判断ポイント）

1. `Assets/Audio/MainMixer.mixer` を作成し、`BGM` / `SE` グループと Expose パラメータを用意する。
2. `GlobalAudioManager` を**使うのか捨てるのか決める。**
   - 現状どのシーンにも無く、呼び出し元も 0 件。
   - **アウトゲーム（Field）に BGM を鳴らす予定があるなら**、`Field.unity` に配置して生かす。
   - **予定が無いなら**、音量スライダーは当面 `BattleAudioManager` とカードSEにしか効かない。
     その場合は「設定画面で音量を変えても Field では変化が見えない」ことを許容するか、
     先にアウトゲームBGMを入れるかを**実装前に確認すること**。
3. `BattleAudioManager` の 2 つの AudioSource に出力グループを割り当てる。

### 段階2: データ層

`Assets/Scripts/Common/SaveData.cs` の `GameSettings` を拡張する。

```csharp
[System.Serializable]
public class GameSettings
{
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;

    public bool isFullScreen = true;
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
}
```

`JsonUtility.FromJson` は JSON に存在しないフィールドをフィールド初期化子の値のまま残すため、
**既存の save.json はそのまま読めます**（マイグレーション不要）。

### 段階3: 反映クラス `SettingsApplier`

`Assets/Scripts/Common/SettingsApplier.cs`（static クラス）を新規作成する。
**UI に一切依存させないこと。** バトル中のポーズ画面から再利用できるようにするためです。

```csharp
public static class SettingsApplier
{
    public static void ApplyAll(GameSettings settings);
    public static void ApplyBgmVolume(float volume);
    public static void ApplySfxVolume(float volume);
    public static void ApplyScreen(bool isFullScreen, int width, int height);
}
```

- AudioMixer は `Resources.Load<AudioMixer>()` か、`GlobalAudioManager` に `[SerializeField]` で持たせて参照する。
  static クラスから触るなら `Resources` 配下に置くのが確実（`Assets/Resources/` は既に存在する）。
- **起動時に一度 `ApplyAll` を呼ぶこと。** 呼ばないと保存した設定が反映されないまま始まります。
  呼ぶ場所は `GameDataHolder.Initialize()` の `LoadIntoMemory()` 直後が妥当。

### 段階4: UI

1. `Field.unity` の外側 `ModeSelectionPanel` の子として `SettingsPanel` を作る（`GachaPanel` と同階層）。
2. `08Settings Canvas.prefab` から Slider / Toggle / `Btn_Close` の見た目を流用する（Canvas ごとは持ってこない）。
3. `SettingsPanel` に `PanelEntranceAnimator` を付ける。
4. `Assets/Scripts/OutGame/Settings/SettingsUI.cs` を新規作成する。

```csharp
public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;   // または左右送りボタン

    [SerializeField] private GameObject settingsPanelObj;
    [SerializeField] private PanelEntranceAnimator settingsEntrance;

    // メニュー側の設定ボタンの押下演出。未設定なら演出を挟まず即開く
    [SerializeField] private ModeButtonPressEffect settingsButtonEffect;
    // 設定画面の閉じるボタンの押下演出。SettingsPanel のフェードアウトを担当
    [SerializeField] private ModeButtonPressEffect closeButtonEffect;
    // メニューの登場演出。閉じた時に明示再生する
    [SerializeField] private PanelEntranceAnimator modeSelectionEntrance;

    public void Open();    // 設定ボタンの OnClick に登録
    public void Close();   // closeButtonEffect の onEffectComplete に登録
}
```

- **`Open()` では、UI の値を `GameDataHolder.Instance.Settings` から流し込む。**
  この時のスライダー代入で `onValueChanged` が発火して無駄な `Apply` が走らないよう、
  `SetValueWithoutNotify()` を使うか、流し込み中フラグでガードすること。
- 値変更時は**即座に `SettingsApplier` へ反映**する（音量は耳で確認できないと調整できないため）。
- **ファイルへの保存は `Close()` の1回だけ**（理由は 3-5）。

### 段階5: メニューからの遷移を配線する

1. 内側 `ModeSelectionPanel` の子として `SettingsButton` を追加（`Button` + `ModeButtonPressEffect`）。
2. `ModeButtonPressEffect.fadeOutPanel` に**内側 `ModeSelectionPanel` を明示指定**する（4-1 参照）。
3. `SettingsButton.onClick` → `SettingsUI.Open`。
4. `Btn_Close.onClick` → `closeButtonEffect.PlayAndInvoke`、その `onEffectComplete` → `SettingsUI.Close`。
   （`GachaManager` の戻るボタンと同じ配線）

---

## 4. 絶対に間違えるポイント

### 4-1. `ModeButtonPressEffect.fadeOutPanel` は必ず明示指定する

未指定だと**親オブジェクトが対象**になります（`Initialize()` を参照）。
`SettingsButton` が `Header` などの中間オブジェクトの下に置かれると、
パネル本体ではなく中間オブジェクトだけがフェードして、見た目が破綻します。

また `ModeButtonPressEffect` に **`IDragHandler` を実装してはいけません**。
実装すると入力モジュールがドラッグ開始時点で `OnPointerUp` を発火させ、押下判定と演出の両方が壊れます
（クラスのコメントに明記されています）。

### 4-2. 常時アクティブなパネルは `OnEnable` が走らない

`PanelEntranceAnimator` は `OnEnable()` で自動再生しますが、
ガチャ方式では `SettingsPanel` を `SetActive` で切り替えないため **`OnEnable` が来ません**。
`Play()` を明示的に呼ぶこと。

閉じる時も同様に、`ModeButtonPressEffect.ResetState()` で
「拡大したままのボタン」「alpha=0 のままのパネル」を元に戻す必要があります。
これを忘れると**2回目に開いた時に画面が真っ白／透明のまま**になります。

### 4-3. 設定の読み書きは必ず `GameDataHolder` 経由にする

`GachaManager.GetUnlockedPacks()` は `SaveManager.Load()` をファイルから直接呼んでいますが、
**設定でこれを真似しないでください。**
メモリ上の `GameDataHolder.Instance.Settings` とファイルの内容がズレて、
「スライダーを動かしたのに反映されない／閉じたら戻る」という不整合になります。

```csharp
// 正
GameSettings settings = GameDataHolder.Instance.Settings;
settings.bgmVolume = value;

// 誤
SaveData data = SaveManager.Load();   // ← ファイルから読み直してはいけない
```

### 4-4. スライダー操作のたびにファイル保存しない

`SaveManager.Save()` は**毎回 save.json のバックアップコピーを取ってから書き込みます**。
スライダーのドラッグ中に毎フレーム呼ぶと重く、セーブ破損のリスクもあります。

- 値変更時 → `GameDataHolder.Instance.Settings` の書き換え + `SettingsApplier` の反映のみ
- `Close()` 時 → `GameDataHolder.Instance.SaveToFile()` を1回

### 4-5. 解像度変更はエディタ上では効かない

`Screen.SetResolution()` はエディタの Game ビューには反映されません。
**エディタで動かないことを不具合と判断しないでください。** ビルドして確認すること。

解像度の候補は `Screen.resolutions` から取得しますが、同一解像度がリフレッシュレート違いで重複します。
`width`/`height` で重複排除し、必要なら 16:9 のみに絞ること。

### 4-6. 設定ボタンを追加すると登場演出の対象が増える

内側 `ModeSelectionPanel` の `PanelEntranceAnimator` は `slideTargets` が空 = **直下の子を自動収集**します。
`SettingsButton` を追加すると自動的にスライドイン対象に入りますが、
**Hierarchy 上の並び順で `stagger`（0.05秒ずつ）の順番が決まります。**
意図した順番にならない場合は Hierarchy の並びを直すか、`slideTargets` を明示指定してください。

---

## 5. 完了条件

- [ ] メインメニューに設定ボタンがあり、押すと他のモードボタンと同じ押下演出の後に設定画面が開く
- [ ] 設定画面が `PanelEntranceAnimator` でフェードイン＋スライドインする
- [ ] BGM / SE スライダーを動かすと**その場で**音量が変わる
- [ ] フルスクリーン切替・解像度変更がビルドで機能する
- [ ] 閉じるボタンで押下演出の後にメニューへ戻り、**メニューの登場演出が再生される**
- [ ] **2回開閉しても表示が壊れない**（4-2 の `ResetState` 忘れの検証）
- [ ] ゲームを再起動しても設定が保持されている
- [ ] 既存の save.json（設定フィールド追加前のもの）を読んでもエラーにならない

---

## 6. 実装前に確認が必要な事項

1. **アウトゲーム（Field シーン）に BGM を鳴らす予定はあるか？**（1-4 / 段階1-2）
   無いなら、BGM スライダーは Field 上では効果が確認できません。
2. **解像度の選択肢は `Screen.resolutions` の実機依存リストか、固定リスト（1920x1080 / 1600x900 / 1280x720）か？**
3. 設定画面を将来バトル中のポーズ画面からも開く予定があるか（あるなら `SettingsUI` をプレハブ化しておく）。
