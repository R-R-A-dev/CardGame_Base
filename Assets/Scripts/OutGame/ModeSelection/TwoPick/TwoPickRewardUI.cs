using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 2Pickで「やめる」を選択した際に表示される報酬パネル
public class TwoPickRewardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        if (closeButton == null)
        {
            Debug.LogError("TwoPickRewardUI: closeButtonがアサインされていません。", this);
            return;
        }

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    public void Open(int rewardGold)
    {
        gameObject.SetActive(true);

        GameDataHolder.Instance.AddGold(rewardGold);
        GameDataHolder.Instance.SaveToFile();

        if (goldText == null)
        {
            Debug.LogError("TwoPickRewardUI: goldTextがアサインされていません。", this);
            return;
        }

        goldText.text = $"G +{rewardGold} 獲得！";
    }

    private void OnCloseButtonClick()
    {
        gameObject.SetActive(false);
    }
}
