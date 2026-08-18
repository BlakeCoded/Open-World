using UnityEngine;
using WorldGen.Terrain;

public class ChunkView : MonoBehaviour
{
    public ChunkData Data { get; private set; }
    public Mesh Mesh { get; private set; }
    public MeshRenderer MeshRenderer { get; private set; }
    public MeshFilter MeshFilter { get; private set; }
    public MeshCollider MeshCollider { get; private set; }
    [HideInInspector] public float[] Heights;
    [HideInInspector] public Vector3[] Vertices;
    [HideInInspector] public Vector3[] Normals;
    [SerializeField] Material terrainMaterial;

    private void Awake()
    {
        Mesh = TerrainMeshGenerator.CreateBaseMesh();
        MeshRenderer = GetComponent<MeshRenderer>();
        MeshFilter = GetComponent<MeshFilter>();
        MeshCollider = GetComponent<MeshCollider>();

        MeshFilter.sharedMesh = Mesh;

        //terrainMaterial = new Material(terrainMaterial)
        //{
        //    color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f))
        //};

        MeshRenderer.sharedMaterial = terrainMaterial;

        int borderedVerts = ChunkSettings.ChunkVerticies + 2;

        Heights = new float[borderedVerts * borderedVerts];
        Vertices = Mesh.vertices;
        Normals = Mesh.normals;
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

    public void BakeMeshCollider()
    {
        MeshCollider.sharedMesh = Mesh;
    }

    public void SetVisible(bool visible)
    {
        //MeshRenderer.enabled = visible; // Enable this line of code for better editor performance
    }
}
