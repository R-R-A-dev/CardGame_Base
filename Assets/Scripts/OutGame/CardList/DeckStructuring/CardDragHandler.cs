using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler,
    IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    GameObject holdCard;
    GameObject clickedCard;
    private ScrollRect scrollRect;

    Transform dropParent;
    int dropSiblingIndex;

    public bool isDeck = false;

    bool isDrag = false;

    public bool dropSuccess = false;


    private void Update()
    {
        //右クリックでカード詳細を非表示
        if (Input.GetMouseButtonUp(1) && !DeckBuilderManager.Instance.cardDetailUI.isOnCard)
        {
            DeckBuilderManager.Instance.cardDetailUI.Hide();
        }

    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
/*            // ScrollRectをドラッグ対象に
            scrollRect = GetComponentInParent<ScrollRect>();
            eventData.pointerDrag = scrollRect.gameObject;
            EventSystem.current.SetSelectedGameObject(scrollRect.gameObject);

            // ScrollRect側でドラッグの初期化
            scrollRect.OnInitializePotentialDrag(eventData);
            scrollRect.OnBeginDrag(eventData);*/
            
            //

        }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            dropSuccess = false;
            DeckBuilderManager.Instance.cardDetailUI.Hide();
            if (!isDraggable()) return;
            holdCard = DeckBuilderManager.Instance.deckBuilderUI.GetCardPool();
            if (holdCard == null)
            {
                holdCard = Instantiate(gameObject, transform.root);
            }
            else if (holdCard != null)
            {
            }
            //クリックしたオブジェクトからholdCardに情報をコピー
            holdCard.GetComponent<OutGameCardList>().DragCardGen(GetComponent<OutGameCardList>());
            holdCard.GetComponent<CardDragHandler>().isDeck = isDeck;
            holdCard.SetActive(true);

            holdCard.GetComponent<OutGameCardList>().PanelOff();
            holdCard.GetComponent<CanvasGroup>().blocksRaycasts = false;
            //dropParent = transform.parent;
            //dropSiblingIndex = transform.GetSiblingIndex();
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            //scrollRect.OnDrag(eventData);
        }
        if (eventData.button == PointerEventData.InputButton.Right && holdCard != null)
        {
            holdCard.transform.position = eventData.position;
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            //scrollRect.OnEndDrag(eventData);
        }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (holdCard != null)
            {
                CanvasGroup cg = holdCard.GetComponent<CanvasGroup>();
                cg.blocksRaycasts = true;

                // Drop先をRaycastで確認
                var raycastResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, raycastResults);

                bool isOverValidDropZone = false;

                foreach (var result in raycastResults)
                {
                    //  DropZone にドロップされたらOK
                    if (result.gameObject.GetComponent<IDropHandler>() != null)
                    {
                        isOverValidDropZone = true;
                        break;
                    }

                    //  自分自身または自身の子にドロップしている場合 → 無効扱い
                    if (result.gameObject == gameObject || result.gameObject.transform.IsChildOf(transform))
                    {
                        isOverValidDropZone = false;
                        break;
                    }
                }

                // DropZoneで処理されなかった場合（自分の上など）→ 消す
                if (!isOverValidDropZone || !holdCard.GetComponent<CardDragHandler>().dropSuccess)
                {
                    DeckBuilderManager.Instance.deckBuilderUI.PoolCard(holdCard);
                }

                holdCard = null;
            }
        }
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OutGameCardList card = GetComponent<OutGameCardList>();
            if (card == null) return;

            DeckBuilderManager.Instance.cardDetailUI.ShowCardDetail(
                CardListData.Entities[card.No - 1]
            );
        }

    }

    /// <summary>
    /// ドラッグしたカードの取得
    /// </summary>
    /// <returns></returns>
    public GameObject GetHoldCard()
    {
        return holdCard;
    }


    bool isDraggable()
    {
        //所持一覧でoutgamecardlistのNoを取得してカードの枚数から取得可能か判定
        //CardListData.PossessionCard[cardNum - 1];
        int cardNo = GetComponent<OutGameCardList>().No;
        if (!isDeck && CardListData.PossessionCard[cardNo - 1] > 0)
            return true;
        else if (isDeck && CardListData.Decks[DeckBuilderManager.Instance.deckNum][cardNo - 1] > 0)
            return true;

        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        DeckBuilderManager.Instance.cardDetailUI.isOnCard = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DeckBuilderManager.Instance.cardDetailUI.isOnCard = false;
    }
}

/*追加事項
 * ドラッグアンドドラッグで追加と削除
 * 表示させるカードはオブジェクトプールを使用
 * クリックかつドラッグしたカードを生成
 * 既存のカードは重ねて枚数を右上に表示
 * 一覧からは基本的に消えない
 * デッキから一覧にドロップするときはデッキからなくして移動する
 * 
 * データ一覧と比較して枚数やデッキの使用状況を表示
 * 
 * 所持一覧のカードが0枚になったらドラッグできないようにする
 * ドラッグアンドドロップからリストへの追加と削除を行う
 * 
 *不具合
 * ドロップの受付場所
 * 
 * 
*/



