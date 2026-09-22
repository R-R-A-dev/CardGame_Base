using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI atText;
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] Image iconImage;
    [SerializeField] GameObject selectablePanel;
    [SerializeField] GameObject oneceNull;
    [SerializeField] GameObject shieldPanel;
    [SerializeField] public GameObject maskPanel;
    [SerializeField] CanvasGroup canvasGroup;

    public void SetCard(CardModel cardModel)
    {
        nameText.text = cardModel.name;
        hpText.text = cardModel.hp.ToString();
        atText.text = cardModel.at.ToString();
        costText.text = cardModel.cost.ToString();
        iconImage.sprite = cardModel.icon;
        //maskPanel.SetActive(!cardModel.isPlayerCard);

        if (cardModel.isPlayerCard)
        {
            maskPanel.SetActive(false);
        }
        else
        {
            //maskPanel.SetActive(true);
        }
        RefreshShieldPanel(cardModel);
        RefreshOneceNull(cardModel);
        // スペルは攻撃力・体力を持たないので非表示にする
        CardStatsDisplay.SetActiveStatsText(cardModel.spells, atText, hpText);
    }

    public void Show()
    {
        maskPanel.SetActive(false);
    }

    public void Refresh(CardModel cardModel)
    {
        hpText.text = cardModel.hp.ToString();
        atText.text = cardModel.at.ToString();
        costText.text = cardModel.cost.ToString();
        // ダメージ無効はCardModel.Damage()内で被弾時に消費される。
        // ダメージ適用後は必ずRefreshView()が呼ばれるので、ここで表示も落とす。
        RefreshOneceNull(cardModel);
    }

    public void SetActiveSelectablePanel(bool flag)
    {
        selectablePanel.SetActive(flag);
    }

    /// <summary>
    /// 守護の表示更新。場に出ているカードのみオーラを表示する。
    /// </summary>
    public void RefreshShieldPanel(CardModel cardModel)
    {
        shieldPanel.SetActive(cardModel.isFieldCard && cardModel.abilities.HasFlag(ABILITIES.SHIELD));
    }

    /// <summary>
    /// ダメージ無効（一度だけ）の表示更新。
    /// 場に出ていて、かつまだ消費していないカードだけ表示する。
    /// 2Pickの選択画面やカード一覧など、oneceNullを持たないCardViewもあるのでnullチェックする。
    /// </summary>
    public void RefreshOneceNull(CardModel cardModel)
    {
        if (oneceNull == null) return;
        oneceNull.SetActive(cardModel.isFieldCard && cardModel.isDamageNullifyOnce);
    }

    /// <summary>
    /// 場以外（2Pickの選択画面など）で守護表示を明示的に切り替えたい場合に使う。
    /// </summary>
    public void SetActiveShieldPanel(bool flag)
    {
        shieldPanel.SetActive(flag);
    }

    public void HideCard()
    {
        canvasGroup.DOFade(0f, 0.2f).OnComplete(() => {
            
        });
    }

    public void ShowCard()
    {
        canvasGroup.DOFade(1f, 0.2f).OnComplete(() => {

        });
    }
}
