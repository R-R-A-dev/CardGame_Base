using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardRewardConfirmPanel : MonoBehaviour
{
    [SerializeField] private Transform cardNameListParent;
    [SerializeField] private TextMeshProUGUI cardNameItemPrefab;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private System.Action onConfirmed;

    private void Start()
    {
        yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(OnNoClicked);
        gameObject.SetActive(false);
    }

    public void Open(List<int> selectedCardIds, System.Action onConfirmedCallback)
    {
        onConfirmed = onConfirmedCallback;
        gameObject.SetActive(true);

        // 選択したカード名を一覧表示
        foreach (Transform child in cardNameListParent)
            Destroy(child.gameObject);

        foreach (int cardId in selectedCardIds)
        {
            CardEntity card = CardDatabase.LoadCardByID(cardId);
            TextMeshProUGUI nameText = Instantiate(cardNameItemPrefab, cardNameListParent);
            nameText.text = card.name;
        }
    }

    private void OnYesClicked()
    {
        gameObject.SetActive(false);
        onConfirmed?.Invoke();
    }

    private void OnNoClicked()
    {
        gameObject.SetActive(false);
    }
}