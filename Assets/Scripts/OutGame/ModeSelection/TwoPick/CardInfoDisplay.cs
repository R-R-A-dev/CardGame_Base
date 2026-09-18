using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardInfoDisplay : MonoBehaviour, IPointerDownHandler
{
    //カード情報パネル
    [SerializeField] private GameObject cardInfoPanel;
    [SerializeField] private CardController cardController;
    [SerializeField] private Button closeInfoButton;

    [SerializeField] private Text cardNameText;
    [SerializeField] private TMPro.TextMeshProUGUI cardAttackText;
    [SerializeField] private TMPro.TextMeshProUGUI cardHealthText;
    [SerializeField] private Text cardDescriptionText;



    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetKeyDown(KeyCode.Mouse1)) return;
        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        CardController card = clickedObject.GetComponent<CardController>();
        if (card != null)
        {
            cardInfoPanel.SetActive(true);
            cardController.Init(card.model.no, false);
            cardNameText.text = card.model.name;
            cardAttackText.text = card.model.at.ToString();
            cardHealthText.text = card.model.hp.ToString();
            cardDescriptionText.text = card.model.description;
        }
    }

    public void ClosePannel()
    {
        cardInfoPanel.SetActive(false);
    }
}
//クリックして表示する仕組み
//別のクラス作る