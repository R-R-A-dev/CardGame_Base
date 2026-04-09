using UnityEngine;

public class TestScroll : MonoBehaviour
{
    AutoScrollController scrollController;

    void Start()
    {
        scrollController = GetComponent<AutoScrollController>();
        scrollController.ScrollToBottom();
    }
}
