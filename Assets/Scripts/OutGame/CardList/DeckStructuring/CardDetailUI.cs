using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardDetailUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] private GameObject detailPanel;

    public bool isOnCard = false;

    /// <summary>
    /// カード情報を表示
    /// </summary>
    public void ShowCardDetail(CardEntity entity)
    {
        if (entity == null) return;

        cardImage.sprite = entity.icon;
        nameText.text = entity.name;
        hpText.text = entity.hp.ToString();
        attackText.text = entity.at.ToString();
        costText.text = entity.cost.ToString();
        descriptionText.text = entity.description;

        detailPanel.SetActive(true);
    }

    /// <summary>
    /// カード詳細を閉じる
    /// </summary>
    public void Hide()
    {
        detailPanel.SetActive(false);
    }
}
