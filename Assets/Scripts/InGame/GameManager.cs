using Coffee.UIExtensions;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

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

    public RectTransform summonLight;
    [SerializeField] UIParticle UIParticleObj;
    public ParticleSystem summonTrail;
    public ParticleSystem summonEffect;
    public UIParticlesManager uiParticlesManager;
    public CameraShake cameraShake;
    [SerializeField] Image selectingPanel;

    [SerializeField] Transform playerDeck;
    [SerializeField] Transform enemyDeck;


    public bool isSummoning = false;
    public bool isAttacking;
    public bool isOnCard;
    public bool showDescriptionClicked;

    // 時間管理
    public int timeCount;

    // シングルトン化（どこからでもアクセスできるようにする）
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
        player.Init(new List<int>() { 2, 2, 2, 2, 3, 3, 1, 1 });
        enemy.Init(new List<int>() { 3, 3, 3, 3, 3, 4, 4, 4, 1 });

        uiManager.ShowHeroHP(player.heroHp, enemy.heroHp);
        uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
        SettingInitHand();
        isPlayerTurn = true;
        showDescriptionClicked = false;
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

    void SettingInitHand()
    {
        // カードをそれぞれに3まい配る
        for (int i = 0; i < 3; i++)
        {
            GiveCardToHand(player.deck, playerHandTransform);
            GiveCardToHand(enemy.deck, enemyHandTransform);
        }
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
        // カードの生成とデータの受け渡し
        CardController card = Instantiate(cardPrefab, hand, false);
        if (hand.name == "PlayerHand")
        {
            card.Init(cardID, true);
            card.transform.SetParent(playerDeck);
        }
        else
        {
            card.Init(cardID, false);
            card.transform.SetParent(enemyDeck);
        }

        card.transform.localPosition = Vector3.zero;
        card.transform.localEulerAngles = Vector3.zero;
        card.transform.SetParent(card.transform.parent.parent);
        //
        card.movement.DrawEffect(card);
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

        isPlayerTurn = !isPlayerTurn;
        isSummoning = true;
        yield return new WaitForSeconds(4f);
        isSummoning = false;
        //ターン変更演出




        CardController[] playerFieldCardList = playerFieldTransform.GetComponentsInChildren<CardController>();
        SettingCanAttackView(playerFieldCardList, false);
        CardController[] enemyFieldCardList = enemyFieldTransform.GetComponentsInChildren<CardController>();
        SettingCanAttackView(enemyFieldCardList, false);


        if (isPlayerTurn)
        {
            player.IncreaseManaCost();
            DrawCard(player.deck, playerHandTransform);
        }
        else
        {
            enemy.IncreaseManaCost();
            DrawCard(enemy.deck, enemyHandTransform);
        }
        uiManager.ShowManaCost(player.manaCost, enemy.manaCost);
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
        attacker.CheckAlive();
        defender.CheckAlive();
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
        {
            ShowResultPanel(player.heroHp);
        }
    }
    void ShowResultPanel(int heroHp)
    {
        StopAllCoroutines();
        uiManager.ShowResultPanel(heroHp);
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
 * 
*/