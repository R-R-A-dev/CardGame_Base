using DG.Tweening;
using UnityEngine;
using TMPro;

public class MapStatusUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI deckCountText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI currentNodeText;

    [Header("回復演出")]
    [SerializeField] private Color healColor = new Color32(0x5B, 0xE3, 0x7D, 0xFF); // 対戦画面の回復表示と同じ緑
    [SerializeField] private Color hpMaxColor = new Color32(0xB2, 0xF0, 0xF7, 0xFF); // HP満タンで回復しなかった時の「HP MAX」の色
    [SerializeField] private Vector2 healPopupOffsetFromText = new Vector2(12f, 0f); // HPの文字列の右端からのずらし量(px)
    [SerializeField] private float healPopupRise = 30f;                              // フェードしながら上がる量(px)
    [SerializeField] private float healPopupHoldDuration = 0.8f;                     // 表示後、消え始めるまで止まって見せる時間
    [SerializeField] private float healPopupFadeDuration = 0.5f;                     // 上がりながら消える時間
    [SerializeField] private float hpFlashHoldDuration = 0.3f;                       // HPを緑のまま保つ時間
    [SerializeField] private float hpFlashReturnDuration = 0.4f;                     // 元の色へ戻る時間

    private TextMeshProUGUI healPopupText;
    private Color hpBaseColor;
    private bool isHpBaseColorInitialized;
    private Sequence hpFlashSeq;
    private Sequence healPopupSeq;

    public void Refresh(RoguelikeGameState state)
    {
        hpText.text = $"HP: {state.CurrentHP} / {state.MaxHP}";
        deckCountText.text = $"デッキ: {state.CurrentDeck.Count}枚";
        goldText.text = $"G: {state.Gold}";
        currentNodeText.text = state.CurrentNode != null
            ? $"現在地: {state.CurrentNode.nodeId}"
            : "現在地: スタート";
    }

    /// <summary>
    /// 回復後のHPを表示し、「HP +○」のポップとHP表示の緑点滅で回復したことを伝える。
    /// HP満タンで回復しなかった時は「HP MAX」をポップさせる
    /// </summary>
    public void PlayHealEffect(int healAmount, RoguelikeGameState state)
    {
        Refresh(state);

        if (healAmount > 0)
        {
            PlayHpFlash();
            PlayHealPopup($"HP +{healAmount}", healColor);
        }
        else if (state.CurrentHP >= state.MaxHP)
        {
            // 満タンで回復できなかったことが分かるよう、回復とは別の色で知らせる
            PlayHealPopup("HP MAX", hpMaxColor);
        }
    }

    private void PlayHpFlash()
    {
        // 点滅中に元の色を取ると緑を「元の色」として覚えてしまうため、最初の1回だけ取得する
        if (!isHpBaseColorInitialized)
        {
            hpBaseColor = hpText.color;
            isHpBaseColorInitialized = true;
        }

        hpFlashSeq?.Kill();
        hpText.color = healColor;
        hpFlashSeq = DOTween.Sequence()
            .AppendInterval(hpFlashHoldDuration)
            .Append(DOTween.To(() => hpText.color, c => hpText.color = c, hpBaseColor, hpFlashReturnDuration))
            .SetLink(gameObject);
    }

    // 対戦画面のダメージ表示(GameManager.GenFloatingText)と同じ動き。
    // マップ画面では読み取る余裕を持たせるため、消え始める前に止まって見せる時間を入れている
    private void PlayHealPopup(string label, Color color)
    {
        TextMeshProUGUI popup = GetHealPopupText();
        RectTransform rect = popup.rectTransform;

        healPopupSeq?.Kill();

        // ポップはHPTextの子なので、HPTextのローカル座標で文字列の右端に合わせる。
        // Refresh直後は文字列の大きさが未計算のため、メッシュを更新してから右端を取る
        hpText.ForceMeshUpdate();
        Bounds textBounds = hpText.textBounds;
        Vector3 startLocalPos = new Vector3(textBounds.max.x, textBounds.center.y, 0f) + (Vector3)healPopupOffsetFromText;
        rect.localPosition = startLocalPos;
        rect.localScale = Vector3.zero;
        popup.text = label;
        popup.color = color;
        popup.alpha = 1f;
        popup.gameObject.SetActive(true);

        healPopupSeq = DOTween.Sequence()
            .Append(rect.DOScale(1.5f, 0.2f).SetEase(Ease.OutBack))
            .Append(rect.DOScale(1.0f, 0.1f))
            .AppendInterval(healPopupHoldDuration)
            .Append(rect.DOLocalMoveY(startLocalPos.y + healPopupRise, healPopupFadeDuration))
            .Join(DOTween.To(() => popup.alpha, a => popup.alpha = a, 0f, healPopupFadeDuration))
            .OnComplete(() => popup.gameObject.SetActive(false))
            .SetLink(gameObject);
    }

    // シーンに専用オブジェクトを置かずに済むよう、HPTextを複製してフォントや大きさを揃える
    private TextMeshProUGUI GetHealPopupText()
    {
        if (healPopupText != null) return healPopupText;

        // StatusPanelの子にするとVerticalLayoutGroupに1行として並べられてしまうため、
        // HPTextの子にしてレイアウトの対象から外す
        healPopupText = Instantiate(hpText, hpText.transform);
        healPopupText.name = "HealPopupText";
        healPopupText.raycastTarget = false;

        // 文字列の右端を基準に右へ伸びるよう、左端・上下中央を基準点にする
        RectTransform rect = healPopupText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = hpText.rectTransform.rect.size;
        healPopupText.alignment = TextAlignmentOptions.MidlineLeft;

        healPopupText.gameObject.SetActive(false);
        return healPopupText;
    }

    // マップ切り替えで非表示になった時に、演出途中の色や表示が残らないよう戻しておく
    private void OnDisable()
    {
        hpFlashSeq?.Kill();
        healPopupSeq?.Kill();

        if (isHpBaseColorInitialized) hpText.color = hpBaseColor;
        if (healPopupText != null) healPopupText.gameObject.SetActive(false);
    }
}
