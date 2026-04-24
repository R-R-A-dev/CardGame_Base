using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RestUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healAmountText;
    [SerializeField] private Button restButton;
    [SerializeField] private Button upgradeButton; // canUpgradeCard=trueの時のみ有効

    private RoguelikeGameState gameState;
    private RestData restData;

    public void Open(RestData data, RoguelikeGameState state)
    {
        restData = data;
        gameState = state;
        gameObject.SetActive(true);

        // 回復量表示
        int healAmount = data.isPercentageHeal
            ? Mathf.FloorToInt(state.MaxHP * data.healPercentage / 100f)
            : data.healAmount;
        healAmountText.text = $"HP {healAmount} 回復";

        // カード強化ボタンの表示切替
        upgradeButton.gameObject.SetActive(data.canUpgradeCard);
    }

    // 休憩ボタン押下
    public void OnRestButtonClick()
    {
        int heal = restData.isPercentageHeal
            ? Mathf.FloorToInt(gameState.MaxHP * restData.healPercentage / 100f)
            : restData.healAmount;

        gameState.CurrentHP = Mathf.Min(gameState.MaxHP, gameState.CurrentHP + heal);
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }
}
