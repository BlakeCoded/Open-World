using Unity.Collections;
using UnityEngine;

public class LODMeshData
{
    public Mesh Mesh;
    public int Stride;
    public int Verts;
    public Vector2Int GeneratedFor = new Vector2Int(int.MinValue, int.MaxValue);
}
