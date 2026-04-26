using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardRewardPanel : MonoBehaviour
{
    [SerializeField] private Transform cardListParent;
    [SerializeField] private CardRewardItem cardItemPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private CardRewardDetailPanel detailPanel;

    private RoguelikeGameState gameState;

    public void Open(RewardData rewardData, RoguelikeGameState state)
    {
        gameState = state;
        gameObject.SetActive(true);
        titleText.text = "報酬カードを獲得しました";

        foreach (Transform child in cardListParent)
            Destroy(child.gameObject);

        // 報酬カードを全て取得してデッキに追加・表示
        List<int> rewardCards = GetRewardCards(rewardData);
        foreach (int cardId in rewardCards)
        {
            gameState.CurrentDeck.Add(cardId); // 全部デッキに追加

            CardRewardItem item = Instantiate(cardItemPrefab, cardListParent);
            item.Setup(cardId, OnCardClicked);
        }
    }

    // カードをクリックで詳細パネルを開く
    private void OnCardClicked(int cardId)
    {
        detailPanel.Open(cardId);
    }

    public void OnCloseButtonClick()
    {
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