using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardRewardPanel : MonoBehaviour
{
    [SerializeField] private Transform cardListParent;
    [SerializeField] private CardRewardItem cardItemPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button confirmButton;          // 選択確定ボタン
    [SerializeField] private CardRewardDetailPanel detailPanel;
    [SerializeField] private CardRewardConfirmPanel confirmPanel; // 確認パネル

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

        titleText.text = $"カードを{maxSelectCount}枚選んでください";
        confirmButton.interactable = false;

        foreach (Transform child in cardListParent)
            Destroy(child.gameObject);
        spawnedItems.Clear();

        List<int> rewardCards = GetRewardCards(rewardData);
        foreach (int cardId in rewardCards)
        {
            CardRewardItem item = Instantiate(cardItemPrefab, cardListParent);
            item.Setup(cardId, OnCardClicked, OnCardDetailClicked);
            spawnedItems.Add(item);
        }
    }

    // カード選択時
    private void OnCardClicked(int cardId, CardRewardItem item)
    {
        if (selectedCardIds.Contains(cardId))
        {
            // 選択解除
            selectedCardIds.Remove(cardId);
            item.SetSelected(false);
        }
        else if (selectedCardIds.Count < maxSelectCount)
        {
            // 選択追加
            selectedCardIds.Add(cardId);
            item.SetSelected(true);
        }

        // 規定枚数選択で確定ボタン有効化
        confirmButton.interactable = selectedCardIds.Count == maxSelectCount;
        titleText.text = $"カードを{maxSelectCount}枚選んでください" +
                         $"（{selectedCardIds.Count}/{maxSelectCount}）";
    }

    // カード詳細表示
    private void OnCardDetailClicked(int cardId)
    {
        detailPanel.Open(cardId);
    }

    // 確定ボタン押下→確認パネルへ
    public void OnConfirmButtonClick()
    {
        confirmPanel.Open(selectedCardIds, OnConfirmed);
    }

    // 確認パネルで「はい」押下
    private void OnConfirmed()
    {
        foreach (int cardId in selectedCardIds)
            gameState.CurrentDeck.Add(cardId);

        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
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