using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CardOwnershipConverter
{
    public static List<int> ToCardIdList(List<int> ownedCardCounts)
    {
        List<int> result = new List<int>();

        if (ownedCardCounts == null) return result;

        for (int i = 0; i < ownedCardCounts.Count; i++)
        {
            int cardId = i + 1;
            int count = ownedCardCounts[i];

            for (int n = 0; n < count; n++)
                result.Add(cardId);
        }

        return result;
    }

    public static List<int> ToOwnedCardCounts(List<int> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0)
            return new List<int>();

        // cardId=0以下が混入していないかチェック
        int maxCardId = cardIds.Where(id => id > 0).DefaultIfEmpty(0).Max();
        if (maxCardId == 0) return new List<int>();

        List<int> result = new List<int>(new int[maxCardId]);

        foreach (int cardId in cardIds)
        {
            if (cardId <= 0)
            {
                Debug.LogWarning($"不正なcardId({cardId})はスキップされました");
                continue;
            }

            int index = cardId - 1;
            result[index]++;
        }

        return result;
    }

    public static void AddCard(List<int> ownedCardCounts, int cardId)
    {
        if (cardId <= 0)
        {
            Debug.LogWarning($"不正なcardId({cardId})のため追加をスキップしました");
            return;
        }

        int index = cardId - 1;

        while (ownedCardCounts.Count <= index)
            ownedCardCounts.Add(0);

        ownedCardCounts[index]++;
    }

    public static bool RemoveCard(List<int> ownedCardCounts, int cardId)
    {
        if (cardId <= 0) return false;

        int index = cardId - 1;

        if (index < 0 || index >= ownedCardCounts.Count) return false;
        if (ownedCardCounts[index] <= 0) return false;

        ownedCardCounts[index]--;
        return true;
    }

    public static int GetCardCount(List<int> ownedCardCounts, int cardId)
    {
        if (cardId <= 0) return 0;

        int index = cardId - 1;
        if (ownedCardCounts == null || index < 0 || index >= ownedCardCounts.Count) return 0;
        return ownedCardCounts[index];
    }
}