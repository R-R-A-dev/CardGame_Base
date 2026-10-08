using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 詰みパズルをクリアした際に表示される報酬パネル
public class LethalPuzzleRewardUI : MonoBehaviour
{
    [SerializeField] private Text goldText;
    [SerializeField] private Button closeButton;
    [SerializeField] private int rewardGold = 100;

    private void Start()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    public void Open(int puzzleId)
    {
        gameObject.SetActive(true);

        GameDataHolder.Instance.AddGold(rewardGold);
        GameDataHolder.Instance.ClearLethalPuzzleStage(puzzleId);
        GameDataHolder.Instance.SaveToFile();

        goldText.text = $"G +{rewardGold} 獲得！";
    }

    private void OnCloseButtonClick()
    {
        gameObject.SetActive(false);
    }
}
