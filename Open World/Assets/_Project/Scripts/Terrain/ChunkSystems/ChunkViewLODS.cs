using UnityEngine;
using WorldGen.Terrain;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class ChunkViewLODS : MonoBehaviour
{
    public ChunkData Data { get; private set; }
    public LODMeshData[] MeshLODs { get; private set; }
    public Mesh[] Meshes { get; private set; }
    private int[] meshLODS = { -1, -1, -1 };
    public int CurrentLOD = -1;
    public MeshRenderer[] MeshRenderers { get; private set; }
    public MeshFilter[] MeshFilters { get; private set; }
    public MeshCollider MeshCollider { get; private set; }

    [SerializeField] Material terrainMaterial;

    int activeRenderIndex = -1;

    private void Awake()
    {
        MeshLODs = new LODMeshData[ChunkSettings.MeshLevelsOfDetail];
        MeshRenderers = GetComponentsInChildren<MeshRenderer>();
        MeshFilters = GetComponentsInChildren<MeshFilter>();
        MeshCollider = GetComponentInChildren<MeshCollider>();

        for (int i = 0; i < MeshLODs.Length; i++)
        {
            int stride = 1 << i;
            int verts = (ChunkSettings.ChunkVerticies - 1) / stride + 1;
            int borderedVerts = verts + 2;

            var md = new LODMeshData()
            {
                Vertices = new Vector3[verts * verts],
                Normals = new Vector3[verts * verts],
                Heights = new float[borderedVerts * borderedVerts],
                Stride = stride,
                Verts = verts
            };

            md.Mesh = TerrainMeshGenerator.CreateBaseMesh(md.Vertices, md.Normals, md.Verts);

            MeshLODs[i] = md;
        }

        terrainMaterial = new Material(terrainMaterial)
        {
            color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f))
        };

        foreach(var mr in MeshRenderers)
        {
            mr.sharedMaterial = terrainMaterial;
        }
    }

    public void Bind(ChunkData data)
    {
        Data = data;

        //MeshRenderer.enabled = data.CullData.Visible; // Enable this line of code for better editor performance
    }

    public void Unbind()
    {
        MeshCollider.sharedMesh = null;

        Data = null;
    }

    public int SetMeshRendererMesh(int lod)
    {
        var target = activeRenderIndex == 0 ? 1 : 0;

        MeshFilters[target].sharedMesh = MeshLODs[lod].Mesh;

        return target;
    }

    public void BakeMeshCollider()
    {
        if (MeshCollider.sharedMesh == null)
        {
            MeshCollider.sharedMesh = MeshLODs[ChunkSettings.ColliderLevelOfDetail].Mesh;
        }
    }

    public LODMeshData GetLODMeshData(int lod)
    {
        return MeshLODs[lod];
    }

    public void SetVisible(bool visible)
    {
        //MeshRenderer.enabled = visible; // Enable this line of code for better editor performance
    }

    public bool HasMesh(int lod)
    {
        return Meshes[lod] != null ? true : false;
    }
}
