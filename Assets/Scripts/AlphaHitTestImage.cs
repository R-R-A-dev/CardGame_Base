using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AlphaHitTestImage : MonoBehaviour, ICanvasRaycastFilter
{
    [SerializeField, Range(0f, 1f)] private float alphaThreshold = 0.1f;

    private Image image;
    private Texture2D readableTexture;

    void Awake()
    {
        image = GetComponent<Image>();
        readableTexture = CreateReadableTexture((Texture2D)image.mainTexture);
    }

    void OnDestroy()
    {
        if (readableTexture != null)
        {
            Destroy(readableTexture);
        }
    }

    // Import設定でRead/Writeが無効なテクスチャでも判定できるよう、
    // RenderTextureへBlitして読み取り可能なコピーを作成する
    private static Texture2D CreateReadableTexture(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readable;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (image.sprite == null)
        {
            return true;
        }

        Rect rect = image.rectTransform.rect;
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, screenPoint, eventCamera, out localPoint))
        {
            return false;
        }

        float normalizedX = (localPoint.x - rect.x) / rect.width;
        float normalizedY = (localPoint.y - rect.y) / rect.height;

        if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
        {
            return false;
        }

        Rect spriteRect = image.sprite.rect;
        int texX = Mathf.Clamp(Mathf.FloorToInt(spriteRect.x + normalizedX * spriteRect.width), 0, readableTexture.width - 1);
        int texY = Mathf.Clamp(Mathf.FloorToInt(spriteRect.y + normalizedY * spriteRect.height), 0, readableTexture.height - 1);

        return readableTexture.GetPixel(texX, texY).a >= alphaThreshold;
    }
}
