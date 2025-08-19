using System.Collections;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    [SerializeField] CardController cardPrefab;
    [SerializeField] Transform openedCardTrans;

    [SerializeField] Transform[] openPackPos;

    [SerializeField] Transform expandCardTrans1;
    [SerializeField] Transform expandCardTrans2;
    [SerializeField] Transform expandCardTrans3;
    [SerializeField] Transform expandCardTrans4;
    [SerializeField] Transform expandCardTrans5;
    [SerializeField] Transform expandCardTrans6;
    [SerializeField] Transform expandCardTrans7;
    [SerializeField] Transform expandCardTrans8;

    private void Start()
    {
        OpenPack();
    }

    void OpenPack()
    {
        CreateCard(decisionCardId());

        StartCoroutine(ExpandPackCards());
    }

    void CreateCard(int[] cardId)
    {
        for (int i = 0; i < openPackPos.Length; i++)
        {
            CardController card = Instantiate(cardPrefab, openPackPos[i]);
            card.Init(cardId[i], true);
        }

    }

    int[] decisionCardId()
    {
        int[] cardId = new int[8];
        return cardId;
    }

    IEnumerator ExpandPackCards()
    {
        int cardNum = 0;
        Transform moveTarget = null;
        //CardController[] cardList = openedCardTrans.GetComponentsInChildren<CardController>();
        CardController[] cardList = new CardController[openPackPos.Length];
        for (int i = 0; i < openPackPos.Length; i++)
        {
            cardList[i] = openPackPos[i].GetComponentInChildren<CardController>();
        }
        yield return new WaitForSeconds(1.25f);
        foreach (CardController card in cardList)
        {
            cardNum += 1;

            switch (cardNum)
            {
                case 1:
                    moveTarget = expandCardTrans1;
                    break;
                case 2:
                    moveTarget = expandCardTrans2;
                    break;
                case 3:
                    moveTarget = expandCardTrans3;
                    break;
                case 4:
                    moveTarget = expandCardTrans4;
                    break;
                case 5:
                    moveTarget = expandCardTrans5;
                    break;
                case 6:
                    moveTarget = expandCardTrans6;
                    break;
                case 7:
                    moveTarget = expandCardTrans7;
                    break;
                case 8:
                    moveTarget = expandCardTrans8;
                    break;
            }

            StartCoroutine(card.movement.ExpandThisCard(moveTarget));
            yield return new WaitForSeconds(0.1f);
        }
    }

}
/*


*/