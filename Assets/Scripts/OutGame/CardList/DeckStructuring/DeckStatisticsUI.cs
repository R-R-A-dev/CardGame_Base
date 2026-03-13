using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckStatisticsUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI pickCountText;
    [SerializeField] private TextMeshProUGUI totalCountText;
    [SerializeField] private TextMeshProUGUI unitCountText;
    [SerializeField] private TextMeshProUGUI spellCountText;
    [SerializeField] private TextMeshProUGUI[] costCountTexts;
    [SerializeField] private RectTransform[] costBars;

    /// <summary>
    /// UI上の集計・統計情報を表示
    /// </summary>
    /// <param name="deckNum"></param>
    public void RefreshStatistics(int deckNum, List<int> deck = null, List<CardEntity> twoPickData = null)
    {
        // デッキ情報が渡されなかった場合、指定されたデッキ番号から取得(デッキの所持状況)
        //TwoPickのデッキが渡された場合はそちらを優先
        if (deck == null)
            deck = CardListData.Decks[deckNum];

        // TwoPickのCaredEntityリストを用意が必要
        // 所持カードリスト取得
        //TwoPickDataが渡された場合はそちらを優先
        if (CardListData.Entities.Count == 0)
        {
            CardEntity[] entitys = CardDatabase.LoadAllCards();
            for (int i = 0; i < entitys.Length; i++)
            {
                CardListData.Entities.Add(entitys[i]);
            }
        }
            List<CardEntity> allCards = CardListData.Entities;

        int pickCount = 0;
        int totalCount = 0;
        int unitCount = 0;
        int spellCount = 0;
        int[] costCounts = new int[10];

        for (int i = 0; i < deck.Count; i++)
        {
            int count = deck[i];
            if (count <= 0) continue;

            totalCount += count;
            CardEntity entity = allCards[i];

            if (entity.spells == SPELLS.NONE)
                unitCount += count;
            else
                spellCount += count;

            int cost = Mathf.Clamp(entity.cost, 1, 10);
            costCounts[cost - 1] += count;
        }
        if (pickCountText != null)
            //pickCountText.text = pickCount.ToString();

        totalCountText.text = totalCount.ToString();
        unitCountText.text = unitCount.ToString();
        spellCountText.text = spellCount.ToString();

        // コスト帯の最大枚数（ゲージの基準）
        int maxCount = 1;
        foreach (int c in costCounts)
            if (c > maxCount) maxCount = c;

        // --- ゲージ表示更新 ---
        for (int i = 0; i < costBars.Length; i++)
        {
            int cardCount = costCounts[i];

            // yスケールを0.1ずつ増加（1枚 = +0.1）
            float newY = 0.1f * cardCount;

            // RectTransformのscaleを変更
            costBars[i].localScale = new Vector3(
                costBars[i].localScale.x,
                newY,
                costBars[i].localScale.z
            );

            // 数字も表示（0でも表示）
            if (costCountTexts != null && i < costCountTexts.Length)
                costCountTexts[i].text = cardCount.ToString();
        }
    }
    void Update()
    {

    }
}
