using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.Rendering.GPUSort;

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
        while (GameManager.instance.isAttacking || GameManager.instance.isSummoning)
        {
            yield return null;
            continue;
        }
        yield return new WaitForSeconds(1);
        // フィールドのカードを攻撃可能にする
        CardController[] enemyFieldCardList = gameManager.enemyFieldTransform.GetComponentsInChildren<CardController>();
        gameManager.SettingCanAttackView(enemyFieldCardList, true);

        /* 場にカードをだす */
        // 手札のカードリストを取得
        CardController[] handCardList = gameManager.enemyHandTransform.GetComponentsInChildren<CardController>();
        CardController[] fieldCardList = gameManager.GetEnemyFieldCards(true);

        // コスト以下のカードがあれば、カードをフィールドに出し続ける
        // 条件：モンスターカードならコストのみ
        // 条件：スペルならコストと、使用可能かどうか（CanUseSpell）
        while (Array.Exists(handCardList, card => (card.model.cost <= gameManager.enemy.manaCost) && (!card.IsSpell || (card.IsSpell && card.CanUseSpells()))) && gameManager.timeCount > 0 )
        {
            while (GameManager.instance.isAttacking || GameManager.instance.isSummoning)
            {
                yield return null;
                continue;
            }

            // コスト以下のカードリストを取得
            CardController[] selectableHandCardList = Array.FindAll(handCardList, card => (card.model.cost <= gameManager.enemy.manaCost) && (!card.IsSpell || (card.IsSpell && card.CanUseSpells())));//CanUseSpell()
                                                                                                                                                                                                       // 場に出すカードを選択
            CardController selectCard = Array.Find(
                selectableHandCardList,
                card => !(gameManager.GetEnemyFieldCards(true).Length > 4 && card.model.spells == SPELLS.NONE)
            );
            
            if(selectCard == null) break;
            

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
                    yield return new WaitForSeconds(2);
                }
            }
            if (GameManager.instance.player.heroHp <= 0 || GameManager.instance.enemy.heroHp <= 0)
                yield break;

            yield return new WaitForSeconds(2);
            handCardList = gameManager.enemyHandTransform.GetComponentsInChildren<CardController>();
        }



        yield return new WaitForSeconds(1);
        /* 攻撃 */
        // フィールドのカードリストを取得
        fieldCardList = gameManager.enemyFieldTransform.GetComponentsInChildren<CardController>();


        //攻撃可能カードがあれば攻撃を繰り返す
        while (Array.Exists(fieldCardList, card => card.model.canAttack) && gameManager.timeCount > 0)
        {

            while (GameManager.instance.isAttacking || GameManager.instance.isSummoning)
            {
                yield return null;
                continue;
            }
            // 攻撃可能カードを取得
            CardController[] enemyCanAttackCardList = Array.FindAll(fieldCardList, card => card.model.canAttack); // 検索：Array.FindAll
            CardController[] playerFieldCardList = gameManager.playerFieldTransform.GetComponentsInChildren<CardController>();

            // attackerカードを選択
            CardController attacker = enemyCanAttackCardList[0];
            CardController defender = GetFirstZeroOrLess(playerFieldCardList);

            if (playerFieldCardList.Length > 0 && defender != null)
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

                // attackerとdefenderを戦わせる
                //StartCoroutine(attacker.movement.MoveToTarget(defender.transform));
                yield return new WaitForSeconds(0.51f);
                StartCoroutine(gameManager.CardsBattle(attacker, defender));

            }
            else
            {
                //StartCoroutine(attacker.movement.MoveToTarget(gameManager.playerHero));
                yield return new WaitForSeconds(0.25f);
                gameManager.AttackToHero(attacker);
                yield return new WaitForSeconds(0.25f);
                gameManager.CheckHeroHP();
                if (GameManager.instance.player.heroHp <= 0 || GameManager.instance.enemy.heroHp <= 0)
                    yield break;
            }
            fieldCardList = gameManager.enemyFieldTransform.GetComponentsInChildren<CardController>();
            yield return new WaitForSeconds(2);
        }

        yield return new WaitForSeconds(1);
        StartCoroutine(gameManager.ChangeTurn());
    }

    public CardController GetFirstZeroOrLess(CardController[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].model.hp > 0)
            {
                return array[i];
            }
        }
        return null;
    }

    IEnumerator CastAbilityOf(CardController card)
    {
        yield return new WaitForSeconds(1.5f);
        CardController target = null;
        Transform movePosition = null;
        CardController[] targets = null;
        if (card.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY) && card.model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARD) ||
            card.model.abilities.HasFlag(ABILITIES.DESTROY_ENEMY_CARD) || card.model.abilities.HasFlag(ABILITIES.STEAL_ENEMY_CARD) ||
            card.model.abilities.HasFlag(ABILITIES.CONDITIONAL_ENEMY_DEBUFF))
        {
            target = gameManager.GetEnemyFieldCards(card.model.isPlayerCard)[0];
        }
        else if (card.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND) && card.model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARD))
            target = gameManager.GetFriendFieldCards(card.model.isPlayerCard)[0];

        else if (card.model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARDS))
            targets = gameManager.GetEnemyFieldCards(card.model.isPlayerCard);

        else if (card.model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARDS))
            targets = gameManager.GetFriendFieldCards(card.model.isPlayerCard);

        else if (card.model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_HERO) || card.model.abilities.HasFlag(ABILITIES.HEAL_BY_DAMAGE))
        {
            yield return new WaitForSeconds(0.5f);
            movePosition = gameManager.playerHero;
            card.attackSpellEffectHero(movePosition, true);
            yield break;
        }
        else if (card.model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_HERO))
        {
            yield return new WaitForSeconds(0.5f);
            movePosition = gameManager.enemyHero;
            card.attackSpellEffectHero(movePosition, true);
            yield break;
        }
        else if (card.model.abilities.HasFlag(ABILITIES.DRAW_CARDS))
        {
            GameManager.instance.isAttacking = true;
        }

        if (card.model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST) || card.model.abilities.HasFlag(ABILITIES.DISCARD_ALL_FRIEND_HAND))
        {
            CardController[] hand = gameManager.GetFriendHandTransform(card.model.isPlayerCard);
            targets = new CardController[hand.Length];
            int index = 0;
            for (int i = 0; i < hand.Length; i++)
            {
                targets[index] = hand[i];
                index++;
            }
        }
        if (card.model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST) || card.model.abilities.HasFlag(ABILITIES.DISCARD_ALL_ENEMY_HAND))
            targets = gameManager.GetEnemyHandTransform(card.model.isPlayerCard);

        if (card.model.abilities.HasFlag(ABILITIES.DISCARD_ENEMY_HAND))
            target = gameManager.GetEnemyHandTransform(card.model.isPlayerCard)[0];

        if (card.model.abilities.HasFlag(ABILITIES.DISCARD_FRIEND_HAND))
        {
            CardController[] hand = gameManager.GetFriendHandTransform(card.model.isPlayerCard);
            targets = new CardController[hand.Length];

            int index = 0;
            for (int i = 0; i < hand.Length; i++)
            {
                targets[index] = hand[i];
                index++;
            }
            target = targets[0];
        }

        if (card.model.abilities.HasFlag(ABILITIES.RANDOM_ENEMY))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(card.model.isPlayerCard);
            target = enemyCards[UnityEngine.Random.Range(0, enemyCards.Length)];
        }
        else if (card.model.abilities.HasFlag(ABILITIES.RANDOM_FRIEND))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(card.model.isPlayerCard);
            target = friendCards[UnityEngine.Random.Range(0, friendCards.Length)];
        }

        if (target != null || targets != null)
        {
            card.transform.DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd);
            if (card.model.abilities.HasFlag(ABILITIES.DAMAGE_ENEMY_CARDS) || card.model.abilities.HasFlag(ABILITIES.HEAL_FRIEND_CARDS) ||
                card.model.abilities.HasFlag(ABILITIES.INCREASE_ENEMY_COST) || card.model.abilities.HasFlag(ABILITIES.DISCARD_ALL_ENEMY_HAND) ||
                card.model.abilities.HasFlag(ABILITIES.REDUCE_HAND_COST) || card.model.abilities.HasFlag(ABILITIES.DISCARD_ALL_FRIEND_HAND))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    card.AbilityEffect(targets[i], true);
                }
            }
            else
            {
                card.AbilityEffect(target, true);
            }
        }
        if (card.model.abilities.HasFlag(ABILITIES.SUMMON_SPECIFIC_UNIT))
        {
            card.UseAbilitiesTo();
        }
    }

    IEnumerator CastSpellOf(CardController card)
    {
        CardController target = null;
        Transform movePosition = null;
        CardController[] targets = null;
        if (card.model.spells.HasFlag(SPELLS.STEAL_ENEMY_CARD) || card.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARD) || card.model.spells.HasFlag(SPELLS.CONDITIONAL_ENEMY_DEBUFF) ||
            card.model.spells.HasFlag(SPELLS.DESTROY_ENEMY_CARD) || card.model.spells.HasFlag(SPELLS.SWAP_HP_ATK) && card.model.spells.HasFlag(SPELLS.EFFECT_SELECTION_ENEMY))
        {
            target = gameManager.GetEnemyFieldCards(card.model.isPlayerCard)[0];
        }
        else if (card.model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARD) || card.model.spells.HasFlag(SPELLS.CONDITIONAL_FRIEND_BUFF) &&
                card.model.spells.HasFlag(SPELLS.EFFECT_SELECTION_FRIEND))
        {
            target = gameManager.GetFriendFieldCards(card.model.isPlayerCard)[0];
        }
        else if (card.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS))
        {
            targets = gameManager.GetEnemyFieldCards(card.model.isPlayerCard);
        }
        else if (card.model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS))
        {
            targets = gameManager.GetFriendFieldCards(card.model.isPlayerCard);
        }
        else if (card.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_HERO) || card.model.spells.HasFlag(SPELLS.HEAL_BY_DAMAGE))
        {
            StartCoroutine(card.movement.MoveLeftSpell(card));
            yield return new WaitForSeconds(0.9f);
            movePosition = gameManager.playerHero;
            card.attackSpellEffectHero(movePosition, true);
            yield break;
        }
        else if (card.model.spells.HasFlag(SPELLS.HEAL_FRIEND_HERO))
        {
            StartCoroutine(card.movement.MoveLeftSpell(card));
            yield return new WaitForSeconds(0.9f);
            movePosition = gameManager.enemyHero;
            card.attackSpellEffectHero(movePosition, true);
            yield break;
        }
        else if (card.model.spells.HasFlag(SPELLS.DRAW_CARDS))
        {
            transform.SetParent(transform.parent.parent);
            StartCoroutine(card.movement.MoveLeftSpell(card));
            //yield return new WaitForSeconds(0.9f);

        }
        if (card.model.spells.HasFlag(SPELLS.DESTROY_ALL_FIELD_CARDS))
        {
            CardController[] enemys = gameManager.GetEnemyFieldCards(card.model.isPlayerCard);
            CardController[] friends = gameManager.GetFriendFieldCards(card.model.isPlayerCard);
            targets = enemys.Concat(friends).ToArray();
        }

        if (card.model.spells.HasFlag(SPELLS.REDUCE_HAND_COST) || card.model.spells.HasFlag(SPELLS.DISCARD_ALL_FRIEND_HAND))
        {
            CardController[] hand = gameManager.GetFriendHandTransform(card.model.isPlayerCard);
            targets = new CardController[hand.Length - 1];

            int index = 0;
            for (int i = 0; i < hand.Length; i++)
            {
                if (card == hand[i])
                    continue;

                targets[index] = hand[i];
                index++;
            }
        }
        if (card.model.spells.HasFlag(SPELLS.INCREASE_ENEMY_COST) || card.model.spells.HasFlag(SPELLS.DISCARD_ALL_ENEMY_HAND))
        {
            targets = gameManager.GetEnemyHandTransform(card.model.isPlayerCard);
        }
        if (card.model.spells.HasFlag(SPELLS.DISCARD_ENEMY_HAND))
        {
            target = gameManager.GetEnemyHandTransform(card.model.isPlayerCard)[0];
        }
        if (card.model.spells.HasFlag(SPELLS.DISCARD_FRIEND_HAND))
        {
            CardController[] hand = gameManager.GetFriendHandTransform(card.model.isPlayerCard);
            targets = new CardController[hand.Length - 1];

            int index = 0;
            for (int i = 0; i < hand.Length; i++)
            {
                if (card == hand[i])
                    continue;

                targets[index] = hand[i];
                index++;
            }
            target = targets[0];
        }

        if (card.model.spells.HasFlag(SPELLS.RANDOM_ENEMY))
        {
            CardController[] enemyCards = gameManager.GetEnemyFieldCards(card.model.isPlayerCard);
            target = enemyCards[UnityEngine.Random.Range(0, enemyCards.Length)];
        }
        else if (card.model.spells.HasFlag(SPELLS.RANDOM_FRIEND))
        {
            CardController[] friendCards = gameManager.GetFriendFieldCards(card.model.isPlayerCard);
            target = friendCards[UnityEngine.Random.Range(0, friendCards.Length)];
        }
        //Debug.Log(targets[0]);
        //　ターゲット/それぞれのフィールド/それぞれのHeroのTransformが必要
        StartCoroutine(card.movement.MoveLeftSpell(card));
        //　スペル発動時に一回転して中央に移動したから右に回転しながら移動
        yield return new WaitForSeconds(0.9f);
        if (card.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARDS) || card.model.spells.HasFlag(SPELLS.HEAL_FRIEND_CARDS) || card.model.spells.HasFlag(SPELLS.DESTROY_ALL_FIELD_CARDS) ||
            card.model.spells.HasFlag(SPELLS.INCREASE_ENEMY_COST) || card.model.spells.HasFlag(SPELLS.DISCARD_ALL_ENEMY_HAND) ||
            card.model.spells.HasFlag(SPELLS.REDUCE_HAND_COST) || card.model.spells.HasFlag(SPELLS.DISCARD_ALL_FRIEND_HAND))
        {
            gameManager.ReduceManaCost(card.model.cost, card.model.isPlayerCard);
            for (int i = 0; i < targets.Length; i++)
            {
                card.spellEffect(targets[i], true);
            }
        }
        else
        {
            gameManager.ReduceManaCost(card.model.cost, card.model.isPlayerCard);
            card.spellEffect(target, true);
        }

        //card.UseSpellTo(target);//スペルエフェクト
    }

}
/*
 * RANDOM_DAMAGE　SEARCH_SPECIFIC_UNIT　SUMMON_SPECIFIC_UNIT
 * EFFECT_SELECTION_FRIEND　EFFECT_SELECTION_ENEMY
 * 
 * 
 * 敵のスペル使用時の挙動について
 * EFFECT_SELECTIONがあれば選択の処理にする
 * ランダムは記入のとおり
 * 
*/