using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct LineStructure
{
    public Vector2 start;
    public Vector2 end;
    public Color color;
    public float thickness;
}

public class ScreenSpaceLineRenderer
{
    private Material lineMaterial;
    private bool zTest = false;
    private float depth;

    private List<LineStructure> lines = new();

    public ScreenSpaceLineRenderer(bool zTest = false)
    {
        this.zTest = zTest;
        CreateLineMaterial();
        RenderPipelineManager.endCameraRendering += Draw;
    }

    ~ScreenSpaceLineRenderer()
    {
        RenderPipelineManager.endCameraRendering -= Draw;
    }

    public void Clear()
    {
        lines.Clear();
    }

    public void SubmitLine(LineStructure line)
    {
        lines.Add(line);
    }

    void CreateLineMaterial()
    {
        // Unity has a built-in shader that is useful for drawing simple colored things
        var shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        // Turn on alpha blending
        lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        // Turn backface culling off
        lineMaterial.SetInt("_Cull", (int)CullMode.Off);
        // Turn off depth writes
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.SetInt("_ZTest", zTest ? 1 : 0);
    }

    private void Draw(ScriptableRenderContext context, Camera cam)
    {
        depth = cam.nearClipPlane + 5f;
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(cam.transform.localToWorldMatrix);
        GL.Begin(GL.QUADS);

        foreach (var item in lines)
        {
            DrawLine(cam.orthographicSize * new Vector3(item.start.x, item.start.y),
                cam.orthographicSize * new Vector3(item.end.x, item.end.y),
                item.thickness * cam.orthographicSize, item.color);
        }

        GL.End();
        GL.PopMatrix();
    }

    private void DrawLine(Vector3 start, Vector3 end, float thickness, Color color)
    {
        var translationVector = end - start;
        var normalVector = new Vector3(-translationVector.y, translationVector.x, 0).normalized;
        normalVector *= thickness;

        GL.Color(color);
        Vector3 corner = start + normalVector;
        GL.Vertex3(corner.x, corner.y, depth);
        corner = end + normalVector;
        GL.Vertex3(corner.x, corner.y, depth);
        corner = end - normalVector;
        GL.Vertex3(corner.x, corner.y, depth);
        corner = start - normalVector;
        GL.Vertex3(corner.x, corner.y, depth);
    }
}