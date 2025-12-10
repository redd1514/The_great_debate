using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class VerticalGradientImage : MaskableGraphic
{
    [SerializeField]
    private Color _topColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField]
    private Color _bottomColor = new Color(1f, 1f, 1f, 0f);

    public Color topColor
    {
        get => _topColor;
        set { _topColor = value; SetVerticesDirty(); }
    }

    public Color bottomColor
    {
        get => _bottomColor;
        set { _bottomColor = value; SetVerticesDirty(); }
    }

    public void SetColors(Color top, Color bottom)
    {
        _topColor = top;
        _bottomColor = bottom;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();

        // Vertices: TopLeft, TopRight, BottomRight, BottomLeft
        UIVertex v0 = UIVertex.simpleVert; v0.color = _topColor;   v0.position = new Vector2(rect.xMin, rect.yMax);
        UIVertex v1 = UIVertex.simpleVert; v1.color = _topColor;   v1.position = new Vector2(rect.xMax, rect.yMax);
        UIVertex v2 = UIVertex.simpleVert; v2.color = _bottomColor; v2.position = new Vector2(rect.xMax, rect.yMin);
        UIVertex v3 = UIVertex.simpleVert; v3.color = _bottomColor; v3.position = new Vector2(rect.xMin, rect.yMin);

        vh.AddVert(v0);
        vh.AddVert(v1);
        vh.AddVert(v2);
        vh.AddVert(v3);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}
