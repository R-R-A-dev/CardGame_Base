using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardRewardPanel : MonoBehaviour
{
    [SerializeField] private Transform cardListParent;
    [SerializeField] private CardRewardItem cardItemPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI goldRewardText;    // 獲得ゴールド表示
    [SerializeField] private TextMeshProUGUI selectedCountText; // 選択枚数表示
    [SerializeField] private Button confirmButton;
    [SerializeField] private CardRewardDetailPanel detailPanel;
    [SerializeField] private CardRewardConfirmPanel confirmPanel;

    private RoguelikeGameState gameState;
    private List<CardRewardItem> spawnedItems = new List<CardRewardItem>();
    private List<int> selectedCardIds = new List<int>();
    private int maxSelectCount;

    public void Open(RewardData rewardData, RoguelikeGameState state)
    {
        gameState = state;
        maxSelectCount = rewardData.cardSelectCount;
        selectedCardIds.Clear();
        gameObject.SetActive(true);

        // お金を自動入手
        int goldAmount = 0;
        if (rewardData.hasGoldReward)
        {
            goldAmount = rewardData.isRandomGold
                ? Random.Range(rewardData.goldMin, rewardData.goldMax)
                : rewardData.goldFixed;

            gameState.Gold += goldAmount;
            goldRewardText.text = $"G +{goldAmount} 獲得！";
            goldRewardText.gameObject.SetActive(true);
        }
        else
        {
            goldRewardText.gameObject.SetActive(false);
        }

        titleText.text = $"カードを{maxSelectCount}枚選んでください";
        confirmButton.interactable = false;
        UpdateSelectedCountText();

        foreach (Transform child in cardListParent)
            Destroy(child.gameObject);
        spawnedItems.Clear();

        // 報酬カードを抽選して表示
        List<int> rewardCards = GetRewardCards(rewardData);
        foreach (int cardId in rewardCards)
        {
            CardRewardItem item = Instantiate(cardItemPrefab, cardListParent);
            item.Setup(cardId, OnCardClicked, OnCardDetailClicked);
            spawnedItems.Add(item);
        }
    }

    private void OnCardClicked(int cardId, CardRewardItem item)
    {
        if (selectedCardIds.Contains(cardId))
        {
            selectedCardIds.Remove(cardId);
            item.SetSelected(false);
        }
        else if (selectedCardIds.Count < maxSelectCount)
        {
            selectedCardIds.Add(cardId);
            item.SetSelected(true);
        }

        confirmButton.interactable = selectedCardIds.Count == maxSelectCount;
        UpdateSelectedCountText();
    }

    private void OnCardDetailClicked(int cardId)
    {
        detailPanel.Open(cardId);
    }

    public void OnConfirmButtonClick()
    {
        confirmPanel.Open(selectedCardIds, OnConfirmed);
    }

    private void OnConfirmed()
    {
        foreach (int cardId in selectedCardIds)
            gameState.CurrentDeck.Add(cardId);

        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }

    private void UpdateSelectedCountText()
    {
        selectedCountText.text =
            $"{selectedCardIds.Count} / {maxSelectCount}枚選択中";
    }

    private List<int> GetRewardCards(RewardData rewardData)
    {
        List<int> pool = rewardData.rewardCardPool.Count > 0
            ? new List<int>(rewardData.rewardCardPool)
            : GetCommonPool();

        List<int> result = new List<int>();
        int count = Mathf.Min(rewardData.cardChoiceCount, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return result;
    }

    private List<int> GetCommonPool()
    {
        return new List<int>();
    }
}