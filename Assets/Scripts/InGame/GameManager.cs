using Coffee.UIExtensions;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using DG.Tweening;
using static Unity.Burst.Intrinsics.X86.Avx;
using UnityEngine.AddressableAssets; using UnityEngine.ResourceManagement.AsyncOperations;

public class GameManager : MonoBehaviour
{
    public GamePlayerManager player;
    public GamePlayerManager enemy;

    [SerializeField] AI enemyAI;
    [SerializeField] public UIManager uiManager;

    public Transform playerHandTransform,
                               playerFieldTransform,
                               enemyHandTransform,
                               enemyFieldTransform;

    [SerializeField] CardController cardPrefab;

    public bool isEffectSelectPhase;
    public bool isPlayerTurn;
    public Transform playerHero;
    public Transform enemyHero;

    [SerializeField] Button TurnEndButton;
    [SerializeField] Text TurnEndButtonText;

    public RectTransform summonLight;
    [SerializeField] UIParticle UIParticleObj;
    public ParticleSystem summonTrail;
    public ParticleSystem summonEffect;
    public UIParticlesManager uiParticlesManager;
    public CameraShake cameraShake;
    [SerializeField] Image selectingPanel;

    [SerializeField] GameObject playerTurn;
    [SerializeField] GameObject enemyTurn;
    [SerializeField] GameObject winEffect;
    [SerializeField] GameObject loseEffect;
    [SerializeField] GameObject effectBack;


    [SerializeField] Transform playerDeck;
    [SerializeField] Transform enemyDeck;


    public bool isSummoning = false;
    public bool isAttacking;
    // カード同士の戦闘（CardsBattle）の最中か。
    // isAttackingは反撃がヒットした時点でfalseに戻るが、その後も倒れたカードを消す
    // （CheckAlive）までの待ちがあり、その間に次の攻撃を許すと消える直前のカードを狙えてしまうため、
    // 戦闘が終わるまではこちらで塞ぐ。
    public bool isCardsBattling;
    // プレイヤーが使用中のスペルカード。
    // スペルは移動演出(isSummoning)が終わってから効果(isAttacking)が始まるまでの間や、
    // 複数対象の効果で最初の1発がヒットした後など、どちらのフラグも立っていない時間がある。
    // スペルは効果の最後に自分自身をDestroyするので、破棄されればUnityのnull判定でnullになり自動で解除される。
    public CardController castingSpell;
    public bool isOnCard;
    public bool showDescriptionClicked;

    // 時間管理
    public int timeCount;

    // カウントダウンは「前のターンの分だけ」を止めたいので個別に保持する。
    // StopAllCoroutines()で止めると実行中のダメージ数値表示(GenFloatingText)や
    // 結果表示まで巻き込み、数字が消えないままテキストプールが枯れてしまう。
    // この2つはtimeCountを共有しているため、同時に動くと時間が倍速で減る。
    Coroutine countDownCoroutine;
    Coroutine changeCardCountDownCoroutine;

    // ターン変更の多重起動を防ぐフラグ。
    // 演出待ちの間もターン終了ボタンは押せるため、連打やタイムアップとの競合で
    // ChangeTurn()が二重に走り、ターンが2回切り替わってドローも2回起きていた。
    bool isChangingTurn;

    [SerializeField] GameObject cardChangePanel;
    public bool isCardChange = false;
    public List<CardController> changedCardList = new List<CardController>();

    [SerializeField] TextMeshProUGUI effectText;
    [SerializeField] GameObject textPool;
    [SerializeField] TextMeshProUGUI damageText;


    [SerializeField] BattleAudioManager globalAudioManager;

    /// <summary>
    /// 攻撃・召喚・スペルの演出中や効果対象の選択中で、
    /// プレイヤーに新しい操作（召喚・スペル・攻撃）をさせてはいけない状態か。
    /// </summary>
    public bool IsPlayerActionLocked =>
        isSummoning || isAttacking || isCardsBattling || isEffectSelectPhase || castingSpell != null;

    public static GameManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (isOnCard) { return; }
            uiManager.CloseDescriptionPanel();
        }

        /*        if (!isPlayerTurn && isEffectSelectPhase)
                {
                    DropPlace.droppedCard.view.SetActiveSelectablePanel(false);
                    DropPlace.droppedCard.model.isFieldCard = false;
                    DropPlace.droppedCard.movement.PlayerSelectMoveOff(DropPlace.droppedCard);
                    DropPlace.droppedCard = null;
                    SelectingPanelOff();
                    isEffectSelectPhase = false;
                    EnableButtonCards();
                }*/
    }


    private bool skipNextCheck = false;

    void StartGame()
    {
        uiManager.HideResultPanel();
        uiManager.HideTwoPickResultPanel();

        if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.LETHAL_PUZZLE)
        {
            ApplyLethalPuzzle();
        }
        else if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.TWO_PICK)
        {
            player.deck = new List<int>(GameSession.SelectedDeck);

            List<int> enemyDeckForBattle = GameSession.GetTwoPickEnemyDeck();
            if (enemyDeckForBattle == null)
            {
                Debug.LogWarning("2Pick: 敵デッキが設定されていないため、プレイヤーと同じデッキを使用します。");
                enemyDeckForBattle = new List<int>(GameSession.SelectedDeck);
            }
            enemy.deck = enemyDeckForBattle;

            //テスト用: TwoPickモードのマナを最初から10にする
            player.manaCost = 10;
            player.defaultManaCost = 10;
            player.heroHp = 20; // テスト用: TwoPickモードの初期HPを30にする
            enemy.heroHp = 20;
            uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
            uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
            TurnEndButtonText.text = "Decide";
            StartCoroutine(SettingInitHand());
            changeCardCountDownCoroutine = StartCoroutine(CountDownChangeCard());
        }
        else if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.ROGUELIKE)
        {
            player.Init(RoguelikeSession.GameState.CurrentDeck);
            Debug.Log(RoguelikeSession.GameState.CurrentDeck.Count);
            enemy.Init(new List<int>() { 3, 4, 3, 3, 4, 4, 4, 4, 1 });
            uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
            uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
            TurnEndButtonText.text = "Decide";
            StartCoroutine(SettingInitHand());
            changeCardCountDownCoroutine = StartCoroutine(CountDownChangeCard());
        }
        else if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.CPU_BATTLE)
        {
            player.Init(new List<int>(GameSession.SelectedDeck));
            enemy.Init(new List<int>(GameSession.EnemyDeck));
            uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
            uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
            TurnEndButtonText.text = "Decide";
            StartCoroutine(SettingInitHand());
            changeCardCountDownCoroutine = StartCoroutine(CountDownChangeCard());
        }
        else
        {
            // GameModeが未設定の場合（デバッグ対戦など）。GameSessionにデッキが設定されていればそれを使い、
            // なければ従来通りの固定デッキにフォールバックする
            List<int> playerDeck = GameSession.SelectedDeck != null
                ? new List<int>(GameSession.SelectedDeck)
                : new List<int>() { 3, 2, 3, 4, 3, 2, 1, 1 };
            List<int> enemyDeck = GameSession.EnemyDeck != null
                ? new List<int>(GameSession.EnemyDeck)
                : new List<int>() { 2, 2, 2, 2, 4, 4, 4, 4, 1 };

            player.Init(playerDeck);
            enemy.Init(enemyDeck);
            uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
            uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
            TurnEndButtonText.text = "Decide";
            StartCoroutine(SettingInitHand());
            changeCardCountDownCoroutine = StartCoroutine(CountDownChangeCard());
        }

        UpdateDeckNum();
        StartCoroutine(WaitStartTurn());
    }

    void ApplyLethalPuzzle()
    {
        //手札、フィールド、HP、コスト等をSOから取得して設定
        //ModeConfigManager.Instance.twoPickList
        int selectNum = ModeConfigManager.Instance.LethalPuzzleIndex;
        LethalPuzzleData lethalPuzzleData = ModeConfigManager.Instance.lethalPuzzleList[selectNum];
        StartCoroutine(GenerateCardCoroutine(lethalPuzzleData.playerInitialHand, lethalPuzzleData.playerInitialField, lethalPuzzleData.enemyInitialField));

        player.Init(lethalPuzzleData.playerDeck);
        enemy.Init(lethalPuzzleData.playerDeck);

        uiManager.ShowHeroHP(lethalPuzzleData.playerInitialHP, lethalPuzzleData.enemyInitialHP);
        uiManager.ShowManaCost(lethalPuzzleData.playerInitialMana, enemy.manaCost);
    }

    void ApplyTwoPick()
    {
        //選択画面の作成が必要
    }

    void ApplyRogueolike()
    {

    }


    /// <summary>
    /// 選択されたカードを破棄して新しいカードを引く（破棄したものをリストへ追加）
    /// </summary>
    /// <param name="selectedCards"></param>
    public void ChangeHandCards()
    {
        if (isCardChange)
        {
            if (changedCardList.Count != 0)
                BattleAudioManager.Instance.PlaySE("CardCatch");
            foreach (CardController card in changedCardList)
                player.deck.Add(card.model.no);
            for (int i = 0; i < changedCardList.Count; i++)
            {
                ChangeDrawCard(player.deck, playerHandTransform);
                Destroy(changedCardList[i].gameObject);
            }
            changedCardList.Clear();
            isCardChange = false;

            cardChangePanel.SetActive(false);
            showDescriptionClicked = false;
            StartCoroutine(WaitStartTurn());
        }
    }

    IEnumerator CountDownChangeCard()
    {
        yield return new WaitForSeconds(1.5f);
        timeCount = 50;
        uiManager.UpdateTime(timeCount);

        while (timeCount > 0)
        {
            if (player.heroHp <= 0 || enemy.heroHp <= 0)
                yield break;

            yield return new WaitForSeconds(1);
            timeCount--;
            uiManager.UpdateTime(timeCount);
        }
        ChangeHandCards();
    }
    IEnumerator WaitStartTurn()
    {
        TurnEndButtonText.text = "Turn End";
        EnemyTurnZone();
        yield return new WaitForSeconds(1.5f);
        bool startTurn = true;
        isSummoning = true;
        effectBack.SetActive(true);
        BattleAudioManager.Instance.PlaySE("TurnChange");
        if (startTurn)
        {
            playerTurn.SetActive(true);
            StartCoroutine(TurnChangeAnimateText("Player Turn"));
            TurnEndButton.interactable = true;
        }
        else
        {
            enemyTurn.SetActive(true);
            TurnEndButton.interactable = false;
            StartCoroutine(TurnChangeAnimateText("Enemy Turn"));
        }
        yield return new WaitForSeconds(1.2f);
        playerTurn.SetActive(false);
        enemyTurn.SetActive(false);
        effectBack.SetActive(false);
        isPlayerTurn = true;
        PlayerTurnZone();
        isSummoning = false;
        TurnCalc();
    }


    public void ReduceManaCost(int cost, bool isPlayerCard)
    {

        if (isPlayerCard)
        {
            player.manaCost -= cost;
        }
        else
        {
            enemy.manaCost -= cost;
        }
        uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
    }


    public void Restart()
    {
        // handとFiledのカードを削除
        foreach (Transform card in playerHandTransform)
        {
            Destroy(card.gameObject);
        }
        foreach (Transform card in playerFieldTransform)
        {
            Destroy(card.gameObject);
        }
        foreach (Transform card in enemyHandTransform)
        {
            Destroy(card.gameObject);
        }
        foreach (Transform card in enemyFieldTransform)
        {
            Destroy(card.gameObject);
        }

        // デッキを生成
        player.deck = new List<int>() { 3, 1, 2, 2, 3 };
        enemy.deck = new List<int>() { 3, 1, 2, 1, 3 };

        StartGame();
    }

    public void MoveDeckEdit()
    {
        SceneTransition.Load(0);
    }

    public void DisableButtonCards()
    {
        //選択以外の操作をできないようにする
        //手札のカードをドラッグ不可にする
        TurnEndButton.interactable = false;
        CanvasGroup[] playerHandCards = playerHandTransform.GetComponentsInChildren<CanvasGroup>();
        foreach (CanvasGroup card in playerHandCards)
        {
            card.blocksRaycasts = false;
        }
    }

    public void EnableButtonCards()
    {
        //選択以外の操作をできるようにする
        //手札のカードをドラッグ可能にする
        TurnEndButton.interactable = true;
        CanvasGroup[] playerHandCards = playerHandTransform.GetComponentsInChildren<CanvasGroup>();
        foreach (CanvasGroup card in playerHandCards)
        {
            card.blocksRaycasts = true;
        }
    }

    public void EnemyTurnZone()
    {
        TurnEndButton.interactable = false;
        //spelldropzoneのオブジェクトのRaycastTargetをfalseにする
        playerFieldTransform.GetComponent<Image>().raycastTarget = false;
        enemyFieldTransform.GetComponent<Image>().raycastTarget = false;
    }

    void PlayerTurnZone()
    {
        //TurnEndButton.interactable = true;
        playerFieldTransform.GetComponent<Image>().raycastTarget = true;
        enemyFieldTransform.GetComponent<Image>().raycastTarget = true;

    }

    IEnumerator SettingInitHand()
    {
        // カードをそれぞれに3まい配る
        for (int i = 0; i < 5; i++)
        {
            //GiveCardToHand(player.deck, playerHandTransform);
            //GiveCardToHand(enemy.deck, enemyHandTransform);
            DrawCard(player.deck, playerHandTransform);
            DrawCard(enemy.deck, enemyHandTransform);
        }
        BattleAudioManager.Instance.PlaySE("CardCatch");
        yield return new WaitForSeconds(1.5f);
        //cardChangePanel.SetActive(true);
        TurnEndButton.GetComponent<Button>().interactable = true;
    }
    void GiveCardToHand(List<int> deck, Transform hand)
    {
        if (deck.Count == 0)
        {
            return;
        }
        int cardID = deck[0];
        deck.RemoveAt(0);
        UpdateDeckNum();
        CreateCard(cardID, hand);
    }

    public void DrawCard(List<int> deck, Transform hand)
    {
        if (deck.Count == 0)
        {
            return;
        }
        int cardID = deck[0];
        deck.RemoveAt(0);
        UpdateDeckNum();
        CreateCardEffect(cardID, hand);
    }

    void ChangeDrawCard(List<int> deck, Transform hand)
    {
        if (deck.Count == 0)
        {
            return;
        }
        int cardID = deck[0];
        deck.RemoveAt(0);
        UpdateDeckNum();
        CreateCardEffect(cardID, hand);
    }

    // 山札の残り枚数を表示する（手札に配ったカードは山札から除かれているので含まれない）
    void UpdateDeckNum()
    {
        uiManager.ShowDeckNum(player.deck.Count, enemy.deck.Count);
    }

    public void DrawCard(bool isPlayer)
    {
        if (isPlayer)
        {
            DrawCard(player.deck, playerHandTransform);
        }
        else
        {
            DrawCard(enemy.deck, enemyHandTransform);
        }
    }


    void CreateCardEffect(int cardID, Transform hand)
    {
        // Addressablesで非同期生成を開始
        // 第一引数はAddressableに設定したパス（またはアドレス名）
        Addressables.InstantiateAsync("Prefabs/Card", hand).Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 生成されたGameObjectを取得
                GameObject obj = handle.Result;
                CardController card = obj.GetComponent<CardController>();

                // データの初期化（元の処理）
                bool isPlayer = (hand.name == "PlayerHand");
                card.Init(cardID, isPlayer);

                // 親の設定（元の処理をそのまま適用）
                card.transform.SetParent(isPlayer ? playerDeck : enemyDeck);
                card.transform.localPosition = Vector3.zero;
                card.transform.localEulerAngles = Vector3.zero;
                card.transform.SetParent(card.transform.parent.parent);

                // エフェクトの実行
                card.movement.DrawEffect(card);
            }
            else
            {
                Debug.LogError("カードの生成に失敗しました: " + handle.OperationException);
            }
        };
    }

    void CreateCard(int cardID, Transform hand)
    {
        // カードの生成とデータの受け渡し
        CardController card = Instantiate(cardPrefab, hand, false);
        if (hand.name == "PlayerHand")
        {
            card.Init(cardID, true);
        }
        else
        {
            card.Init(cardID, false);
        }
    }

    public void EffectSearchCard(Transform hand, CardModel model)
    {
        CardController card = Instantiate(cardPrefab, hand, false);
        card.EffectCardInit(model);
    }

    public void EffectSummonCard(Transform hand, CardModel model, CardController baseCard)
    {
        CardController card = Instantiate(cardPrefab, hand, false);
        card.EffectCardInit(model);
        card.summonEffect(card.model.summonEffect, card.transform);
        if (baseCard.model.abilities.HasFlag(ABILITIES.SUMMON_SPECIFIC_UNIT))
        {
            // SetAbility()がダメージ無効の表示更新まで行い、その判定にisFieldCardを使うため、
            // 先に場のカードとして確定させてからアビリティを適用する
            card.model.isFieldCard = true;
            card.SetAbility(card);
            card.RefreshShieldPanel();
            if (card.CanUseAbilities())
            {
                if (card.model.isPlayerCard)
                {
                    card.movement.isDraggable = false;
                    card.movement.isHand = false;
                    card.OnFiledAbilities();
                }
                else
                {
                    card.UseAbilitiesTo();
                }
            }
        }
    }

    IEnumerator GenerateCardCoroutine(List<int> playerHand, List<int> playerField, List<int> enemyField)
    {
        // Addressableのアドレス名（適宜書き換えてください）
        string assetPath = "Prefabs/Card";

        // 1. 手札の生成
        foreach (int cardID in playerHand)
        {
            yield return CreateCardAsync(cardID, playerHandTransform, assetPath);
        }

        // 2. プレイヤーフィールドの生成
        foreach (int cardID in playerField)
        {
            yield return CreateFieldCardAsync(cardID, playerFieldTransform, true, assetPath);
        }

        // 3. 敵フィールドの生成
        foreach (int cardID in enemyField)
        {
            yield return CreateFieldCardAsync(cardID, enemyFieldTransform, false, assetPath);
        }
    }

    // 共通化：非同期でカードを生成して初期化するコルーチン
    IEnumerator CreateCardAsync(int cardID, Transform parent, string path)
    {
        var handle = Addressables.InstantiateAsync(path, parent);
        yield return handle; // ここで生成が終わるまで待機（フレーム分散）

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            CardController card = handle.Result.GetComponent<CardController>();
            card.Init(cardID, parent.name == "PlayerHand");
        }
    }

    // 共通化：非同期でフィールドカードを生成して初期化するコルーチン
    IEnumerator CreateFieldCardAsync(int cardID, Transform parent, bool isPlayer, string path)
    {
        var handle = Addressables.InstantiateAsync(path, parent);
        yield return handle;

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            CardController card = handle.Result.GetComponent<CardController>();
            card.movement.isHand = false;
            card.Init(cardID, isPlayer);
            card.model.isFieldCard = true;
            card.RefreshShieldPanel();
        }
    }


    /// <summary>
    /// 進行中のカウントダウンを止める。timeCountを共有しているため、
    /// 2つが同時に動くと残り時間が倍速で減ってしまう。
    /// </summary>
    void StopTurnTimers()
    {
        if (countDownCoroutine != null)
        {
            StopCoroutine(countDownCoroutine);
            countDownCoroutine = null;
        }
        if (changeCardCountDownCoroutine != null)
        {
            StopCoroutine(changeCardCountDownCoroutine);
            changeCardCountDownCoroutine = null;
        }
    }

    void TurnCalc()
    {
        // 以前はStopAllCoroutines()だったが、ダメージ数値表示や結果表示まで
        // 巻き込んで止めてしまうため、止めたいカウントダウンだけを止める。
        StopTurnTimers();
        countDownCoroutine = StartCoroutine(CountDown());
        if (isPlayerTurn)
        {
            PlayerTurn();
        }
        else
        {
            StartCoroutine(enemyAI.EnemyTurn());
        }
    }

    IEnumerator CountDown()
    {
        timeCount = 50;
        uiManager.UpdateTime(timeCount);

        while (timeCount > 0)
        {
            if (player.heroHp <= 0 || enemy.heroHp <= 0)
                yield break;

            yield return new WaitForSeconds(1);
            timeCount--;
            uiManager.UpdateTime(timeCount);
        }
        while (isSummoning || isAttacking)
        {
            yield return null;
        }
        if (!isSummoning && !isAttacking)
        {
            StartCoroutine(ChangeTurn());
        }
    }

    public Transform GetFriendHandFieldTransform(bool isPlayer)
    {
        if (isPlayer)
        {
            return playerHandTransform;
        }
        else
        {
            return enemyHandTransform;
        }
    }

    public Transform GetEnemyHandFieldTransform(bool isPlayer)
    {
        if (isPlayer)
        {
            return enemyHandTransform;
        }
        else
        {
            return playerHandTransform;
        }
    }

    public Transform GetEnemyFieldTransform(bool isPlayer)
    {
        if (isPlayer)
        {
            return enemyFieldTransform;
        }
        else
        {
            return playerFieldTransform;
        }
    }

    public Transform GetFriendFieldTransform(bool isPlayer)
    {
        if (isPlayer)
        {
            return playerFieldTransform;
        }
        else
        {
            return enemyFieldTransform;
        }
    }

    public CardController[] GetEnemyHandTransform(bool isPlayer)
    {
        if (isPlayer)
        {
            return enemyHandTransform.GetComponentsInChildren<CardController>();
        }
        else
        {
            return playerHandTransform.GetComponentsInChildren<CardController>();
        }
    }

    public CardController[] GetFriendHandTransform(bool isPlayer)
    {
        if (isPlayer)
        {
            return playerHandTransform.GetComponentsInChildren<CardController>();
        }
        else
        {
            return enemyHandTransform.GetComponentsInChildren<CardController>();
        }
    }

    // REDUCE_HAND_COST（手札のコストを下げる）のように、発動元のカード自身を対象に含めたくない効果用。
    // 発動元がまだ手札のTransform配下にいるかどうかは経路によって異なる
    // （プレイヤーはドラッグ開始時点で手札から外れている／AIは召喚演出中でまだ手札にいる）ため、
    // 常に自身を除外した「自分以外の手札」を返すことで、どちらの経路でも枚数判定と対象が一致する。
    public CardController[] GetFriendHandTransformExcept(bool isPlayer, CardController exclude)
    {
        CardController[] handCards = GetFriendHandTransform(isPlayer);
        List<CardController> targets = new List<CardController>();
        foreach (CardController handCard in handCards)
        {
            if (handCard == exclude) continue;
            targets.Add(handCard);
        }
        return targets.ToArray();
    }

    public CardController[] GetEnemyFieldCards(bool isPlayer)
    {
        if (isPlayer)
        {
            return enemyFieldTransform.GetComponentsInChildren<CardController>();
        }
        else
        {
            return playerFieldTransform.GetComponentsInChildren<CardController>();
        }
    }

    public CardController[] GetFriendFieldCards(bool isPlayer)
    {
        if (isPlayer)
        {
            return playerFieldTransform.GetComponentsInChildren<CardController>();
        }
        else
        {
            return enemyFieldTransform.GetComponentsInChildren<CardController>();
        }
    }

    /// <summary>
    /// 守護（SHIELD）に守られていて対象に選べないカードかどうかを返す。
    /// targetと同じ側の場に守護持ちがいて、target自身が守護でない場合にtrueになる。
    /// </summary>
    public bool IsBlockedByShield(CardController target)
    {
        if (target.model.abilities.HasFlag(ABILITIES.SHIELD)) return false;

        CardController[] sameSideCards = GetFriendFieldCards(target.model.isPlayerCard);
        return Array.Exists(sameSideCards, card => card.model.abilities.HasFlag(ABILITIES.SHIELD));
    }

    // HEAL_FRIEND_CARDS（自分のフォロワー全体回復）のように、
    // 発動元のカード自身を対象に含めたくないアビリティ用。
    // 発動時点で自分も場に出ている（=GetFriendFieldCards()に含まれる）ため、ここで除外する。
    public CardController[] GetFriendFieldCardsExcept(bool isPlayer, CardController exclude)
    {
        CardController[] fieldCards = GetFriendFieldCards(isPlayer);
        List<CardController> targets = new List<CardController>();
        foreach (CardController fieldCard in fieldCards)
        {
            if (fieldCard == exclude) continue;
            targets.Add(fieldCard);
        }
        return targets.ToArray();
    }



    public void OnClickTurnEndButton()
    {
        if (isCardChange) return;
        if (!isPlayerTurn) return;

        // 演出待ちの間もボタンは押せるため、押した時点で無効化して連打を防ぐ
        // （プレイヤーターン開始時にChangeTurn()内で再度有効になる）
        TurnEndButton.interactable = false;
        StartCoroutine(WaitAndChangeTurn());
    }

    private IEnumerator WaitAndChangeTurn()
    {
        while (isSummoning || isAttacking)
        {
            yield return null;
        }

        // 待っている間にタイムアップ等で既にターンが変わっていたら何もしない
        if (!isPlayerTurn) yield break;

        StartCoroutine(ChangeTurn());
    }
    //ターン変更時の通常ドロー時もsummonningをtrueにする
    public IEnumerator ChangeTurn()
    {
        //既に決着がついている場合はターン変更演出を出さない（負け演出とPlayerTurn演出が重複するのを防ぐ）
        if (player.heroHp <= 0 || enemy.heroHp <= 0)
            yield break;

        // ターン終了ボタンの連打とタイムアップ(CountDown)が競合すると多重に呼ばれる。
        // 二重に進むとターンが2回切り替わり、ドローも2回走ってしまう。
        if (isChangingTurn) yield break;
        isChangingTurn = true;

        if (DropPlace.droppedCard != null)
            DropPlace.droppedCard.gameObject.GetComponent<CardClickManager>().TimeUpSelect();
        isSummoning = true;
        EnemyTurnZone();
        effectBack.SetActive(true);

        if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.LETHAL_PUZZLE)
        {
            //詰将棋モードは自分のターン内に敵を倒せなければ敗北（heroHpは実際には減らさず、演出のためだけに0を渡す）
            //暗転(effectBack)は既にオンなので、遅延なしですぐ負けエフェクトを出す
            StartCoroutine(ShowResultPanel(0, true));
            yield break;
        }
        if (!isPlayerTurn)
        {
            playerTurn.SetActive(true);
            StartCoroutine(TurnChangeAnimateText("Player Turn"));
            TurnEndButton.interactable = true;
        }
        else
        {
            enemyTurn.SetActive(true);
            TurnEndButton.interactable = false;
            StartCoroutine(TurnChangeAnimateText("Enemy Turn"));
        }
        IsDraggFlgOff();
        // 演出が例外などで途中終了した場合に、次のターンも操作できないまま固まらないための保険
        isCardsBattling = false;
        castingSpell = null;
        BattleAudioManager.Instance.PlaySE("TurnChange");
        yield return new WaitForSeconds(1.2f);
        effectBack.SetActive(false);
        playerTurn.SetActive(false);
        enemyTurn.SetActive(false);

        isSummoning = false;
        PlayerTurnZone();
        //ターン変更演出

        CardController[] playerFieldCardList = playerFieldTransform.GetComponentsInChildren<CardController>();
        SettingCanAttackView(playerFieldCardList, false);
        CardController[] enemyFieldCardList = enemyFieldTransform.GetComponentsInChildren<CardController>();
        SettingCanAttackView(enemyFieldCardList, false);


        if (isPlayerTurn)
        {
            Debug.Log("Playerのターン");
            player.IncreaseManaCost();
            // 手札が3枚未満ならドロー
            if (GetFriendHandTransform(true).Length < 3)
            {
                // 山札が0枚の状態でドローしようとした場合はそのまま敗北
                if (player.deck.Count == 0)
                {
                    yield return new WaitForSeconds(1f);
                    player.heroHp = 0;
                    uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
                    uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
                    StartCoroutine(ShowResultPanel(player.heroHp, true));
                    yield break;
                }
                BattleAudioManager.Instance.PlaySE("CardCatch");
                DrawCard(player.deck, playerHandTransform);
            }

        }
        else
        {
            Debug.Log("Enemyのターン");
            enemy.IncreaseManaCost();
            // 手札が3枚未満ならドロー
            if (GetEnemyHandTransform(true).Length < 3)
            {
                // 山札が0枚の状態でドローしようとした場合はそのまま敗北
                if (enemy.deck.Count == 0)
                {
                    yield return new WaitForSeconds(1f);
                    enemy.heroHp = 0;
                    uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
                    uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
                    StartCoroutine(ShowResultPanel(player.heroHp, true));
                    yield break;
                }
                BattleAudioManager.Instance.PlaySE("CardCatch");
                DrawCard(enemy.deck, enemyHandTransform);
            }
        }
        uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
        if (isPlayerTurn && GetFriendHandTransform(true).Length < 3 ||
            !isPlayerTurn && GetEnemyHandTransform(true).Length < 3)
        {
            yield return new WaitForSeconds(1.3f);
        }

        isChangingTurn = false;
        TurnCalc();
    }

    public void SettingCanAttackView(CardController[] fieldCardList, bool canAttack)
    {
        foreach (CardController card in fieldCardList)
        {
            card.SetCanAttack(canAttack);
        }
    }

    void PlayerTurn()
    {
        //Debug.Log("Playerのターン");
        // フィールドのカードを攻撃可能にする
        CardController[] playerFieldCardList = playerFieldTransform.GetComponentsInChildren<CardController>();
        SettingCanAttackView(playerFieldCardList, true);
    }

    public IEnumerator CardsBattle(CardController attacker, CardController defender)
    {
        GameManager.instance.isAttacking = true;
        isCardsBattling = true;
        /*        Debug.Log("CardsBattle");
                Debug.Log("attacker HP:" + attacker.model.hp);
                Debug.Log("defender HP:" + defender.model.hp);*/
        attacker.Attack(defender);
        yield return new WaitForSeconds(0.8f + (attacker.model.attackTime / 60));
        defender.Defense(attacker);
        /*        Debug.Log("attacker HP:" + attacker.model.hp);
                Debug.Log("defender HP:" + defender.model.hp);*/
        yield return new WaitForSeconds(0.8f + (defender.model.attackTime / 60));
        StartCoroutine(attacker.CheckAlive());
        StartCoroutine(defender.CheckAlive());
        isCardsBattling = false;
    }


    public void AttackToHero(CardController attacker)
    {
        GameManager.instance.isAttacking = true;

        if (attacker.model.isPlayerCard)
        {
            attacker.AttackHero(enemyHero);
        }
        else if (!attacker.model.isPlayerCard)
        {
            attacker.AttackHero(playerHero);
        }

        //if (attacker.model.isPlayerCard)
        //{
        //    enemy.heroHp -= attacker.model.at;
        //}
        //else
        //{
        //    player.heroHp -= attacker.model.at;
        //}
        //uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
        //if (attacker.model.isDoubleAction)
        //{
        //    if (!attacker.model.isSingleAction)
        //    {
        //        attacker.SetCanAttack(true);
        //        attacker.model.isSingleAction = true;
        //    }
        //    else
        //    {
        //        attacker.SetCanAttack(false);
        //        attacker.model.isSingleAction = false;
        //    }
        //}
        //else
        //{
        //    attacker.SetCanAttack(false);
        //}
        //if (attacker.model.isStatsUpOnAttack)
        //{
        //    attacker.model.at += attacker.model.effectDmg;
        //    attacker.model.hp += attacker.model.effectHeal;
        //    attacker.RefreshView();
        //}
    }
    public void AttackToHeroSpell(CardController card)
    {
        GameManager.instance.isAttacking = true;
        if (card.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO))
        {
            if (card.model.isPlayerCard)
            {
                card.AttackHeroSpell(enemyHero);
            }
            else if (!card.model.isPlayerCard)
            {
                card.AttackHeroSpell(playerHero);
            }
        }
        if (card.model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
        {
            if (card.model.isPlayerCard)
            {
                card.AttackHeroSpell(playerHero);
            }
            else if (!card.model.isPlayerCard)
            {
                card.AttackHeroSpell(enemyHero);
            }
        }
    }

    public void AttackToHeroAbility(CardController card)
    {
        if (card.model.isPlayerCard)
        {
            enemy.heroHp -= card.model.effectDmg;
        }
        else
        {
            player.heroHp -= card.model.effectDmg;
        }
        uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
    }

    public void HealToHero(CardController healer)
    {
        if (healer.model.isPlayerCard)
        {
            player.heroHp += healer.model.at;
        }
        else
        {
            enemy.heroHp += healer.model.at;
        }
        uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
    }

    public void HealToHeroAbility(CardController card)
    {
        if (card.model.isPlayerCard)
        {
            player.heroHp += card.model.effectHeal;
        }
        else
        {
            enemy.heroHp += card.model.effectHeal;
        }
        uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
    }

    public void CheckHeroHP()
    {
        if (player.heroHp <= 0 || enemy.heroHp <= 0)
            StartCoroutine(ShowResultPanel(player.heroHp));
    }


    void ReturnRogueLikeMap()
    {
        if (player.heroHp <= 0)
        {
            SceneTransition.Load("Field");
        }
        else if (enemy.heroHp <= 0)
        {
            RoguelikeSession.IsBattleWin = true;
            DeckSetCards();
            SceneTransition.Load("Field");
        }
    }

    void DeckSetCards()
    {
        List<int> survivingCards = new List<int>();

        foreach (CardController card in playerHandTransform.GetComponentsInChildren<CardController>())
            survivingCards.Add(card.model.no);

        foreach (CardController card in playerFieldTransform.GetComponentsInChildren<CardController>())
            survivingCards.Add(card.model.no);

        survivingCards.AddRange(player.deck);

        RoguelikeSession.GameState.CurrentDeck = survivingCards;
    }

    IEnumerator ShowResultPanel(int heroHp, bool skipDelay = false)
    {
        // StopAllCoroutines();
        if (!skipDelay)
            yield return new WaitForSeconds(2f);
        //uiManager.ShowResultPanel(heroHp);
        //TODO:勝利、敗北のエフェクト
        effectBack.SetActive(true);
        if (heroHp <= 0)
        {
            loseEffect.SetActive(true);
            StartCoroutine(TurnChangeAnimateText("You Lose"));
        }
        else if (heroHp > 0)
        {
            winEffect.SetActive(true);
            StartCoroutine(TurnChangeAnimateText("You Win"));
        }
        yield return new WaitForSeconds(1.2f);
        effectBack.SetActive(false);
        //結果後の動作
        ReturnMode();
    }

    void ReturnMode()
    {
        if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.LETHAL_PUZZLE)
        {
            GameSession.LethalPuzzleFinished = true;
            GameSession.LethalPuzzleWon = enemy.heroHp <= 0;
            SceneTransition.Load("Field");
        }
        else if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.TWO_PICK)
        {
            if (enemy.heroHp <= 0)
            {
                // 勝利：次の対戦に進むか、やめるかをバトル内パネルで選択させる
                GameSession.TwoPickBattleIndex++;
                uiManager.ShowTwoPickResultPanel(GameSession.HasNextTwoPickBattle());
            }
            else
            {
                // 敗北：元のモード選択画面に戻る
                GameSession.TwoPickFinished = true;
                SceneTransition.Load("Field");
            }
        }
        else if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.ROGUELIKE)
        {
            ReturnRogueLikeMap();
        }else if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.CPU_BATTLE)
        {
            GameSession.CpuBattleFinished = true;
            GameSession.CpuBattleWon = enemy.heroHp <= 0;
            SceneTransition.Load("Field");
        }
        else if (GameSession.DebugReturnToDeckEdit)
        {
            // GameModeが未設定のデバッグ対戦。フラグはField側（ModeSelectionUI）で消費される
            SceneTransition.Load("Field");
        }
    }

    /// <summary>
    /// 2Pick勝利パネルの「次の対戦へ」ボタン
    /// </summary>
    public void OnTwoPickNextBattle()
    {
        uiManager.HideTwoPickResultPanel();
        SceneTransition.Load("Game");
    }

    /// <summary>
    /// 2Pick勝利パネルの「やめる」ボタン
    /// </summary>
    public void OnTwoPickGiveUp()
    {
        uiManager.HideTwoPickResultPanel();
        GameSession.TwoPickFinished = true;

        // 現在までの勝利数に応じた報酬を確定させる
        int moneyPerWin = GameSession.TwoPickData != null ? GameSession.TwoPickData.moneyPerWin : 0;
        GameSession.TwoPickReward = GameSession.TwoPickBattleIndex * moneyPerWin;

        SceneTransition.Load("Field");
    }


    public void SummonLightCenterOn()
    {
        summonLight.localPosition = new Vector3(0, 0, 0);
        summonLight.GetComponent<Image>().color = new Color(255, 255, 255, 255);
    }

    public void SummonLightLeftOn()
    {
        summonLight.localPosition = new Vector3(-800, 400, 0);
        summonLight.GetComponent<Image>().color = new Color(255, 255, 255, 255);
    }

    public ParticleSystem SummonTrailOn()
    {
        summonTrail.gameObject.SetActive(true);
        return summonTrail;
    }

    public ParticleSystem GetSummonEffect()
    {
        return summonEffect;
    }
    public void SummonEffectOff()
    {
        summonTrail.gameObject.SetActive(false);
    }

    public void ScaleYSummonLightFadeOut()
    {
        summonLight.DOScaleY(0f, 0.3f).OnComplete(() =>
        {

        });
    }

    public void ScaleYSummonLightCenterOn()
    {
        summonLight.localScale = new Vector3(2, 2, 1);
    }

    public void ScaleYSummonLightLeftOn()
    {
        summonLight.localScale = new Vector3(1, 1, 1);
    }

    public void SelectingPanelOn()
    {
        selectingPanel.gameObject.SetActive(true);
        Color initialColor = selectingPanel.color;
        initialColor.a = 0f;
        selectingPanel.color = initialColor;

        selectingPanel.DOFade(200f / 255f, 0.2f);
    }

    public void SelectingPanelOff()
    {
        selectingPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// ターン変更時のテキストアニメーション
    /// </summary>
    /// <returns></returns>
    public IEnumerator TurnChangeAnimateText(string text)
    {
        effectText.text = text;
        // 初期状態取得
        float startFontSize = effectText.fontSize;
        Color color = effectText.color;
        float startAlpha = color.a;

        // --- (1) 1.5秒で fontsize=100, alpha=190 ---
        float duration1 = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration1)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration1);

            effectText.fontSize = Mathf.Lerp(startFontSize, 100f, t);
            color.a = Mathf.Lerp(startAlpha, 190f / 255f, t);
            effectText.color = color;

            yield return null;
        }

        // --- (2) 0.5秒で fontsize=130, alpha=255 ---
        float duration2 = 0.7f;
        elapsed = 0f;

        while (elapsed < duration2)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration2);

            effectText.fontSize = Mathf.Lerp(100f, 130f, t);
            color.a = Mathf.Lerp(190f / 255f, 1f, t);
            effectText.color = color;

            yield return null;
        }

        float duration3 = 0.2f;
        elapsed = 0f;
        isPlayerTurn = !isPlayerTurn;
        while (elapsed < duration3)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration3);

            color.a = Mathf.Lerp(1f, 0f, t);
            effectText.color = color;

            yield return null;
        }
        
        // 完全に非表示に
        color.a = 0f;
        effectText.color = color;
    }

    void IsDraggFlgOff()
    {
        foreach (CardMovement card in playerHandTransform.GetComponentsInChildren<CardMovement>())
        {
            card.isDraggable = false;
        }
    }

    public IEnumerator GenDamageText(GameObject text, int damage, Transform cardTransform)
    {
        if (cardTransform == null) yield break;
        // ★yieldする前に座標を確定させる。
        //   この後の待機中に対象カードが破棄されても、既に値を持っているので例外にならない
        //   （CheckAlive()経由の同期Destroyと、Addressables生成時のyield待ちが重なると
        //   破棄済みTransformにアクセスしてMissingReferenceExceptionになっていたため）
        yield return StartCoroutine(GenDamageText(text, damage, cardTransform.position));
    }

    // 暗い赤・緑だと黒い縁取りとの差が出ず読みにくいため、明るめの色にしている
    static readonly Color DamageTextColor = new Color32(0xFF, 0x4D, 0x5E, 0xFF); // #FF4D5E
    static readonly Color HealTextColor = new Color32(0x5B, 0xE3, 0x7D, 0xFF);   // #5BE37D

    public IEnumerator GenDamageText(GameObject text, int damage, Vector3 position)
    {
        yield return StartCoroutine(GenFloatingText(text, damage.ToString(), DamageTextColor, position));
    }

    public IEnumerator GenHealText(GameObject text, int heal, Transform cardTransform)
    {
        if (cardTransform == null) yield break;
        // GenDamageText(Transform)と同じ理由で、yieldする前に座標を確定させる
        yield return StartCoroutine(GenHealText(text, heal, cardTransform.position));
    }

    public IEnumerator GenHealText(GameObject text, int heal, Vector3 position)
    {
        // 回復はダメージと同じ見た目だと区別がつかないため、色（緑）で区別する。
        // 数字だけを出す（プラス記号は付けない）
        yield return StartCoroutine(GenFloatingText(text, heal.ToString(), HealTextColor, position));
    }

    /// <summary>
    /// ダメージ／回復の数字をターゲット上にポップさせる共通処理。
    /// 表示位置・プール管理・DOTween演出はダメージも回復も同じなので、
    /// 文字列と色だけを差し替えてここに集約している。
    /// </summary>
    IEnumerator GenFloatingText(GameObject text, string label, Color color, Vector3 position)
    {
        // 1. 生成処理（Addressables化）
        if (text == null)
        {
            string assetPath = "Prefabs/DamageText"; // 指定のパス
            var handle = Addressables.InstantiateAsync(assetPath);

            // 生成完了まで1フレーム待機
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                text = handle.Result;
            }
            else if (damageText != null)
            {
                // Addressablesの読み込みに失敗しても、インスペクタ設定のプレハブで代替する
                text = Instantiate(damageText.gameObject);
            }
            else
            {
                Debug.LogError("ダメージテキストの生成に失敗しました");
                yield break;
            }
        }

        // 2. 初期設定
        text.transform.SetParent(textPool.transform);
        text.transform.position = position;

        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.color = color;
        tmp.alpha = 1f;

        text.transform.localScale = Vector3.zero;
        text.SetActive(true);

        // 3. DOTween演出
        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(text.transform.DOScale(1.5f, 0.2f).SetEase(Ease.OutBack));
        seq.Append(text.transform.DOScale(1.0f, 0.1f));
        // 表示中は上へ30px移動しながらフェードアウトさせる。
        // 移動量はCanvas上のpxで指定したいので、親(textPool)基準のローカル座標で動かす
        Vector3 startLocalPos = text.transform.localPosition;
        seq.Append(text.transform.DOLocalMoveY(startLocalPos.y + 30f, 0.3f));
        seq.Join(DOTween.To(() => tmp.alpha, a => tmp.alpha = a, 0f, 0.3f));

        // シーケンス終了まで待機
        yield return seq.WaitForCompletion();

        // 4. 後処理
        text.SetActive(false);
        // プールから再利用されたときに移動・フェード後の状態が残らないよう戻しておく
        text.transform.localPosition = startLocalPos;
        tmp.alpha = 1f;

        // ※もし「生成したインスタンス」をその都度破棄したい場合は以下を有効化
        // Addressables.ReleaseInstance(text);
    }


    public GameObject GetTextPool()
    {
        if (textPool.transform.childCount == 0)
            return null;

        for (int i = 0; i < textPool.transform.childCount; i++)
        {
            GameObject child = textPool.transform.GetChild(i).gameObject;
            // 非アクティブならそれを返す
            if (!child.activeSelf)
            {
                return child;
            }
        }
        return null;
    }

    List<int> GetDeck()
    {
        List<int> deck = new List<int>();

        return deck;
    }

}

/* 追加機能リスト
 * 手札をドラッグしたときに順番が変わらない
 * スペルやアビリティの追加　アビリティ複数持ち
 * フィールドの敵を倒す、フィールドの敵を自分のフィールドのカードにする
 * カードを複数枚引く、特定のユニットを手札orフィールドに出す
 * 選択した敵にダメージを与える、
 * 破壊された時に発動、場に出た時に発動
 * 手札のカードのコストを減らす
 * 自分のコストを増やす
 * 一度だけ受けるダメージを0にする
 * 二回行動
 * 攻撃した分回復する
 * 
 * 
 * 
 * エフェクト、演出の追加
 *  エフェクトと演出が一通りできればインゲーム完成
 *  
 * 
 * アウトゲーム部分の実装
 *  本編のゲーム進行
 *  フィールド探索
 *  デッキ作成
 *  カード購入
 *  別の遊び方
 *  
 *  タイムアップ処理
 *  ターン終了ボタン押下orタイムアップでターン終了
 *  ↓
 *  敵のターン開始演出
 *  ↓
 *  ドロー　時間Maxになる　
 *  行動完了
 *  ↓
 *  自ターン演出
 *  ↓
 *  ドロー
 *  ↓
 *  行動可能
 *  
 *  次にやること
 *  手札交換時間*
 *  コスト*
 *  勝敗処理
 *  場に出せるカードの上限設定*
 *  不具合探し
 *  手札上限*
 *  ドロップ時にカードの上からでもできるようにする*
 *  山札が無いときの処理*
 *  スペルの挙動*
 *  召喚や奪取時のフィールド上限
 *  ドラッグアンドドロップがおかしい*
 *      ターンが始まるより前にドロップできてしまうから
 *  ドラッグ時に矢印がおかしい*
 *  ターン交代の挙動
 *  ダメージの表示
 *  カード入れ替えの挙動
 *  
 *  
 *  テキスト設定
 *  
 *  ゲーム内の音とメニュー画面などのアウトゲームの音の管理方法
 *  遊び方の追加方法
 *  
 *  
 *  
 *  
 *  
 *  以下デバフの適応
 *  // 戦闘シーン側のBattleManagerで受け取って適用
private void Start()
{
    // バフ・デバフを適用してから戦闘開始
    if (RoguelikeSession.BattleModifiers != null)
        ApplyModifiers(RoguelikeSession.BattleModifiers);
}

private void ApplyModifiers(List<ParameterModifier> modifiers)
{
    foreach (ParameterModifier modifier in modifiers)
    {
        if (modifier.applyToPlayer)
            ApplyToPlayer(modifier);

        if (modifier.applyToEnemy)
            ApplyToEnemy(modifier);
    }
}

private void ApplyToPlayer(ParameterModifier modifier)
{
    switch (modifier.modifierType)
    {
        case ParameterModifierType.HP_BOOST:
            RoguelikeSession.GameState.MaxHP    += modifier.value;
            RoguelikeSession.GameState.CurrentHP += modifier.value;
            break;
        case ParameterModifierType.HP_REDUCTION:
            RoguelikeSession.GameState.MaxHP    -= modifier.value;
            RoguelikeSession.GameState.CurrentHP =
                Mathf.Min(RoguelikeSession.GameState.CurrentHP,
                          RoguelikeSession.GameState.MaxHP);
            break;
        case ParameterModifierType.MANA_BOOST:
            // マナ処理
            break;
        case ParameterModifierType.MANA_REDUCTION:
            // マナ処理
            break;
        case ParameterModifierType.CARD_DRAW_BOOST:
            // ドロー枚数処理
            break;
        case ParameterModifierType.CARD_DRAW_REDUCTION:
            // ドロー枚数処理
            break;
    }
}

private void ApplyToEnemy(ParameterModifier modifier)
{
    // 敵のステータスに同様に適用
}
*/

/*public void OnBattleWin()
{
    // 報酬処理などを済ませてからローグライクシーンへ戻る
    RoguelikeSession.GameState.CurrentDeck = *//* 更新後のデッキ *//*;
    SceneManager.LoadScene("RoguelikeScene");
}

public void OnBattleLose()
{
    SceneManager.LoadScene("RoguelikeScene");
    // ローグライクシーン側でGameOver処理
}*/


/*public void OnBattleWin()
{
    RoguelikeSession.IsBattleWin = true;
    SceneManager.LoadScene("RoguelikeScene");
}
*/