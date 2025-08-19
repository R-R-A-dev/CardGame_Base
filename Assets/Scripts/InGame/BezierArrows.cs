using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class BezierArrows : MonoBehaviour
{
    #region Public Fields 
    public GameObject ArrowHeadPrefab;
    public GameObject ArrowNodePrefab;
    public int arrowNodeNum;
    public float scaleFactor = 1.0f;

    public static BezierArrows Instance { get; private set; }

    #endregion

    #region Private Fields
    Vector3 origin;
    Vector3 top;
    List<RectTransform> arrowNodes = new List<RectTransform>();

    List<Vector2> controlPoints = new List<Vector2>();

    readonly List<Vector2> controlPointFactors = new List<Vector2>()
    {
        new Vector2(-0.3f, 0.8f),
        new Vector2(0.1f, 1.4f),
    };
    #endregion

    private void Awake()
    {
        Instance = this;
        // origin = GetComponent<RectTransform>();
        for (int i = 0; i < arrowNodeNum; i++)
        {
            // Nodeを生成して、リストに追加
            GameObject arrowNode = Instantiate(ArrowNodePrefab, transform);
            arrowNodes.Add(arrowNode.GetComponent<RectTransform>());
        }

        // Headを生成
        GameObject arrowHead = Instantiate(ArrowHeadPrefab, transform);
        arrowNodes.Add(arrowHead.GetComponent<RectTransform>());

        // 初期位置を設定(遠くに)
        arrowNodes.ForEach(node => node.GetComponent<RectTransform>().position = new Vector2(-1000, 1000));

        for (int i = 0; i < 4; i++)
        {
            controlPoints.Add(Vector2.zero);
        }

        SetColor(new Color32(100, 100, 100, 255));
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        controlPoints[0] = origin;// new Vector2(origin.x, origin.position.y);
        controlPoints[3] = top;// new Vector2(top.x, top.position.y);

        controlPoints[1] = controlPoints[0] + (controlPoints[3] - controlPoints[0]) * controlPointFactors[0];
        controlPoints[2] = controlPoints[0] + (controlPoints[3] - controlPoints[0]) * controlPointFactors[1];

        for (int i = 0; i < arrowNodes.Count; i++)
        {
            float t = Mathf.Log(1f * i / (this.arrowNodes.Count - 1) + 1f, 2f);

            this.arrowNodes[i].position =
                Mathf.Pow(1 - t, 3) * controlPoints[0] +
                3 * Mathf.Pow(1 - t, 2) * t * controlPoints[1] +
                3 * (1 - t) * Mathf.Pow(t, 2) * controlPoints[2] +
                Mathf.Pow(t, 3) * controlPoints[3];

            if (i > 0)
            {
                var euler = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, arrowNodes[i].position - arrowNodes[i - 1].position));
                arrowNodes[i].rotation = Quaternion.Euler(euler);
            }

            var scale = scaleFactor * (1f - 0.03f * (arrowNodes.Count - 1 - i));
            arrowNodes[i].localScale = new Vector3(scale, scale, 1);
        }

        this.arrowNodes[0].transform.rotation = arrowNodes[1].transform.rotation;
    }

    public void SetOriginPos(Vector3 pos)
    {
        origin = pos;
    }
    public void SetTopPos(Vector3 pos)
    {
        // 少し左にずらす
        // top = pos + new Vector3(-50, 0, 0);
        top = pos;
        // Input.mousePosition.y
    }

    // 色を変える
    public void SetColor(Color color)
    {
        foreach (var node in arrowNodes)
        {
            node.GetComponent<Image>().color = color;
        }
    }
}