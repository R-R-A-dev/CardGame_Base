using DG.Tweening.Core.Easing;
using JetBrains.Annotations;
using System.Collections;
using UnityEngine;

public class CardModel
{
    public int no;
    public string name;
    public int hp;
    public int at;
    public int effectDmg;
    public int effectHeal;
    public int cost;
    public int price;
    public string description;
    public float attackTime = 0f;
    public Sprite icon;
    public ATTACKTYPE attackType;
    public RARE rare;
    public ABILITY ability;
    public ABILITIES abilities;
    public SPELLS spells;
    public AudioClip summonAudio;
    public AudioClip summonAbilityAudio;
    public AudioClip attackAudio;
    public AudioClip hitAudio;
    public AudioClip destroyAudio;
    public ParticleSystem summonEffect;
    public ParticleSystem summonAbilityEffect;
    public ParticleSystem attackEffect;
    public ParticleSystem hitEffect;
    public ParticleSystem destroyEffect;
    public CardModel[] targetCards;

    public bool isAlive;
    public bool canAttack;
    public bool isFieldCard;
    public bool isPlayerCard;
    public bool isDamageNullifyOnce;
    public bool isDoubleAction;
    public bool isSingleAction;
    public bool isStatsUpOnAttack;
    public bool isDestroyer;

    public CardModel(int cardID, bool isPlayerCard)
    {
        CardEntity cardEntity = Resources.Load<CardEntity>("CardEntityList/Card" + cardID);
        no = cardID;
        name = cardEntity.name;
        hp = cardEntity.hp;
        at = cardEntity.at;
        effectDmg = cardEntity.effectDmg;
        effectHeal = cardEntity.effectHeal;
        cost = cardEntity.cost;
        icon = cardEntity.icon;
        price = cardEntity.price;
        attackTime = cardEntity.attackTime;
        attackType = cardEntity.attackType;
        rare = cardEntity.rare;
        description = cardEntity.description;
        summonAudio = cardEntity.summonAudio;
        summonAbilityAudio = cardEntity.summonAbilityAudio;
        attackAudio = cardEntity.attackAudio;
        hitAudio = cardEntity.hitAudio;
        destroyAudio = cardEntity.destroyAudio;
        summonEffect = cardEntity.summonEffect;
        summonAbilityEffect = cardEntity.summonAbilityEffect;
        attackEffect = cardEntity.attackEffect;
        hitEffect = cardEntity.hitEffect;
        destroyEffect = cardEntity.destroyEffect;
        ability = cardEntity.ability;
        abilities = cardEntity.abilities;
        spells = cardEntity.spells;

        if (cardEntity.targetCardID.Length > 0)
        {
            targetCards = new CardModel[cardEntity.targetCardID.Length];
            for (int i = 0; i < cardEntity.targetCardID.Length; i++)
            {
                CardEntity targetEntity = Resources.Load<CardEntity>("TargetEntityList/Card" + cardEntity.targetCardID[i]);
                targetCards[i] = new CardModel();
                targetCards[i].no = cardEntity.targetCardID[i];
                targetCards[i].name = targetEntity.name;
                targetCards[i].hp = targetEntity.hp;
                targetCards[i].at = targetEntity.at;
                targetCards[i].effectDmg = targetEntity.effectDmg;
                targetCards[i].effectHeal = targetEntity.effectHeal;
                targetCards[i].cost = targetEntity.cost;
                targetCards[i].icon = targetEntity.icon;
                targetCards[i].attackTime = targetEntity.attackTime;
                targetCards[i].attackType = targetEntity.attackType;
                targetCards[i].rare = targetEntity.rare;
                targetCards[i].description = targetEntity.description;
                targetCards[i].summonAudio = targetEntity.summonAudio;
                targetCards[i].summonAbilityAudio = targetEntity.summonAbilityAudio;
                targetCards[i].attackAudio = targetEntity.attackAudio;
                targetCards[i].hitAudio = targetEntity.hitAudio;
                targetCards[i].destroyAudio = targetEntity.destroyAudio;
                targetCards[i].summonEffect = targetEntity.summonEffect;
                targetCards[i].summonAbilityEffect = targetEntity.summonAbilityEffect;
                targetCards[i].attackEffect = targetEntity.attackEffect;
                targetCards[i].hitEffect = targetEntity.hitEffect;
                targetCards[i].destroyEffect = targetEntity.destroyEffect;
                targetCards[i].ability = targetEntity.ability;
                targetCards[i].abilities = targetEntity.abilities;
                targetCards[i].spells = targetEntity.spells;
                targetCards[i].isPlayerCard = isPlayerCard;
                targetCards[i].isAlive = true;
            }
        }

        isAlive = true;
        this.isPlayerCard = isPlayerCard;
    }//効果召喚用のフォルダからmodelに代入するようにする

    public CardModel()
    {
    }

    void Damage(int damage)
    {
        if (isDamageNullifyOnce)
        {
            isDamageNullifyOnce = false;
            return;
        }
        hp -= damage;
        if (hp <= 0)
        {
            hp = 0;
            isAlive = false;
        }
    }

    public void EffectDmg(CardController card)
    {
        card.model.Damage(effectDmg);
    }

    public void EffectHeal(CardController card)
    {
        card.model.RecoveryHP(effectHeal);
    }

    public void Attack(CardController card)
    {
        card.model.Damage(at);
    }

    public void Destroy(CardController card)
    {
        card.model.Damage(card.model.hp);
    }

    public void CardToHand(CardModel[] cards, Transform hand, bool isPlayer)
    {
        foreach (CardModel card in cards)
        {
            card.isPlayerCard = isPlayer;
            GameManager.instance.EffectSearchCard(hand, card);
        }
    }

    public void SummonCard(CardModel[] cards, Transform hand, bool isPlayer, CardController baseCard)
    {
        foreach (CardModel card in cards)
        {
            card.isPlayerCard = isPlayer;
            GameManager.instance.EffectSummonCard(hand, card, baseCard);
            CardController[] friendCards = GameManager.instance.GetFriendFieldCards(isPlayer);
            if (friendCards.Length > 4)
            {
                break;
            }
        }
    }

    public void Steal(CardController target)
    {
        Transform fieldTransform = GameManager.instance.GetFriendFieldTransform(!target.model.isPlayerCard);
        target.model.isPlayerCard = !target.model.isPlayerCard;
        target.transform.SetParent(fieldTransform);
    }

    public void ReduceHandCost(CardController card)
    {
        card.model.cost -= effectDmg;
        if (card.model.cost < 0)
        {
            card.model.cost = 0;
        }
    }

    public void IncreaseEnemyHandCost(CardController card)
    {
        card.model.cost += effectDmg;
        if (card.model.cost >= 11)
        {
            card.model.cost = 10;
        }
    }

    void RecoveryHP(int point)
    {
        hp += point;
    }

    public void Heal(CardController card)
    {
        card.model.RecoveryHP(at);
    }
}
