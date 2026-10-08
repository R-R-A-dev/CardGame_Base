# CardGame

Unity製の2Dデジタルカードゲームです。CPU対戦やローグライクなど、複数のゲームモードを搭載しています。

## ゲームモード

- **CPU対戦** — 編成したデッキでCPUと対戦
- **2Pick** — 提示されたカードから選んでデッキを組み、対戦
- **リーサルパズル** — 決められた盤面から1ターンでの勝利を目指す詰めパズル
- **ローグライク** — マップを進みながら戦闘・報酬・回復マスを経てデッキを強化（Slay the Spire風）
- **ガチャ** — カードパックを開封してカードを入手

## 動作環境

- Unity **6000.0.44f1**
- Universal Render Pipeline (URP)

## セットアップ

1. リポジトリをクローン
   ```sh
   git clone https://github.com/R-R-A-dev/CardGame_Base.git
   ```
2. Unity Hub からプロジェクトを開く（Unity 6000.0.44f1）
3. `Assets/Scenes/Field.unity` を開いて再生

## シーン構成

| シーン | 内容 |
| --- | --- |
| `Field.unity` | アウトゲーム（モード選択・デッキ編成・ガチャなど） |
| `Game.unity` | インゲーム（カードバトル） |

## ディレクトリ構成

```
Assets/
├── Scenes/            シーン
├── Scripts/
│   ├── Common/        セーブ/ロード、シーン遷移などの共通処理
│   ├── InGame/        バトル処理（カード、手札、AI など）
│   └── OutGame/       モード選択、デッキ編成、ガチャ、ローグライクなど
├── Resources/         実行時ロードするデータ
└── AddressableAssetsData/
```

## 主な使用パッケージ・アセット

- Addressables
- Input System
- DOTween
- TextMesh Pro
- UI Particle (Coffee.UIParticle)
