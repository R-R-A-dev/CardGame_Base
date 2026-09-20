using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaCardDetailPanel : MonoBehaviour
{
    [Header("カード情報パネル")]
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private CardController cardController;
    [SerializeField] private Button closeInfoButton;

    [SerializeField] private Text cardNameText;
    [SerializeField] private TextMeshProUGUI cardAttackText;
    [SerializeField] private TextMeshProUGUI cardHealthText;
    [SerializeField] private Text cardDescriptionText;

    private void Start()
    {
        closeInfoButton.onClick.RemoveAllListeners();
        closeInfoButton.onClick.AddListener(ClosePanel);
        cardInfoPanel.SetActive(false);
    }

    // 外部から直接呼んでもらう
    public void Open(CardController card)
    {
        cardInfoPanel.SetActive(true);
        cardController.Init(card.model.no, false);
        cardNameText.text = card.model.name;
        cardAttackText.text = card.model.at.ToString();
        cardHealthText.text = card.model.hp.ToString();
        cardDescriptionText.text = card.model.description;
        // スペルは攻撃力・体力を持たないので非表示にする
        CardStatsDisplay.SetActiveStatsText(card.model.spells, cardAttackText, cardHealthText);
    }

    public void ClosePanel()
    {
        cardInfoPanel.SetActive(false);
    }
}