using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpellDropManager : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardController spellCard = eventData.pointerDrag.GetComponent<CardController>();
        CardController target = GetComponent<CardController>();
        
        if (spellCard == null)
        {
            return;
        }
        if (spellCard.CanUseSpells())//CanUseSpell()
        {
            spellCard.UseSpellTo(target);
        }

    }
}
