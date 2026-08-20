using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class MeshTicket
{
    public Vector2Int ChunkID;
    public int LOD;
    public NativeArray<float> Heights;
    //public NativeArray<Vector3> Vertices;
    //public NativeArray<Vector3> Normals;
    public JobHandle Handle;
    public MeshTicketState State;
}

public enum MeshTicketState
{
    GeneratingHeights,
    GeneratingVertices,
    GeneratingNormals,
    Completed
}