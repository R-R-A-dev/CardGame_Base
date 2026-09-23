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
        // droppedはこのカードのドラッグ終了時の値を保持し続ける。
        // 効果選択が成立した場合CancelSelect()はクリックされた側のインスタンスでしか走らないため、
        // 召喚した側のdroppedは自分を指したまま残る。
        // 現在選択待ちのカードと一致する時だけ処理し、過去の残留値で
        // 無関係なカードが場から手札へ引き戻されるのを防ぐ。
        if (dropped == null || DropPlace.droppedCard != dropped) return;
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

            // ATKダウン（CONDITIONAL_ENEMY_DEBUFF）はATKを1未満に下げられないため、
            // ATKが1以下の相手フォロワーは対象に選べない。
            // 選び直せるように、キャンセル（手札に戻す）はせず選択フェーズを続ける。
            if ((droppedCard.IsSpell
                    ? droppedCard.model.spells.HasFlag(SPELLS.CONDITIONAL_ENEMY_DEBUFF)
                    : droppedCard.model.abilities.HasFlag(ABILITIES.CONDITIONAL_ENEMY_DEBUFF)) &&
                !clickedCard.model.isPlayerCard && clickedCard.model.isFieldCard &&
                !CardController.CanAttackDebuff(clickedCard))
            {
                return;
            }

            if (!droppedCard.IsSpell)
            {
                if (!droppedCard.CanUseAbilities())
                {
                    // 選択中に盤面が変わって発動できなくなった場合。
                    // 手札へ戻さないとカードが選択位置に浮いたまま取り残されるので、
                    // 下のキャンセル分岐と同じ扱いにする。
                    droppedCard.view.SetActiveSelectablePanel(false);
                    droppedCard.model.isFieldCard = false;
                    droppedCard.RefreshShieldPanel();
                    droppedCard.movement.PlayerSelectMoveOff(droppedCard);
                    CancelSelect();
                    return;
                }

                if (droppedCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND) && clickedCard.model.isPlayerCard && clickedCard.model.isFieldCard ||
                    droppedCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY) && !clickedCard.model.isPlayerCard && clickedCard.model.isFieldCard)
                {
                    //droppedCard.movement.isHand = false;
                    //droppedCard.movement.isDraggable = false;
                    // このコルーチンは対象カードではなく召喚する側のカードで回す。
                    // クリックされた側（this）で回すと、効果で対象が破壊された時に
                    // ホストごと消えてHideCard()のままShowCard()に到達せず、
                    // 召喚したカードが透明のまま残るため。
                    droppedCard.StartCoroutine(SummonMove(droppedCard, selectedCard));
                    DropPlace.droppedCard = null;
                    // 効果が成立した場合はコストを払い戻さない
                    // （CancelSelect()を呼ぶと OnFiled() で払ったコストが戻り、
                    //   対象選択を持つフォロワーが実質0コストになっていた）
                    EndSelectPhase();
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
                droppedCard.StartCoroutine(droppedCard.movement.UseSpellEffect(droppedCard));
                GameManager.instance.ReduceManaCost(droppedCard.model.cost, droppedCard.model.isPlayerCard);
                droppedCard.spellEffect(selectedCard, true);
                BattleAudioManager.Instance.PlaySE("Summon_Effect1");
                DropPlace.droppedCard = null;
                EndSelectPhase();
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
        // 効果対象の選択待ち中のカードを手札へ戻すための処理なので、選択フェーズ中に限定する。
        // DropPlace.droppedCardはドラッグ終了時の早期returnで解放漏れすることがあり、
        // 無条件に実行すると「場に出ているだけの無関係なカード」まで
        // 手札へ引き戻し（＋CancelSelect()でコストを払い戻し）てしまうため。
        if (!GameManager.instance.isEffectSelectPhase) return;
        dropped = DropPlace.droppedCard;
        // CancelSelect()の払い戻しはdroppedCardを見るので、ここでも揃えておく
        // （このインスタンスのdroppedCardが未設定だと払い戻しされない／例外になる）
        droppedCard = dropped;
        dropped.view.SetActiveSelectablePanel(false);
        dropped.model.isFieldCard = false;
        dropped.RefreshShieldPanel();
        dropped.movement.PlayerSelectMoveOff(dropped);
        CancelSelect();
    }

    IEnumerator SummonMove(CardController droppedCard, CardController selectedCard)
    {
        // 召喚演出も召喚する側のカードで回す（対象が破壊されても止まらないように）
        droppedCard.movement.StartCoroutine(droppedCard.movement.SelectedSummon(droppedCard, GameManager.instance.playerFieldTransform));
        yield return new WaitForSeconds(1f);
        // 待っている間に対象が他の効果で破壊されている場合がある
        if (selectedCard == null) yield break;
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
        if (selectedCard == null) yield break;
        droppedCard.UseAbilitiesTo(selectedCard);
        droppedCard.hitEffect(selectedCard.transform);
    }

    /// <summary>
    /// 効果の対象選択を「取りやめて」フェーズを終える。払った分のコストを戻す。
    /// </summary>
    void CancelSelect()
    {
        if (droppedCard != null && !droppedCard.IsSpell)
        {
            GameManager.instance.ReduceManaCost(-droppedCard.model.cost, droppedCard.model.isPlayerCard);
        }
        EndSelectPhase();
    }

    /// <summary>
    /// 効果の対象選択を「成立させて」フェーズを終える。コストは払い戻さない。
    /// キャンセルと確定で同じCancelSelect()を呼んでいたため、成立時にもコストが
    /// 戻ってしまい、対象選択を持つフォロワーが実質0コストになっていた。
    /// </summary>
    void EndSelectPhase()
    {
        // 選択が終わった以上、静的な参照も残さない
        DropPlace.droppedCard = null;
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

