using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardInfoDisplay : MonoBehaviour, IPointerClickHandler
{
    //カード情報パネル
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private CardController cardController;
    [SerializeField] private Button closeInfoButton;

    [SerializeField] private Text cardNameText;
    [SerializeField] private TMPro.TextMeshProUGUI cardAttackText;
    [SerializeField] private TMPro.TextMeshProUGUI cardHealthText;
    [SerializeField] private Text cardDescriptionText;



    /// <summary>
    /// TwoPickSelectPanel配下のカード（ピック中のカード・一覧のカード）を左クリックすると詳細を開く。
    /// カードには枚数表示など当たり判定を持つ子があるため、当たった位置から親をたどってカードを特定する。
    /// 押した瞬間ではなくクリック成立時に開くので、一覧をドラッグしてスクロールしても開かない。
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        if (clickedObject == null) return;

        CardController card = clickedObject.GetComponentInParent<CardController>();
        if (card == null || card.model == null) return;

        cardInfoPanel.SetActive(true);
        cardController.Init(card.model.no, false);
        cardNameText.text = card.model.name;
        cardAttackText.text = card.model.at.ToString();
        cardHealthText.text = card.model.hp.ToString();
        cardDescriptionText.text = card.model.description;
        // スペルは攻撃力・体力を持たないので非表示にする
        CardStatsDisplay.SetActiveStatsText(card.model.spells, cardAttackText, cardHealthText);
    }

    public void ClosePannel()
    {
        cardInfoPanel.SetActive(false);
    }
}
//クリックして表示する仕組み
//別のクラス作る