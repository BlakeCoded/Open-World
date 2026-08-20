using Unity.Collections;
using UnityEngine;

public class LODMeshData
{
    public Mesh Mesh;
    public Vector3[] Vertices;
    public Vector3[] Normals;
    public float[] Heights;
    public int Stride;
    public int Verts;
    public Vector2Int GeneratedFor = new Vector2Int(int.MinValue, int.MaxValue);
}
