using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct VertexJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> Heights;
    [WriteOnly] public NativeArray<float3> Vertices;
    //public NativeArray<TerrainVertex> Vertices;
    public float SizeInWorldUnits;
    public int Stride;
    public int Verts;

    public void Execute(int index)
    {
        int x = index % Verts;
        int z = index / Verts;
        int borderedVerts = Verts + 2;

        float step = SizeInWorldUnits / (Verts - 1);

        int heightIndex = (z + 1) * borderedVerts + (x + 1);

        Vertices[index] = new float3(x * step, Heights[heightIndex], z * step);

        //var vertex = Vertices[index];

        //vertex.Position = new float3(x * step, Heights[heightIndex], z * step);

        //Vertices[index] = vertex;
    }
}