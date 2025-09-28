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
            dropped.view.SetActiveSelectablePanel(false);
            dropped.model.isFieldCard = false;
            dropped.movement.PlayerSelectMoveOff(dropped);
            CancelSelect();
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CardController descriptionCard = GetComponent<CardController>();
            GameManager.instance.showDescriptionClicked = true;

            GameManager.instance.uiManager.ShowDescriptionPanel(descriptionCard);

            //GameManager.instance.uiManager.CloseDescriptionPanel();
        }

        if (!GameManager.instance.isEffectSelectPhase) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            selectedCard = GetComponent<CardController>();
            droppedCard = DropPlace.droppedCard;
            clickedCard = eventData.pointerClick.GetComponent<CardController>();

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
                    //TODO: エラー
                    dropped.view.SetActiveSelectablePanel(false);
                    dropped.model.isFieldCard = false;
                    droppedCard.movement.PlayerSelectMoveOff(droppedCard);
                    CancelSelect();
                }
            }
            else if (droppedCard.model.spells.HasFlag(SPELLS.EFFECT_SELECTION_FRIEND) && clickedCard.model.isPlayerCard ||
                    droppedCard.model.spells.HasFlag(SPELLS.EFFECT_SELECTION_ENEMY) && !clickedCard.model.isPlayerCard)
            {
                //droppedCard.UseSpellTo(selectedCard);
                StartCoroutine(droppedCard.movement.UseSpellEffect(droppedCard));
                GameManager.instance.ReduceManaCost(droppedCard.model.cost, droppedCard.model.isPlayerCard);
                droppedCard.spellEffect(selectedCard, true);
                CancelSelect();
            }
            else
            {

            }
        }
    }

    public void TimeUpSelect()
    {
        if (dropped == null) return;
        dropped.view.SetActiveSelectablePanel(false);
        dropped.model.isFieldCard = false;
        dropped.movement.PlayerSelectMoveOff(dropped);
        CancelSelect();
    }

    IEnumerator SummonMove(CardController droppedCard, CardController selectedCard)
    {
        StartCoroutine(droppedCard.movement.SelectedSummon(droppedCard, GameManager.instance.playerFieldTransform));
        yield return new WaitForSeconds(1f);
        Transform abilityEffect = droppedCard.effect.AbilityEffect(droppedCard.model.summonAbilityEffect, droppedCard.transform);
        droppedCard.effect.StartThrow(abilityEffect, 3f, droppedCard.transform.position, selectedCard.transform.position, 20f);
        yield return new WaitForSeconds(0.3f);
        droppedCard.UseAbilitiesTo(selectedCard);
        droppedCard.hitEffect(selectedCard.transform);
    }

    void CancelSelect()
    {
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

