using UnityEngine;

public class SelectionBoxDrawer : MonoBehaviour
{
    public float depth = 0.1f;
    public float thickness = 0.01f;
    
    private LineRenderer[] lineRenderers;
    private Camera mainCamera;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        depth = mainCamera.nearClipPlane + 1e-2f;
        lineRenderers = new LineRenderer[4];
        for (int i = 0; i < transform.childCount; i++)
        {
            lineRenderers[i] = transform.GetChild(i).GetComponent<LineRenderer>();
            lineRenderers[i].positionCount = 2;
        }
        
        Hide();
    }

    public void Display()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetLine(int i, Vector3 from, Vector3 to)
    {
        lineRenderers[i].startWidth = thickness * mainCamera.orthographicSize;
        lineRenderers[i].endWidth = thickness * mainCamera.orthographicSize;
        lineRenderers[i].SetPosition(0, from);
        lineRenderers[i].SetPosition(1, to);
    }

    public void DrawBox(Rect rect)
    {
        Vector3 topLeft = mainCamera.ScreenToWorldPoint(new Vector3(rect.xMin, rect.yMin, depth));
        Vector3 topRight = mainCamera.ScreenToWorldPoint(new Vector3(rect.xMax, rect.yMin, depth));
        Vector3 bottomRight = mainCamera.ScreenToWorldPoint(new Vector3(rect.xMax, rect.yMax, depth));
        Vector3 bottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(rect.xMin, rect.yMax, depth));
        
        SetLine(0, topLeft, bottomLeft);
        SetLine(1, topRight, bottomRight);
        SetLine(2, bottomRight, bottomLeft);
        SetLine(3, topLeft, topRight);
    }
}
