using DG.Tweening;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using DG.Tweening;
using UnityEngine.UI;
using Unity.Jobs;

public class CardMovement : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public Transform defaultParent;
    public bool isHand = true;
    public bool isDraggable;
    public CardController draggCard;
    int handSiblingIndex = 0;


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!GameManager.instance.isPlayerTurn) return;
        if (GameManager.instance.isSummoning) return;
        if (GameManager.instance.isEffectSelectPhase) return;

        //　カードのコストとPlayerのManaコストを比較して、ドラッグ可能かどうかを判断
        CardController card = GetComponent<CardController>();
        //　アビリティで場に出したときにドラッグの線が出る
        if (card.model.isPlayerCard && GameManager.instance.isPlayerTurn && !card.model.isFieldCard && card.model.cost <= GameManager.instance.player.manaCost)
        {
            isDraggable = true;
        }
        else if (card.model.isPlayerCard && GameManager.instance.isPlayerTurn && card.model.isFieldCard && card.model.canAttack)
        {
            isDraggable = false;
            draggCard = card;
            isHand = false;
        }
        else
        {
            isDraggable = false;
        }
        if (!isHand && card.model.canAttack)
        {
            BezierArrows.Instance.Show();
            return;
        }
        if (!isDraggable)
        {
            return;
        }

        defaultParent = transform.parent;
        handSiblingIndex = transform.GetSiblingIndex();
        transform.SetParent(defaultParent.parent);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!GameManager.instance.isPlayerTurn)
        {
            if (GameManager.instance.isSummoning) return;
            if (GameManager.instance.isEffectSelectPhase) return;
           
            // ドラッグ中にプレイヤーのターンでない場合は元の手札に戻る
            if (isDraggable)
            {
                isDraggable = false;
                defaultParent = GameManager.instance.playerHandTransform;
                transform.SetParent(defaultParent, false);
                transform.SetSiblingIndex(handSiblingIndex);
                GetComponent<CanvasGroup>().blocksRaycasts = true;
            }
            BezierArrows.Instance.Hide();
            return;
        }
        if (!isHand)
        {
            BezierArrows.Instance.SetOriginPos(transform.position);
            BezierArrows.Instance.SetTopPos(Input.mousePosition);
            return;
        }
        if (!isDraggable)
        {
            return;
        }
        transform.position = eventData.position;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!GameManager.instance.isPlayerTurn) return;
        if (GameManager.instance.isSummoning) return;
        if (GameManager.instance.isEffectSelectPhase) return;
        if (!isDraggable)
        {
            BezierArrows.Instance.Hide();
            return;
        }
        DropPlace dropPlace = eventData.pointerEnter?.GetComponent<DropPlace>();
        if (eventData.pointerEnter != null && dropPlace != null)
        {
            //ドロップ先がDropPlaceコンポーネントを持ち
            //　そのタイプがHANDの場合HANDに戻す
            if (dropPlace.type != DropPlace.TYPE.FIELD)
            {
                transform.SetParent(defaultParent, false);
                transform.SetSiblingIndex(handSiblingIndex);
            }
            // ドロップ先がDropPlaceコンポーネントを持ち、タイプがFIELDの場合
            // 場に出すカードの場合はエフェクトを再生する
            CardController summonCard = GetComponent<CardController>();
            if (!summonCard.IsSpell && summonCard.model.summonEffect != null && eventData.pointerEnter.GetComponent<DropPlace>() != null)
            {
                if (dropPlace.type == DropPlace.TYPE.FIELD &&
                    !(summonCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND) || summonCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY)))
                {
                    isDraggable = false;
                    isHand = false;
                    StartCoroutine(SummonMove(summonCard, dropPlace.transform));
                }
            }else if (summonCard.IsSpell)
            {
                transform.SetParent(defaultParent, false);
                transform.SetSiblingIndex(handSiblingIndex);
            }
        }
        // ドロップ先がDropPlaceコンポーネントを持たない場合手札に戻す
        if ((eventData.pointerEnter != null || eventData.pointerEnter == null) && dropPlace == null)
        {
            transform.SetParent(defaultParent, false);
            transform.SetSiblingIndex(handSiblingIndex);
        }


        GetComponent<CanvasGroup>().blocksRaycasts = true;
    }

    public IEnumerator MoveToField(Transform field)
    {
        //　一度親をCanvasに変更する
        transform.SetParent(defaultParent.parent);
        //　DOTweenでカードをフィールドに移動
        transform.DOMove(field.position, 0.25f);
        yield return new WaitForSeconds(0.25f);
        defaultParent = field;
        transform.SetParent(defaultParent);
    }

    public void MoveCardToField(Transform field, CardController selectedCard)
    {
        defaultParent = field;
        transform.SetParent(defaultParent);
    }

    public IEnumerator MoveToTarget(Transform target)
    {
        //　現在の位置を保存
        Vector3 currentPosition = transform.position;
        int siblingIndex = transform.GetSiblingIndex();

        //　一度親をCanvasに変更する
        transform.SetParent(defaultParent.parent);
        //　DOTweenでカードをターゲットに移動
        transform.DOMove(target.position, 0.25f);
        yield return new WaitForSeconds(0.25f);
        //　元の位置に戻す
        transform.DOMove(currentPosition, 0.25f);
        yield return new WaitForSeconds(0.25f);
        if (this != null)
        {
            transform.SetParent(defaultParent);
            transform.SetSiblingIndex(siblingIndex);
        }
        transform.SetParent(defaultParent);
        transform.SetSiblingIndex(siblingIndex);
    }

    void Start()
    {
        BezierArrows.Instance.Hide();
        defaultParent = transform.parent;
    }

    public IEnumerator ExpandThisCard(Transform moveTarget)
    {
        yield return transform.DOMove(moveTarget.position, 0.5f).WaitForCompletion();
        transform.SetParent(moveTarget);
    }

    public IEnumerator SummonMove(CardController summonCard, Transform dropPlace)
    {
        GameManager.instance.isSummoning = true;
        //拡大しながら中央へ移動
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd);
        rectTransform.DOScale(2f, 0.3f);
        yield return rectTransform.DOAnchorPos(new Vector2(960, -540), 0.3f).WaitForCompletion();
        GameManager.instance.ScaleYSummonLightCenterOn();

        //光のエフェクトを表示しながら縮小　カードは非表示
        GameManager.instance.SummonLightCenterOn();
        summonCard.view.HideCard();
        GameManager.instance.ScaleYSummonLightFadeOut();
        yield return new WaitForSeconds(0.3f);
        summonCard.CardDisappearEffect(GameManager.instance.summonEffect, transform);

        //トレールを表示しながら出現場所へ移動
        ParticleSystem trail = GameManager.instance.SummonTrailOn();
        Transform trailTrans = summonCard.effect.SummonTrail(trail, transform);

        rectTransform.DOScale(1, 0f);
        Vector3 prevPos = transform.position;
        defaultParent = dropPlace;
        transform.SetParent(defaultParent, false);
        yield return null;
        Vector3 nextPos = transform.position;
        summonCard.effect.StartThrow(trailTrans, 3f, prevPos, nextPos, 20);
        yield return new WaitForSeconds(0.3f);
        //到着したらカードを表示
        summonCard.view.ShowCard();
        //カメラを揺らす
        GameManager.instance.cameraShake.StartShake(0.3f, 50f, 10, 0, false);
        //出現エフェクトを表示
        summonCard.summonEffect(summonCard.model.summonEffect, transform);
        GameManager.instance.isSummoning = false;
    }

    public IEnumerator PlayerSelectMoveOn()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.DOAnchorPos(new Vector2(160, -140), 0.2f);
        GameManager.instance.SelectingPanelOn();
        yield return null;
    }

    public void PlayerSelectMoveOff(CardController dropped)
    {
        defaultParent = GameManager.instance.playerHandTransform;
        dropped.transform.SetParent(defaultParent, false);
        dropped.transform.SetSiblingIndex(handSiblingIndex);
    }

    public IEnumerator SelectedSummon(CardController summonCard, Transform summonPlace)
    {
        GameManager.instance.isSummoning = true;
        GameManager.instance.SummonLightLeftOn();
        summonCard.view.HideCard();
        GameManager.instance.ScaleYSummonLightFadeOut();
        yield return new WaitForSeconds(0.3f);
        summonCard.CardDisappearEffect(GameManager.instance.summonEffect, transform);

        //トレールを表示しながら出現場所へ移動
        ParticleSystem trail = GameManager.instance.SummonTrailOn();
        Transform trailTrans = summonCard.effect.SummonTrail(trail, transform);

        Vector3 prevPos = transform.position;
        defaultParent = summonPlace;
        transform.SetParent(defaultParent, false);
        yield return null;
        Vector3 nextPos = transform.position;
        summonCard.effect.StartThrow(trailTrans, 3f, prevPos, nextPos, 20);
        yield return new WaitForSeconds(0.3f);
        //到着したらカードを表示
        summonCard.view.ShowCard();
        //カメラを揺らす
        GameManager.instance.cameraShake.StartShake(0.3f, 50f, 10, 0, false);
        //出現エフェクトを表示
        summonCard.summonEffect(summonCard.model.summonEffect, transform);
        GameManager.instance.isSummoning = false;
    }

    public IEnumerator MoveLeftSpell(CardController summonCard)
    {
        GameManager.instance.isSummoning = true;
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd);
        //rectTransform.DOScale(2f, 0.3f);
        yield return rectTransform.DOAnchorPos(new Vector2(160, -140), 0.3f).WaitForCompletion();
        GameManager.instance.ScaleYSummonLightLeftOn();

        //光のエフェクトを表示しながら縮小　カードは非表示
        GameManager.instance.SummonLightLeftOn();
        summonCard.view.HideCard();
        GameManager.instance.ScaleYSummonLightFadeOut();
        yield return new WaitForSeconds(0.3f);
        summonCard.CardDisappearEffect(GameManager.instance.summonEffect, transform);
        GameManager.instance.isSummoning = false;
    }

    public IEnumerator UseSpellEffect(CardController summonCard)
    {
        GameManager.instance.SummonLightLeftOn();
        GameManager.instance.ScaleYSummonLightLeftOn();
        GameManager.instance.ScaleYSummonLightFadeOut();
        summonCard.view.HideCard();
        yield return new WaitForSeconds(0.3f);
        summonCard.CardDisappearEffect(GameManager.instance.summonEffect, transform);
    }
}
