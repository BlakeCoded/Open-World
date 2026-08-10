using UnityEngine;
using WorldGen.Terrain;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class ChunkView : MonoBehaviour
{
    public ChunkData Data {  get; private set; }
    public MeshRenderer MeshRenderer {  get; private set; }
    public MeshFilter MeshFilter {  get; private set; }
    public MeshCollider MeshCollider {  get; private set; }

    [SerializeField] Material terrainMaterial;

    private void Awake()
    {
        MeshRenderer = GetComponent<MeshRenderer>();
        MeshFilter = GetComponent<MeshFilter>();
        MeshCollider = GetComponent<MeshCollider>();
    }

    public void Bind(ChunkData data, Mesh mesh)
    {
        Data = data;

        MeshFilter.sharedMesh = mesh;
        MeshCollider.sharedMesh = mesh;

        MeshRenderer.sharedMaterial = terrainMaterial;
        MeshRenderer.enabled = data.CullData.Visible; // Enable this line of code for better editor performance
    }

    public void Unbind()
    {
        MeshFilter.sharedMesh = null;
        MeshCollider.sharedMesh = null;

        Data = null;
    }

    public void SetVisible(bool visible)
    {
        MeshRenderer.enabled = visible; // Enable this line of code for better editor performance
    }
}
