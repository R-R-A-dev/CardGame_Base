using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DanielLochner.Assets.SimpleScrollSnap;


public class CardListView : MonoBehaviour
{
    [SerializeField] GameObject cardListPrefab;
    [SerializeField] GameObject content;
    void Start()
    {
        for(int i = 0; i < 5; i++)
        {
            GameObject card = Instantiate(cardListPrefab, content.transform);
        }
    }

    void Update()
    {
        
    }
}
