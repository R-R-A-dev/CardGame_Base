using UnityEngine;
using UnityEngine.UI;

public class SetAlphaHitTest : MonoBehaviour
{
    [SerializeField] private float threshold = 0.1f;

    void Start()
    {
        Image image = GetComponent<Image>();
        image.alphaHitTestMinimumThreshold = threshold;
    }
}