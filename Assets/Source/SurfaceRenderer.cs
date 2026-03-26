using System.Collections.Generic;
using MarchingCubesProject;
using UnityEngine;
using UnityEngine.Rendering;

public class SurfaceRenderer : MonoBehaviour
{
    public static SurfaceRenderer Instance;
    private StatefulTask recalculationTask;
    private MeshFilter meshFilterPositive, meshFilterNegative;
    private MarchingCubes marchingCubes;
    private CrystalStructure currentStructure;
    private List<Vector3> verticesPositive = new(), verticesNegative = new();
    private List<int> indicesPositive = new(), indicesNegative = new();
    private Mesh positiveMesh, negativeMesh;
    private bool isDisplayEnabled = true;

    private float surfaceValue = 0.5f;
    public float SurfaceValue
    {
        get => surfaceValue;
        set
        {
            if (Mathf.Approximately(surfaceValue, value))
            {
                return;
            }
            
            surfaceValue = Mathf.Abs(value);
            if (currentStructure != null && isDisplayEnabled)
            {
                recalculationTask.RequestRecompute();
            }
        }
    }
    
    void Awake()
    {
#if UNITY_WEBGL
        gameObject.SetActive(false);
#else
        Instance = this;
        var meshFilters = GetComponentsInChildren<MeshFilter>();
        meshFilterPositive = meshFilters[0];
        meshFilterNegative = meshFilters[1];
        positiveMesh = new Mesh
        {
            indexFormat = IndexFormat.UInt32
        };;
        negativeMesh = new Mesh
        {
            indexFormat = IndexFormat.UInt32
        };;
        marchingCubes = new MarchingCubes();
        recalculationTask = new StatefulTask(Regenerate, ReplaceMeshes);
#endif
    }

    private void GenerateSurfaceMesh(CrystalStructure structure, float surface, ref List<Vector3> vertices, ref List<int> indices)
    {
        marchingCubes.Surface = surface;
        vertices.Clear();
        indices.Clear();

        int numVoxelsX = structure.VoxelData.GetLength(0);
        int numVoxelsY = structure.VoxelData.GetLength(1);
        int numVoxelsZ = structure.VoxelData.GetLength(2);
        marchingCubes.Generate(structure.VoxelData, vertices, indices);
        
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = CrystalStructureView.SwapYZ(structure.LatticeMatrix * 
                                                      new Vector4(vertices[i].x / numVoxelsX, vertices[i].y / numVoxelsY, vertices[i].z / numVoxelsZ, 1));
        }
    }

    private void Regenerate()
    {
        GenerateSurfaceMesh(currentStructure, surfaceValue, ref verticesPositive, ref indicesPositive);
        if (surfaceValue != 0)
        {
            GenerateSurfaceMesh(currentStructure, -surfaceValue, ref verticesNegative, ref indicesNegative);
        }
        else
        {
            verticesNegative.Clear();
            indicesNegative.Clear();
        }
    }

    private static void UpdateMesh(ref Mesh mesh, ref List<Vector3> vertices, ref List<int> indices)
    {
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void ReplaceMeshes()
    {
        if (verticesPositive.Count > 0)
        {
            UpdateMesh(ref positiveMesh, ref verticesPositive, ref indicesPositive);
            meshFilterPositive.mesh = positiveMesh;
        }
        else
        {
            meshFilterPositive.mesh = null;
        }
        
        if (verticesNegative.Count > 0)
        {
            UpdateMesh(ref negativeMesh, ref verticesNegative, ref indicesNegative);
            meshFilterNegative.mesh = negativeMesh;
        }
        else
        {
            meshFilterNegative.mesh = null;
        }
        
    }

    public void SetStructure(CrystalStructure structure)
    {
        if (structure.VoxelData == null)
        {
            return;
        }
        
        currentStructure = structure;
        recalculationTask.RequestRecompute();
    }
    
    public void SetDisplay(bool display)
    {
        if (display && !isDisplayEnabled)
        {
            recalculationTask.RequestRecompute();
        }

        isDisplayEnabled = display;
        meshFilterPositive.gameObject.SetActive(display);
        meshFilterNegative.gameObject.SetActive(display);
    }
}
