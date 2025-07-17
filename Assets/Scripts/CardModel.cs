using JetBrains.Annotations;
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
    public Sprite icon;
    public ABILITY ability;
    public ABILITIES abilities;
    public SPELLS spells;
    public CardModel[] targetCards;

    public bool isAlive;
    public bool canAttack;
    public bool isFieldCard;
    public bool isPlayerCard;
    public bool isDamageNullifyOnce;
    public bool isDoubleAction;
    public bool isSingleAction;

    public CardModel(int cardID, bool isPlayerCard)
    {
        CardEntity cardEntity = Resources.Load<CardEntity>("CardEntityList/Card" + cardID);
        no = cardEntity.no;
        name = cardEntity.name;
        hp = cardEntity.hp;
        at = cardEntity.at;
        effectDmg = cardEntity.effectDmg;
        effectHeal = cardEntity.effectHeal;
        cost = cardEntity.cost;
        icon = cardEntity.icon;
        ability = cardEntity.ability;
        abilities = cardEntity.abilities;
        spells = cardEntity.spells;

        /*        if (cardEntity.targetCardID.Length > 0)
                {
                    targetCards = new CardModel[cardEntity.targetCardID.Length];
                    for (int i = 0; i < cardEntity.targetCardID.Length; i++)
                    {
                        CardEntity targetEntity = Resources.Load<CardEntity>("TargetEntityList/Card" + cardEntity.targetCardID[i]);
                        targetCards[i].no = targetEntity.no
                        targetCards[i].name = targetEntity.name;
                        targetCards[i].hp = targetEntity.hp;
                        targetCards[i].at = targetEntity.at;
                        targetCards[i].effectDmg = targetEntity.effectDmg;
                        targetCards[i].effectHeal = targetEntity.effectHeal;
                        targetCards[i].cost = targetEntity.cost;
                        targetCards[i].icon = targetEntity.icon;
                        targetCards[i].ability = targetEntity.ability;
                        targetCards[i].abilities = targetEntity.abilities;
                        targetCards[i].spells = targetEntity.spells;
                        targetCards[i].spell = targetEntity.spell;
                    }
                }*/

        isAlive = true;
        this.isPlayerCard = isPlayerCard;
    }//効果召喚用のフォルダからmodelに代入するようにする

    void Damage(int damage)
    {
        if(isDamageNullifyOnce)
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
        RecoveryHP(effectHeal);
    }

    public void Attack(CardController card)
    {
        card.model.Damage(at);
    }

    public void Destroy(CardController card)
    {
        card.model.Damage(card.model.hp);
    }

    public void Steal(CardController target)
    {
        Transform fieldTransform = GameManager.instance.GetFriendFieldTransform(!target.model.isPlayerCard);
        target.model.isPlayerCard = !target.model.isPlayerCard;
        target.transform.SetParent(fieldTransform);
    }

    public void ReduceHandCost(int point)
    {
        cost -= point;
        if (cost - point < 0)
        {
            cost = 0;
        }
    }

    public void IncreaseEnemyHandCost(int point)
    {
        cost += point;
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
