using DG.Tweening;
using System.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

public class CardMovement : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public Transform defaultParent;
    public bool isHand = true;
    public bool isDraggable;
    public CardController draggCard;
    int handSiblingIndex = 0;

    // 演出中のカードの移動先。
    // プレハブ上のアンカーは中央(0.5, 0.5)だが、手札のHorizontalLayoutGroupが
    // 子のanchorMin/anchorMaxをVector2.up (0, 1)＝左上へ書き換える（Unityの仕様）。
    // 手札を経由したカードは手札から外れた後も左上アンカーのままなので、
    // ここの座標は「共通の親（1920x1080）の左上隅からのオフセット」になる。
    // 中央アンカーだと勘違いして値を変えると画面外へ飛ぶので注意。
    //
    // 画面中心を原点に直すとGameManagerの光の位置と一致する（対で管理すること）
    //   SummonCenterPos (960, -540) → 中心から ( 0,   0) ↔ SummonLightCenterOn() の (0, 0, 0)
    //   SpellLeftPos    (160, -140) → 中心から (-800, 400) ↔ SummonLightLeftOn()  の (-800, 400, 0)
    static readonly Vector2 SummonCenterPos = new Vector2(960, -540);
    static readonly Vector2 SpellLeftPos = new Vector2(160, -140);

    /// <summary>
    /// 演出を始める前に、親を共通の親（フィールドの親＝画面中心が原点のオブジェクト）へ揃える。
    /// 上の移動先座標はこの親を基準に書かれているため、親が違うと移動先がずれる。
    /// プレイヤーはOnBeginDrag()で既に手札から外れているが、AIの経路では手札の下にいるままで、
    /// 手札のLayoutGroupに位置を奪われて演出が破綻する。
    /// </summary>
    void MoveToEffectRoot()
    {
        Transform effectRoot = GameManager.instance.playerFieldTransform.parent;
        if (effectRoot == null || transform.parent == effectRoot) return;
        // 見た目の位置は変えずに親だけ付け替える
        transform.SetParent(effectRoot, true);
    }

    /// <summary>
    /// OnBeginDrag()で落とした自分の場のカードのレイキャストを戻す。
    /// DropPlace.OnDrop()でも戻しているが、フィールド以外（敵フィールド・ヒーロー・
    /// 何もない場所・他の手札カードの上）にドロップするとそこを通らず、
    /// 場のカードが攻撃ドラッグも説明表示もできないまま取り残されるため、
    /// ドラッグ終了時に必ず戻す。
    /// </summary>
    void RestoreFieldCardsRaycast()
    {
        foreach (CardController fieldCard in GameManager.instance.GetFriendFieldCards(true))
        {
            CanvasGroup canvasGroup = fieldCard.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        }
    }


    public void OnBeginDrag(PointerEventData eventData)
    {

        //右クリックはreturn
        if (eventData.button == PointerEventData.InputButton.Right) return;

        if (!GameManager.instance.isPlayerTurn) return;
        if (GameManager.instance.isSummoning) return;
        if (GameManager.instance.isAttacking) return;
        if (GameManager.instance.isEffectSelectPhase) return;
        if (GameManager.instance.player.heroHp <= 0 || GameManager.instance.enemy.heroHp <= 0) return;
        if (GameManager.instance.GetFriendFieldCards(true).Length > 4 && GetComponent<CardController>().model.spells == SPELLS.NONE
            && !GetComponent<CardController>().model.isFieldCard) return;

        //　カードのコストとPlayerのManaコストを比較して、ドラッグ可能かどうかを判断
        CardController card = GetComponent<CardController>();
        //　アビリティで場に出したときにドラッグの線が出る
        if (card.model.isPlayerCard && GameManager.instance.isPlayerTurn && !card.model.isFieldCard && card.model.cost <= GameManager.instance.player.manaCost)
        {
            isDraggable = true;
            if (!card.model.isFieldCard && !card.movement.isHand)
            {
                //isDraggable = false;
                //card.model.isFieldCard = true;
            }

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
        // 効果対象の選択中はこのメソッド自体が冒頭でreturnしているため、
        // ここに残っている値は前のドラッグの解放漏れ。必ず上書きして居座らせない
        // （居座るとChangeTurn()のTimeUpSelect()が毎ターンそのカードを手札へ引き戻す）。
        DropPlace.droppedCard = card;

        CardController[] cards = GameManager.instance.GetFriendFieldCards(true);
        if (cards.Length != 0)
        {
            foreach (CardController c in cards)
            {
                c.GetComponent<CanvasGroup>().blocksRaycasts = false;
            }
        }
        BattleAudioManager.Instance.PlaySE("CardCatch");
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
            if (GameManager.instance.player.heroHp <= 0 || GameManager.instance.enemy.heroHp <= 0) return;
            if (GameManager.instance.GetFriendFieldCards(true).Length > 4 && GetComponent<CardController>().model.spells == SPELLS.NONE
                && !GetComponent<CardController>().model.isFieldCard) return;
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
        if (eventData.button == PointerEventData.InputButton.Right) return;

        transform.position = eventData.position;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) return;

        // このカードのドラッグが終わる以上、効果対象の選択待ちに入った場合を除いて
        // DropPlace.droppedCardはここで必ず解放する。
        // 以降の早期returnを通ると末尾の解放処理まで到達できず、残った値を
        // ChangeTurn()のTimeUpSelect()が拾って無関係なカードを手札へ引き戻してしまうため。
        CardController endDragCard = GetComponent<CardController>();
        if (!GameManager.instance.isEffectSelectPhase && DropPlace.droppedCard == endDragCard)
        {
            DropPlace.droppedCard = null;
        }
        // ドロップ先に関係なく、場のカードのレイキャストもここで必ず戻す。
        // ドロップの判定（OnDrop）は既に終わっているので、戻しても拾い先は変わらない。
        RestoreFieldCardsRaycast();

        if (!GameManager.instance.isPlayerTurn) return;
        if (GameManager.instance.isSummoning)
        {
            // 他のカードの召喚演出中にドロップした場合、DropPlace.OnDrop()もSpellDropManager.OnDrop()も
            // 同じ条件でreturnしており誰もこのカードを引き受けていない。
            // ドラッグ開始時にShakeObject直下へ移してあるので、宙に浮いたままにならないよう手札へ戻す。
            // 逆に引き受け済みのカードは触らない：
            //   ・フォロワーの召喚      → OnFiled()でisFieldCardがtrueになっている
            //   ・スペルの使用          → SpellDropManager.OnDrop()でisDraggableがfalseにされている
            //     （MoveLeftSpell()が同期でisSummoningをtrueにするため、使用したスペル自身も
            //       この分岐に入ってくる。ここで手札へ戻すと移動演出が壊れる）
            if (isDraggable && !endDragCard.model.isFieldCard)
            {
                transform.SetParent(defaultParent, false);
                transform.SetSiblingIndex(handSiblingIndex);
                GetComponent<CanvasGroup>().blocksRaycasts = true;
            }
            return;
        }
        if (GameManager.instance.isEffectSelectPhase) return;
        if (GameManager.instance.player.heroHp <= 0 || GameManager.instance.enemy.heroHp <= 0) return;
        if (GameManager.instance.GetFriendFieldCards(true).Length > 4 && GetComponent<CardController>().model.spells == SPELLS.NONE
            && !GetComponent<CardController>().model.isFieldCard) return;
        if (!isDraggable)
        {
            BezierArrows.Instance.Hide();
            return;
        }
        // ドロップ先の判定は、DropPlace.OnDrop()と同じ「このフレームのレイキャスト結果」を使う。
        // eventData.pointerEnterはProcessMove()で更新される＝1フレーム前の値であり、
        // OnDrop側が使うcurrentOverGo（このフレームの値）とは、動かしながら離すとズレる。
        // 親をたどるのは、OnDropがExecuteHierarchy()で親方向に探されるのに合わせるため。
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
        DropPlace dropPlace = dropTarget != null ? dropTarget.GetComponentInParent<DropPlace>() : null;
        CardController summonCard = GetComponent<CardController>();

        // 場に出すのは、DropPlace.OnDrop()がこのカードを受け付けた場合に限る。
        // 受け付けの証拠はOnFiled()が立てるisFieldCard。
        // 以前はこことOnDropが別々にドロップ先を判定していたため、判定がズレると
        // OnDrop（＝コストを払い、場に出た時の効果を発動する処理）を通らないまま
        // この下のSummonMove()だけが走り、カードがコストなしで場に出ていた。
        if (dropPlace != null && dropPlace.type == DropPlace.TYPE.FIELD && summonCard.model.isFieldCard)
        {
            isDraggable = false;
            isHand = false;
            if (!summonCard.IsSpell && summonCard.model.summonEffect != null &&
                !(summonCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_FRIEND) || summonCard.model.abilities.HasFlag(ABILITIES.EFFECT_SELECTION_ENEMY)))
            {
                StartCoroutine(SummonMove(summonCard, dropPlace.transform));
            }
            else
            {
                // 召喚エフェクト未設定などで演出を出せない場合も、コストは払っている以上
                // 場には置く（Canvas直下に浮いたままにしない）
                defaultParent = dropPlace.transform;
                transform.SetParent(defaultParent, false);
            }
        }
        else
        {
            // 場に出せなかった（手札側・場以外・OnDropが受け付けなかった）ので手札へ戻す
            transform.SetParent(defaultParent, false);
            transform.SetSiblingIndex(handSiblingIndex);
        }
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        if (DropPlace.droppedCard != null)
        {
            DropPlace.droppedCard = null;
        }
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
        MoveToEffectRoot();
        //拡大しながら中央へ移動
        RectTransform rectTransform = GetComponent<RectTransform>();
        DG.Tweening.Sequence seq = DOTween.Sequence();

        seq.Append(rectTransform
            .DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd)
            .OnUpdate(() =>
            {
                float y = rectTransform.localEulerAngles.y;
                // Unityでは-90度が270度として表現されることがあるので360でmod取る
                if (y >= 90 && y <= 270)
                {
                    summonCard.view.maskPanel.SetActive(true);  // 裏面
                }
                else
                {
                    summonCard.view.maskPanel.SetActive(false); // 表面
                }
            })
        );
        seq.Play();
        rectTransform.DOScale(2f, 0.3f);
        yield return rectTransform.DOAnchorPos(SummonCenterPos, 0.3f).WaitForCompletion();
        GameManager.instance.ScaleYSummonLightCenterOn();

        //光のエフェクトを表示しながら縮小　カードは非表示
        GameManager.instance.SummonLightCenterOn();
        summonCard.view.HideCard();
        GameManager.instance.ScaleYSummonLightFadeOut();
        yield return new WaitForSeconds(0.3f);
        summonCard.CardDisappearEffect(GameManager.instance.summonEffect, transform);
        BattleAudioManager.Instance.PlaySE("Summon_Effect1");

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
        if (summonCard.model.abilities.HasFlag(ABILITIES.INIT_ATTACKABLE))
            summonCard.SetCanAttack(true);
        
        //到着したらカードを表示
        summonCard.view.ShowCard();
        //カメラを揺らす
        GameManager.instance.cameraShake.StartShake(0.3f, 50f, 10, 0, false);
        //出現エフェクトを表示
        summonCard.summonEffect(summonCard.model.summonEffect, transform);
        GameManager.instance.isSummoning = false;
        BattleAudioManager.Instance.PlaySE("Summon_Effect2");
    }
    public Tweener moveTween;
    public IEnumerator PlayerSelectMoveOn()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        moveTween = rectTransform.DOAnchorPos(SpellLeftPos, 0.2f);
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
        MoveToEffectRoot();
        RectTransform rectTransform = GetComponent<RectTransform>();
        DG.Tweening.Sequence seq = DOTween.Sequence();

        seq.Append(rectTransform
            .DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd)
            .OnUpdate(() =>
            {
                float y = rectTransform.localEulerAngles.y;
                // Unityでは-90度が270度として表現されることがあるので360でmod取る
                if (y >= 90 && y <= 270)
                {
                    summonCard.view.maskPanel.SetActive(true);  // 裏面
                }
                else
                {
                    summonCard.view.maskPanel.SetActive(false); // 表面
                }
            })
        );
        seq.Play();
        //rectTransform.DOScale(2f, 0.3f);
        yield return rectTransform.DOAnchorPos(SpellLeftPos, 0.3f).WaitForCompletion();
        GameManager.instance.ScaleYSummonLightLeftOn();

        //光のエフェクトを表示しながら縮小　カードは非表示
        GameManager.instance.SummonLightLeftOn();
        summonCard.view.HideCard();
        GameManager.instance.ScaleYSummonLightFadeOut();
        yield return new WaitForSeconds(0.3f);
        summonCard.CardDisappearEffect(GameManager.instance.summonEffect, transform);
        GameManager.instance.isSummoning = false;
        BattleAudioManager.Instance.PlaySE("Summon_Effect1");
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

    public void DrawEffect(CardController card)
    {
        GameManager.instance.isSummoning = true;
        DG.Tweening.Sequence seq = DOTween.Sequence();
        RectTransform rectTransform = GetComponent<RectTransform>();
        Transform handTransform;

        if (card.model.isPlayerCard)
        {
            handTransform = GameManager.instance.playerHandTransform;

            // プレイヤー用：回転演出
            seq.Join(rectTransform.DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.LocalAxisAdd));
            seq.Join(rectTransform.DOAnchorPos(Vector2.zero, 0.3f));
            seq.Join(rectTransform.DORotateQuaternion(Quaternion.Euler(0, rectTransform.localEulerAngles.y, 0), 0.3f));
        }
        else
        {
            seq.AppendInterval(1.3f);
            handTransform = GameManager.instance.enemyHandTransform;

            // 敵用：移動と回転を同時に実行
            Vector3 handPos = handTransform.position;

            // Appendで基準の移動Tweenを追加
            seq.Append(transform.DOMove(handPos, 0.3f).OnComplete(() =>
            {
                transform.SetParent(handTransform, false);
                seq.Join(rectTransform.DORotateQuaternion(Quaternion.Euler(0, rectTransform.localEulerAngles.y, 0), 0f));
            }));
            seq.AppendInterval(1f);
            seq.AppendCallback(() =>
            {
                GameManager.instance.isSummoning = false;
            });


            return;
        }

        // プレイヤー用処理の続き
        Vector3 playerHandPos = handTransform.position;

        seq.AppendInterval(1f);
        seq.Join(rectTransform.DORotateQuaternion(Quaternion.Euler(0, rectTransform.localEulerAngles.y, 0), 0.3f));
        seq.Append(transform.DOMove(playerHandPos, 0.3f).OnComplete(() =>
        {
            transform.SetParent(handTransform, false);
            GameManager.instance.isSummoning = false;
        }));
    }


}