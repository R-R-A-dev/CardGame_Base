using DG.Tweening;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using DG.Tweening;

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
            // ドラッグ中にプレイヤーのターンでない場合は元の手札に戻る
            if (!isHand)
            {
                transform.SetParent(defaultParent, false);
                transform.SetSiblingIndex(handSiblingIndex);
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

        if (!isDraggable)
        {
            BezierArrows.Instance.Hide();
            return;
        }
/*        CardController summonCard;
        summonCard = eventData.pointerEnter.GetComponent<CardController>();*/

        if (eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<DropPlace>() != null)
        {
                        if (eventData.pointerEnter?.GetComponent<DropPlace>().type != DropPlace.TYPE.FIELD)
            {
                transform.SetParent(defaultParent, false);
                transform.SetSiblingIndex(handSiblingIndex);
            }
        }
        if (eventData.pointerEnter != null || eventData.pointerEnter == null)
        {
            transform.SetParent(defaultParent, false);
            transform.SetSiblingIndex(handSiblingIndex);
        }
        transform.SetParent(defaultParent, false);

        CardController summonCard = GetComponent<CardController>();
        if (!summonCard.IsSpell && summonCard.model.summonEffect != null)
        {
            summonCard.summonEffect(summonCard.model.summonEffect, transform);
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
}
