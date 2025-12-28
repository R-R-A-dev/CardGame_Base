using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RippleEffect : MonoBehaviour
{
    public Image rippleImage;
    public float duration = 2f;
    public float startScale = 1f;
    public float endScale = 5f;

    private void Start()
    {
        StartCoroutine(PlayRipple());
    }

    private IEnumerator PlayRipple()
    {
        while (true) // 無限ループ
        {
            float time = 0f;
            while (time < duration)
            {
                float t = time / duration;
                float scale = Mathf.Lerp(startScale, endScale, t);
                float alpha = Mathf.Lerp(1f, 0f, t); // 透明度をだんだん下げる

                rippleImage.transform.localScale = new Vector3(scale, scale, 1);
                Color color = rippleImage.color;
                color.a = alpha;
                rippleImage.color = color;

                time += Time.deltaTime;
                yield return null;
            }

            // リセット
            rippleImage.transform.localScale = Vector3.one; // サイズを元に戻す
            Color resetColor = rippleImage.color;
            resetColor.a = 1f; // 透明度を元に戻す
            rippleImage.color = resetColor;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(PlayRipple());
    }
}