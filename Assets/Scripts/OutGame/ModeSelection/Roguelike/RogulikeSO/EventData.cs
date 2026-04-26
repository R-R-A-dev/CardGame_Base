using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventData", menuName = "Roguelike/EventData")]
public class EventData : ScriptableObject
{
    [Header("イベント設定")]
    public string eventName;
    [TextArea(2, 4)]
    public string eventDescription;
    public List<EventChoice> choices;   // 選択肢
}