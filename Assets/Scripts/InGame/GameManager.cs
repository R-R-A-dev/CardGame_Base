using Coffee.UIExtensions;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
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
    [SerializeField] TextMeshProUGUI TurnEndButtonText;

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
    public bool isOnCard;
    public bool showDescriptionClicked;

    // 時間管理
    public int timeCount;

    [SerializeField] GameObject cardChangePanel;
    public bool isCardChange = false;
    public List<CardController> changedCardList = new List<CardController>();

    [SerializeField] TextMeshProUGUI effectText;
    [SerializeField] GameObject textPool;
    [SerializeField] TextMeshProUGUI damageText;


    [SerializeField] BattleAudioManager globalAudioManager;

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

        if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.LETHAL_PUZZLE)
        {
            ApplyLethalPuzzle();
        }
        else if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.TWO_PICK)
        {
            player.deck = GameSession.SelectedDeck;
            enemy.deck = GameSession.SelectedDeck;
            uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
            uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
            TurnEndButtonText.text = "Decide";
            StartCoroutine(SettingInitHand());
            StartCoroutine(CountDownChangeCard());
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
            StartCoroutine(CountDownChangeCard());
        }
        else
        {
            player.Init(new List<int>() { 3, 2, 3, 4, 3, 2, 1, 1 });
            enemy.Init(new List<int>() { 2, 2, 2, 2, 4, 4, 4, 4, 1 });
            //player.Init(GameSession.SelectedDeck);
            //enemy.Init(new List<int>(GameSession.SelectedDeck));
            uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
            uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
            TurnEndButtonText.text = "Decide";
            StartCoroutine(SettingInitHand());
            StartCoroutine(CountDownChangeCard());
        }

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
            enemy.manaCost -= cost;
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
        SceneManager.LoadScene(0);
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
        for (int i = 0; i < 2; i++)
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
        CreateCardEffect(cardID, hand);
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
            card.SetAbility(card);
            card.model.isFieldCard = true;
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
        }
    }


    void TurnCalc()
    {
        StopAllCoroutines();
        StartCoroutine(CountDown());
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



    public void OnClickTurnEndButton()
    {
        if (isCardChange) return;

        if (isPlayerTurn)
        {
            StartCoroutine(WaitAndChangeTurn());
        }
    }

    private IEnumerator WaitAndChangeTurn()
    {
        while (isSummoning || isAttacking)
        {
            yield return null;
        }

        if (!isSummoning && !isAttacking)
        {
            StartCoroutine(ChangeTurn());
        }
    }
    //ターン変更時の通常ドロー時もsummonningをtrueにする
    public IEnumerator ChangeTurn()
    {
        if (DropPlace.droppedCard != null)
            DropPlace.droppedCard.gameObject.GetComponent<CardClickManager>().TimeUpSelect();
        isSummoning = true;
        EnemyTurnZone();
        effectBack.SetActive(true);

        if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.LETHAL_PUZZLE)
        {
            //詰将棋モードの場合、ゲームオーバー処理
            Debug.Log("Lethal Puzzle Mode: Game Over on Turn Change");
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
            SceneManager.LoadScene("Field");
        }
        else if (enemy.heroHp <= 0)
        {
            RoguelikeSession.IsBattleWin = true;
            DeckSetCards();
            SceneManager.LoadScene("Field");
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

    IEnumerator ShowResultPanel(int heroHp)
    {
        // StopAllCoroutines();
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
            
        }
        else if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.TWO_PICK)
        {
            
        }
        else if (ModeConfigManager.Instance != null && ModeConfigManager.Instance.currentGameMode == GameMode.ROGUELIKE)
        {
            ReturnRogueLikeMap();
        }
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
            else
            {
                Debug.LogError("ダメージテキストの生成に失敗しました");
                yield break;
            }
        }

        // 2. 初期設定
        text.transform.SetParent(textPool.transform);
        text.transform.position = cardTransform.position;

        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = damage.ToString();
        tmp.color = Color.red;
        tmp.alpha = 1f;

        text.transform.localScale = Vector3.zero;
        text.SetActive(true);

        // 3. DOTween演出
        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(text.transform.DOScale(1.5f, 0.2f).SetEase(Ease.OutBack));
        seq.Append(text.transform.DOScale(1.0f, 0.1f));
        seq.AppendInterval(0.3f);

        // シーケンス終了まで待機
        yield return seq.WaitForCompletion();

        // 4. 後処理
        text.SetActive(false);

        // ※もし「生成したインスタンス」をその都度破棄したい場合は以下を有効化
        // Addressables.ReleaseInstance(text);
    }

    public IEnumerator GenHealText(GameObject text, int damage, Transform cardTransform)
    {
        if (text == null)
        {
            text = Instantiate(damageText.gameObject);
        }
        text.transform.SetParent(textPool.transform);
        text.transform.position = cardTransform.position;
        text.GetComponent<TextMeshProUGUI>().text = damage.ToString();
        text.GetComponent<TextMeshProUGUI>().color = Color.green;
        text.SetActive(true);
        //0.5秒後に消えてtextPoolの子オブジェクトに戻る
        yield return new WaitForSeconds(0.5f);
        text.SetActive(false);

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