using UnityEngine;


public class DottedLineRenderer : MonoBehaviour
{
    void Start()
    {
        var lineRenderer = GetComponent<LineRenderer>();
        float width = lineRenderer.startWidth;
        float lineLength = (lineRenderer.GetPosition(0) - lineRenderer.GetPosition(1)).magnitude;
        lineRenderer.textureScale = new Vector2(lineLength / width, 1.0f);
    }
}