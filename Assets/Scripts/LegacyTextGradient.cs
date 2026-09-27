using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// レガシーの Text（や Image）に上下グラデーションをかけるエフェクト。
/// ・Text の Color は白にしておくと、ここで指定した色がそのまま出ます。
/// ・Outline / Shadow と併用する場合は、このコンポーネントを Inspector で一番上に置いてください。
/// ・ファイル名は必ず「LegacyTextGradient.cs」のままにしてください（クラス名と一致している必要があります）。
/// </summary>
[AddComponentMenu("UI/Effects/Legacy Text Gradient")]
public class LegacyTextGradient : BaseMeshEffect
{
    [SerializeField] private Color topColor = new Color32(0xFF, 0xE9, 0xA8, 0xFF);
    [SerializeField] private Color bottomColor = new Color32(0xD9, 0x8A, 0x2B, 0xFF);

    private readonly List<UIVertex> vertices = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        vertices.Clear();
        vh.GetUIVertexStream(vertices);

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < vertices.Count; i++)
        {
            float y = vertices[i].position.y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        float height = maxY - minY;

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];
            float t = height > 0f ? (vertex.position.y - minY) / height : 0f;
            Color gradient = Color.Lerp(bottomColor, topColor, t);
            vertex.color = gradient * (Color)vertex.color;
            vertices[i] = vertex;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertices);
    }
}
