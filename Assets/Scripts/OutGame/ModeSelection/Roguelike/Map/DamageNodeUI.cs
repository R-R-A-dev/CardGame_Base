using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DamageNodeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);
    }

    public void Open(DamageNodeData data, RoguelikeGameState state)
    {
        gameObject.SetActive(true);

        int damage = data.isPercentageDamage
            ? Mathf.FloorToInt(state.MaxHP * data.damagePercentage / 100f)
            : data.damageAmount;

        // ダメージ適用
        state.CurrentHP = Mathf.Max(0, state.CurrentHP - damage);

        damageText.text = $"HP -{damage}";
        descriptionText.text = data.description;

        // HP0でゲームオーバー
        if (state.CurrentHP <= 0)
        {
            Close();
            RoguelikeManager.Instance.OnGameOver();
        }
    }

    private void Close()
    {
        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }
}