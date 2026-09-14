using UnityEngine;

public class VisibleHitbox_Debug : MonoBehaviour
{
    private BoxCollider boxCollider;
    private LineRenderer lineRenderer;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.positionCount = 16;

        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
    }

    private void Update()
    {
        DrawHitbox();
    }

    private void DrawHitbox()
    {
        Vector3 center = boxCollider.center;
        Vector3 halfSize = boxCollider.size / 2f;

        Vector3[] corners =
        {
            center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
            center + new Vector3( halfSize.x, -halfSize.y, -halfSize.z),
            center + new Vector3( halfSize.x, -halfSize.y,  halfSize.z),
            center + new Vector3(-halfSize.x, -halfSize.y,  halfSize.z),

            center + new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),
            center + new Vector3( halfSize.x,  halfSize.y, -halfSize.z),
            center + new Vector3( halfSize.x,  halfSize.y,  halfSize.z),
            center + new Vector3(-halfSize.x,  halfSize.y,  halfSize.z)
        };

        Vector3[] lines =
        {
            corners[0], corners[1],
            corners[1], corners[2],
            corners[2], corners[3],
            corners[3], corners[0],

            corners[4], corners[5],
            corners[5], corners[6],
            corners[6], corners[7],
            corners[7], corners[4],

            corners[0], corners[4],
            corners[1], corners[5],
            corners[2], corners[6],
            corners[3], corners[7]
        };

        lineRenderer.positionCount = lines.Length;
        lineRenderer.SetPositions(lines);
    }
}