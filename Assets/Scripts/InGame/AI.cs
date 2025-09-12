using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    GameManager gameManager;
    private void Start()
    {
        gameManager = GameManager.instance;
    }
    public IEnumerator EnemyTurn()
    {
        Debug.Log("Enemyのターン");
        // フィールドのカードを攻撃可能にする
        CardController[] enemyFieldCardList = gameManager.enemyFieldTransform.GetComponentsInChildren<CardController>();
        gameManager.SettingCanAttackView(enemyFieldCardList, true);

        yield return new WaitForSeconds(1);

        /* 場にカードをだす */
        // 手札のカードリストを取得
        CardController[] handCardList = gameManager.enemyHandTransform.GetComponentsInChildren<CardController>();

        // コスト以下のカードがあれば、カードをフィールドに出し続ける
        // 条件：モンスターカードならコストのみ
        // 条件：スペルならコストと、使用可能かどうか（CanUseSpell）
        while (Array.Exists(handCardList, card => (card.model.cost <= gameManager.enemy.manaCost) && (!card.IsSpell || (card.IsSpell && card.CanUseSpells()))) && gameManager.timeCount > 0)//CanUseSpell()
        {
            // コスト以下のカードリストを取得
            CardController[] selectableHandCardList = Array.FindAll(handCardList, card => (card.model.cost <= gameManager.enemy.manaCost) && (!card.IsSpell || (card.IsSpell && card.CanUseSpells())));//CanUseSpell()
            // 場に出すカードを選択
            CardController selectCard = selectableHandCardList[0];
            //　カードを表にする
            selectCard.Show();
            // スペルカードなら使用する
            if (selectCard.IsSpell)
            {
                StartCoroutine(CastSpellOf(selectCard));
            }
            else
            {
                // カードを移動
                StartCoroutine(selectCard.movement.SummonMove(selectCard, gameManager.enemyFieldTransform));
                yield return new WaitForSeconds(0.5f);
                selectCard.OnFiled();
                //アビリティ発動
                if (selectCard.IsAbilities && selectCard.CanUseAbilities())
                {
                    StartCoroutine(CastAbilityOf(selectCard));
                }
            }
            yield return new WaitForSeconds(2);
            handCardList = gameManager.enemyHandTransform.GetComponentsInChildren<CardController>();
        }



        yield return new WaitForSeconds(1);
        /* 攻撃 */
        // フィールドのカードリストを取得
        CardController[] fieldCardList = gameManager.enemyFieldTransform.GetComponentsInChildren<CardController>();

        //攻撃可能カードがあれば攻撃を繰り返す
        while (Array.Exists(fieldCardList, card => card.model.canAttack) && gameManager.timeCount > 0)
        {

            if (GameManager.instance.isAttacking || GameManager.instance.isSummoning)
            {
                yield return null;
                continue;
            }

            // 攻撃可能カードを取得
            CardController[] enemyCanAttackCardList = Array.FindAll(fieldCardList, card => card.model.canAttack); // 検索：Array.FindAll
            CardController[] playerFieldCardList = gameManager.playerFieldTransform.GetComponentsInChildren<CardController>();

            // attackerカードを選択
            CardController attacker = enemyCanAttackCardList[0];
            if (playerFieldCardList.Length > 0)
            {

                CardController card = new CardController();

                if (!attacker.model.abilities.HasFlag(ABILITIES.PIERCE))
                {
                    // defenderカードを選択
                    // シールドカードのみ攻撃対象にする
                    if (Array.Exists(playerFieldCardList, card => card.model.abilities.HasFlag(ABILITIES.SHIELD)))
                    {
                        playerFieldCardList = Array.FindAll(playerFieldCardList, card => card.model.abilities.HasFlag(ABILITIES.SHIELD));
                    }
                }
                CardController defender = playerFieldCardList[0];
                // attackerとdefenderを戦わせる
                //StartCoroutine(attacker.movement.MoveToTarget(defender.transform));
                yield return new WaitForSeconds(0.51f);
                StartCoroutine(gameManager.CardsBattle(attacker, defender));

            }
            else
            {
                StartCoroutine(attacker.movement.MoveToTarget(gameManager.playerHero));
                yield return new WaitForSeconds(0.25f);
                gameManager.AttackToHero(attacker);
                yield return new WaitForSeconds(0.25f);
                gameManager.CheckHeroHP();
            }
            fieldCardList = gameManager.enemyFieldTransform.GetComponentsInChildren<CardController>();
            yield return new WaitForSeconds(2);
        }

        yield return new WaitForSeconds(1);
        StartCoroutine(gameManager.ChangeTurn());
    }

    IEnumerator CastAbilityOf(CardController card)
    {
        CardController target = null;
        if (card.model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARD) || card.model.abilities.HasFlag(ABILITIES.DESTROY_ENEMY_CARD) || card.model.abilities.HasFlag(ABILITIES.STEAL_ENEMY_CARD))
        {
            //destoryは先頭ではなく体力を調べさせる
            target = gameManager.GetEnemyFieldCards(card.model.isPlayerCard)[0];
        }
        if (card.model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARD))
        {
            //　対象を選ぶときは計算させる
            target = card;
        }
        yield return new WaitForSeconds(0.75f);
        card.UseAbilitiesTo(target);
    }

    IEnumerator CastSpellOf(CardController card)
    {
        CardController target = null;
        Transform movePosition = null;
        if (card.model.spells.HasFlag(SPELLS.STEAL_ENEMY_CARD) || card.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARD))
        {
            target = gameManager.GetEnemyFieldCards(card.model.isPlayerCard)[0];
            movePosition = target.transform;
        }
        else if (card.model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARD))
        {
            target = gameManager.GetFriendFieldCards(card.model.isPlayerCard)[0];
            movePosition = target.transform;
        }
        else if (card.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS))
        {
            movePosition = gameManager.playerFieldTransform;
        }
        else if (card.model.spells.HasFlag(SPELLS.DRAW_CARDS) || card.model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS))
        {
            movePosition = gameManager.enemyFieldTransform;
        }
        else if (card.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO))
        {
            movePosition = gameManager.playerHero;
        }
        else if (card.model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
        {
            movePosition = gameManager.enemyHero;
        }
        if (card.model.spells.HasFlag(SPELLS.RANDOM_DAMAGE))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(card.model.isPlayerCard);
            target = enemyCards[UnityEngine.Random.Range(0, enemyCards.Length - 1)];
            movePosition = target.transform;
        }
        if (card.model.spells.HasFlag(SPELLS.DESTROY_ALL_FIELD_CARDS))
        {
            Transform enemyCards = gameManager.GetFriendFieldTransform(card.model.isPlayerCard);
            movePosition = enemyCards.transform;
        }
        //　ターゲット/それぞれのフィールド/それぞれのHeroのTransformが必要
        StartCoroutine(card.movement.MoveLeftSpell(card));
        //　スペル発動時に一回転して中央に移動したから右に回転しながら移動
        yield return new WaitForSeconds(0.9f);
        card.spellEffect(target, true);
        //card.UseSpellTo(target);//スペルエフェクト
    }

}
