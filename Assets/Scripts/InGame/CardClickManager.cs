using DG.Tweening;
using DG.Tweening.Core.Easing;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

public class CardClickManager : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IEndDragHandler
{

    public bool isClickable;
    CardController dropped;
    CardController selectedCard;
    CardController droppedCard;
    CardController clickedCard;
    private void Update()
    {
        if (dropped == null) return;
        if (GameManager.instance.isEffectSelectPhase && !GameManager.instance.isOnCard && Input.GetMouseButtonDown(0))
        {
            dropped.movement.moveTween.Kill();
            dropped.view.SetActiveSelectablePanel(false);
            dropped.model.isFieldCard = false;
            dropped.RefreshShieldPanel();
            dropped.movement.PlayerSelectMoveOff(dropped);
            CancelSelect();
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CardController descriptionCard = GetComponent<CardController>();
            //敵の手札のカードは説明表示できない
            if (!descriptionCard.model.isPlayerCard && !descriptionCard.model.isFieldCard)
                return;

            GameManager.instance.showDescriptionClicked = true;

            GameManager.instance.uiManager.ShowDescriptionPanel(descriptionCard);
            BattleAudioManager.Instance.PlaySE("CursorSet");
            //GameManager.instance.uiManager.CloseDescriptionPanel();
        }

        if (!GameManager.instance.isEffectSelectPhase) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            selectedCard = GetComponent<CardController>();
            droppedCard = DropPlace.droppedCard;
            clickedCard = eventData.pointerClick.GetComponent<CardController>();

            // 敵フォロワー1体を選んでダメージを与えるスペルは、相手の場に守護がいるとき守護しか選べない。
            // 選び直せるように、キャンセル（手札に戻す）はせず選択フェーズを続ける。
            if (droppedCard.IsSpell &&
                droppedCard.model.spells.HasFlag(SPELLS.EFFECT_SELECTION_ENEMY) &&
                droppedCard.model.spells.HasFlag(SPELLS.DAMAGE_ENEMY_CARD) &&
                !clickedCard.model.isPlayerCard && clickedCard.model.isFieldCard &&
                GameManager.instance.IsBlockedByShield(selectedCard))
            {
                return;
            }

            if (!droppedCard.IsSpell)
            {
                if (!droppedCard.CanUseAbilities())
                {
                    CancelSelect();
                    return;
                }

                if (droppedCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND) && clickedCard.model.isPlayerCard && clickedCard.model.isFieldCard ||
                    droppedCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY) && !clickedCard.model.isPlayerCard && clickedCard.model.isFieldCard)
                {
                    //droppedCard.movement.isHand = false;
                    //droppedCard.movement.isDraggable = false;
                    StartCoroutine(SummonMove(droppedCard, selectedCard));
                    DropPlace.droppedCard = null;
                    CancelSelect();
                }
                else
                {
                    droppedCard.view.SetActiveSelectablePanel(false);
                    droppedCard.model.isFieldCard = false;
                    droppedCard.RefreshShieldPanel();
                    droppedCard.movement.PlayerSelectMoveOff(droppedCard);
                    CancelSelect();
                }
            }
            else if (droppedCard.model.spells.HasFlag(SPELLS.EFFECT_SELECTION_FRIEND) && clickedCard.model.isPlayerCard && clickedCard.model.isFieldCard ||
                    droppedCard.model.spells.HasFlag(SPELLS.EFFECT_SELECTION_ENEMY) && !clickedCard.model.isPlayerCard && clickedCard.model.isFieldCard)
            {
                //droppedCard.UseSpellTo(selectedCard);
                StartCoroutine(droppedCard.movement.UseSpellEffect(droppedCard));
                GameManager.instance.ReduceManaCost(droppedCard.model.cost, droppedCard.model.isPlayerCard);
                droppedCard.spellEffect(selectedCard, true);
                BattleAudioManager.Instance.PlaySE("Summon_Effect1");
                CancelSelect();
            }
            else
            {
                droppedCard.view.SetActiveSelectablePanel(false);
                droppedCard.model.isFieldCard = false;
                droppedCard.RefreshShieldPanel();
                droppedCard.movement.PlayerSelectMoveOff(droppedCard);
                CancelSelect();
            }
        }
    }

    public void TimeUpSelect()
    {
        if (DropPlace.droppedCard == null) return;
        dropped = DropPlace.droppedCard;
        dropped.view.SetActiveSelectablePanel(false);
        dropped.model.isFieldCard = false;
        dropped.RefreshShieldPanel();
        dropped.movement.PlayerSelectMoveOff(dropped);
        CancelSelect();
    }

    IEnumerator SummonMove(CardController droppedCard, CardController selectedCard)
    {
        StartCoroutine(droppedCard.movement.SelectedSummon(droppedCard, GameManager.instance.playerFieldTransform));
        yield return new WaitForSeconds(1f);
        Transform abilityEffect = droppedCard.effect.AbilityEffect(droppedCard.model.summonAbilityEffect, droppedCard.transform);
        droppedCard.effect.StartThrow(abilityEffect, 3f, droppedCard.transform.position, selectedCard.transform.position, 20f);
        DG.Tweening.Sequence seq = DOTween.Sequence();

        seq.Append(droppedCard.transform
            .DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd)
            .OnUpdate(() =>
            {
                float y = droppedCard.transform.localEulerAngles.y;
                // Unityでは-90度が270度として表現されることがあるので360でmod取る
                if (y >= 90 && y <= 270)
                {
                    droppedCard.view.maskPanel.SetActive(true);  // 裏面
                }
                else
                {
                    droppedCard.view.maskPanel.SetActive(false); // 表面
                }
            })
        );
        seq.Play();
        yield return new WaitForSeconds(0.3f);
        droppedCard.UseAbilitiesTo(selectedCard);
        droppedCard.hitEffect(selectedCard.transform);
    }

    void CancelSelect()
    {
        if (!droppedCard.IsSpell)
        {
            GameManager.instance.ReduceManaCost(-droppedCard.model.cost, droppedCard.model.isPlayerCard);
        }
        dropped = null;
        GameManager.instance.SelectingPanelOff();
        GameManager.instance.isEffectSelectPhase = false;
        GameManager.instance.EnableButtonCards();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GameManager.instance.isOnCard = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GameManager.instance.isOnCard = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        droppedCard = DropPlace.droppedCard;
        dropped = droppedCard;

    }

    /*暗転 カードの移動
     * 
     * 選択後にアビリティ発動してから移動
     * 他の場所選択でキャンセルして手札へ戻す
     * 
     * 
    */
}

