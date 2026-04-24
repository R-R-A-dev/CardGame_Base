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
        gameObject.SetActive(true);

        descText.text = data.description;

        if (data.isPlayerChoice)
        {
            // プレイヤーがカードを選んで捨てる
            ShowDeckForSelection();
        }
        else
        {
            // ランダムで失う
            RemoveRandomCards();
        }
    }

    private void ShowDeckForSelection()
    {
        // デッキのカードを表示して選択させる
    }

    private void RemoveRandomCards()
    {
        for (int i = 0; i < lossCount; i++)
        {
            if (gameState.CurrentDeck.Count == 0) break;
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