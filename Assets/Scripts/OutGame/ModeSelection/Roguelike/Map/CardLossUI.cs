using TMPro;
using UnityEngine;

public class CardLossUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Transform cardListParent;

    private RoguelikeGameState gameState;
    private CardLossData cardLossData;
    private int lossCount;

    public void Open(CardLossData data, RoguelikeGameState state)
    {
        cardLossData = data;
        gameState = state;
        lossCount = data.cardLossCount;
        //gameObject.SetActive(true);

        //descText.text = data.description;

        // ランダムで失う
        RemoveRandomCards();

    }

    private void ShowDeckForSelection()
    {
        // デッキのカードを表示して選択させる
    }

    private void RemoveRandomCards()
    {
        // デッキの枚数がcardLossCount未満なら処理しない
        if (gameState.CurrentDeck.Count < cardLossData.cardLossCount)
        {
            Debug.Log($"デッキ枚数が足りないため消失しません。" +
                      $"デッキ:{gameState.CurrentDeck.Count}枚 / 必要:{cardLossData.cardLossCount}枚");
            Close();
            return;
        }

        for (int i = 0; i < cardLossData.cardLossCount; i++)
        {
            int index = Random.Range(0, gameState.CurrentDeck.Count);
            gameState.CurrentDeck.RemoveAt(index);
        }

        Close();
    }

    public void OnCardSelected(int cardId)
    {
        gameState.CurrentDeck.Remove(cardId);
        lossCount--;

        if (lossCount <= 0) Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
        RoguelikeManager.Instance.ReturnToMap();
    }
}