using UnityEngine;


public class DottedLineScript : MonoBehaviour, IPoolable
{
    public float DotLengthScale = 5;

    private LineRenderer lineRenderer;
    private float width;
    private bool isUsed = false;

    public void SetLine(Vector3 startPos, Vector3 endPos)
    {
        lineRenderer.SetPositions(new[] { startPos, endPos });
        float lineLength = (endPos - startPos).magnitude;
        lineRenderer.textureScale = new Vector2(lineLength / (DotLengthScale * width), 1.0f);
    }

    public void Init()
    {
        lineRenderer = GetComponent<LineRenderer>();
        width = lineRenderer.startWidth;
        isUsed = true;
    }

    public void Enable()
    {
        gameObject.SetActive(true);
        isUsed = true;
    }

    public void Disable()
    {
        gameObject.SetActive(false);
        isUsed = false;
    }

    public void Destroy()
    {
        GameObject.Destroy(gameObject);
    }

    public bool IsUsed => isUsed;
}