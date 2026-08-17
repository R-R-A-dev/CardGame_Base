using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ローグライクのステージを最後までクリアした際に表示される報酬パネル
public class RoguelikeStageClearUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    public void Open(RoguelikeGameState state, string stageName)
    {
        gameObject.SetActive(true);

        // ランで貯めたゴールドを所持金に加算し、クリア状況を保存する
        int gold = state.Gold;
        GameDataHolder.Instance.AddGold(gold);
        GameDataHolder.Instance.ClearRoguelikeStage(stageName);
        GameDataHolder.Instance.SaveToFile();

        goldText.text = $"G +{gold} 獲得！";
    }

    private void OnCloseButtonClick()
    {
        gameObject.SetActive(false);
        RoguelikeManager.Instance.OnStageClearConfirmed();
    }
}

