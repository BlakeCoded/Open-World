using UnityEngine;
using WorldGen.Terrain;

public class TerrainView : MonoBehaviour
{
    public TerrainData Data;
    public Mesh Mesh { get; private set; }
    public MeshRenderer MeshRenderer { get; private set; }
    public MeshFilter MeshFilter { get; private set; }

    [SerializeField] private Material farViewMaterial;

    public void Configure()
    {
        Mesh = TerrainMeshGenerator.CreateBaseTerrainMesh(TerrainSettings.TerrainVerticies, TerrainSettings.SizeInUnits);
        MeshRenderer = GetComponent<MeshRenderer>();
        MeshFilter = GetComponent<MeshFilter>();

        MeshRenderer.sharedMaterial = farViewMaterial;
    }

    public void Bind(TerrainData data)
    {
        Data = data;
    }

    public void UnBind()
    {
        Data = null;

        MeshFilter.sharedMesh = null;
    }

    public void SetMesh()
    {
        MeshFilter.sharedMesh = Mesh;
    }
}
