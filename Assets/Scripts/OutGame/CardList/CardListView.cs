using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DanielLochner.Assets.SimpleScrollSnap;


public class CardListView : MonoBehaviour
{
    [SerializeField] GameObject cardListPrefab;
    [SerializeField] GameObject content;
    [SerializeField] SimpleScrollSnap snap;
    void Start()
    {
        for(int i = 0; i < 5; i++)
        {
            snap.AddToBack(cardListPrefab);
        }
    }

    void Update()
    {
        
    }
}
