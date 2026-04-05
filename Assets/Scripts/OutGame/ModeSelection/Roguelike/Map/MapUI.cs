using UnityEngine;

public class MapUI : MonoBehaviour
{
    [SerializeField] private MapManager mapManager;

    public void Initialize(MapData mapData, RoguelikeGameState state)
    {
        gameObject.SetActive(true);
        //mapManager.Initialize(mapData, state);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}