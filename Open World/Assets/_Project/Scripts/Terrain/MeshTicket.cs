using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MeshTicket
{
    public Vector2Int ID;
    public uint GenerationID;

    public Mesh Mesh;
    //public Mesh.MeshDataArray meshDataArray;

    public NativeArray<float> Heights;
    public NativeArray<float3> Vertices;
    public NativeArray<float3> Normals;
    public NativeReference<float2> MinMaxHeight;

    public JobHandle Handle;

    public Action<MeshTicket> OnComplete;
}