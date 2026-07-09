using UnityEngine;
using UnityEngine.UI;

public class GachaCardItem : MonoBehaviour
{
    [SerializeField] private GameObject cardFront;  // カード表面
    [SerializeField] private GameObject cardBack;   // カード裏面
    [SerializeField] private CardController cardController;
    [SerializeField] private Button cardButton;

    private bool isRevealed = false;
    private bool isClickable = false;
    private int cardId;
    private System.Action<GachaCardItem> onClicked;

    public void Setup(int id, bool revealed, System.Action<GachaCardItem> onClickedCallback)
    {
        cardId = id;
        isRevealed = revealed;
        onClicked = onClickedCallback;

        cardFront.SetActive(revealed);
        cardBack.SetActive(!revealed);

        if (revealed)
            cardController.Init(cardId, false);

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnCardButtonClick);
        cardButton.interactable = false;
    }

    public void SetClickable(bool clickable)
    {
        isClickable = clickable;
        cardButton.interactable = clickable && !isRevealed;
    }

    private void OnCardButtonClick()
    {
        if (!isClickable || isRevealed) return;
        onClicked?.Invoke(this);
    }

    // カードを表向きにするアニメーション枠
    public void PlayRevealAnimation()
    {
        // アニメーション後にRevealを呼ぶ
        Reveal();
    }

    // カードを表向きにする
    public void Reveal()
    {
        isRevealed = true;
        cardButton.interactable = false;

        cardFront.SetActive(true);
        cardBack.SetActive(false);
        cardController.Init(cardId, false);
    }

    // スキップ時に強制的に表向きにする
    public void ForceReveal()
    {
        if (isRevealed) return;
        Reveal();
    }
}