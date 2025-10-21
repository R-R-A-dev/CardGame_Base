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
        if (cardModel.abilities.HasFlag(ABILITIES.SHIELD))
        {
            shieldPanel.SetActive(true);
        }
        else
        {
            shieldPanel.SetActive(false);
        }
        if (cardModel.spells != SPELLS.NONE)
        {
            hpText.gameObject.SetActive(false);
            atText.gameObject.SetActive(false);
        }
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
    }

    public void SetActiveSelectablePanel(bool flag)
    {
        selectablePanel.SetActive(flag);
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
