using UnityEngine;
using WorldGen.Terrain;

public class ChunkView : MonoBehaviour
{
    public ChunkData Data { get; private set; }
    public LODMeshData[] MeshData { get; private set; }
    public MeshRenderer[] MeshRenderers { get; private set; }
    public MeshFilter[] MeshFilters { get; private set; }
    public MeshCollider MeshCollider { get; private set; }
    public int CurrentLOD = -1;
    int activeRenderIndex = -1;
    public bool IsVisible = false;
    [SerializeField] Material terrainMaterial;

    public void Configure()
    {
        MeshData = new LODMeshData[ChunkSettings.MeshLevelsOfDetail];
        MeshRenderers = GetComponentsInChildren<MeshRenderer>();
        MeshFilters = GetComponentsInChildren<MeshFilter>();
        MeshCollider = GetComponentInChildren<MeshCollider>();

        for (int i = 0; i < MeshData.Length; i++)
        {
            int stride = 1 << i;
            int verts = (ChunkSettings.ChunkVerticies - 1) / stride + 1;

            var md = new LODMeshData()
            {
                Stride = stride,
                Verts = verts
            };

            MeshData[i] = md;
        }

        //terrainMaterial = new Material(terrainMaterial)
        //{
        //    color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f))
        //};

        foreach (var mr in MeshRenderers)
        {
            mr.sharedMaterial = terrainMaterial;
        }
    }

    public void Bind(ChunkData data)
    {
        Data = data;

        //MeshRenderers[0].enabled = data.CullData.Visible; // Enable this line of code for better editor performance
    }

    public void Unbind()
    {
        MeshCollider.sharedMesh = null;

        MeshFilters[0].sharedMesh = null;
        MeshFilters[1].sharedMesh = null;

        Data = null;
    }

    public Mesh GetOrCreateMesh(LODMeshData data)
    {
        if(data.Mesh == null)
            data.Mesh = TerrainMeshGenerator.CreateBaseMesh(data.Verts, data.Stride);

        return data.Mesh;
    }

    public void SetLOD(int lod)
    {
        CurrentLOD = lod;
        MeshFilters[0].sharedMesh = MeshData[lod].Mesh;
    }

    public int SetActiveMeshrenderer(int lod)
    {
        var target = activeRenderIndex == 1 ? 0 : 1;

        MeshFilters[target].sharedMesh = MeshData[lod].Mesh;

        return target;
    }

    public void BakeMeshCollider()
    {
            MeshCollider.sharedMesh = MeshData[ChunkSettings.ColliderLevelOfDetail].Mesh;
    }

    public LODMeshData GetLODMeshData(int lod)
    {
        return MeshData[lod];
    }

    public void SetVisible(bool visible)
    {
        //MeshRenderers[0].enabled = visible; // Enable this line of code for better editor performance
    }
}
