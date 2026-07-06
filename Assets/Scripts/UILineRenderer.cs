using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : MaskableGraphic
{
    public float thickness = 2f;
    public List<Vector2> points = new List<Vector2>();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points == null || points.Count < 2)
            return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            CreateLineSegment(points[i], points[i + 1], vh);
        }
    }

    private void CreateLineSegment(Vector2 point1, Vector2 point2, VertexHelper vh)
    {
        float angle = GetAngle(point1, point2) + 90f;
        Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * (thickness / 2);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        int index = vh.currentVertCount;

        vertex.position = point1 - offset;
        vh.AddVert(vertex);

        vertex.position = point1 + offset;
        vh.AddVert(vertex);

        vertex.position = point2 + offset;
        vh.AddVert(vertex);

        vertex.position = point2 - offset;
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }

    private float GetAngle(Vector2 me, Vector2 target)
    {
        return Mathf.Atan2(target.y - me.y, target.x - me.x) * Mathf.Rad2Deg;
    }
}
