using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct NormalsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> Heights;
    [WriteOnly] public NativeArray<float3> Normals;
    public float SizeInWorldUnits;
    public int Verts;
    public int Stride;

    public void Execute(int index)
    {
        int x = index % Verts;
        int z = index / Verts;
        int borderedVerts = Verts + 2;

        float step = SizeInWorldUnits / (Verts - 1);

        int heightX = x + 1;
        int heightZ = z + 1;

        int heightIndex = heightZ * borderedVerts + heightX;

        float3 left = new float3((x - 1) * step, Heights[heightIndex - 1], z * step);
        float3 right = new float3((x + 1) * step, Heights[heightIndex + 1], z * step);
        float3 top = new float3(x * step, Heights[heightIndex - borderedVerts], (z - 1) * step);
        float3 bottom = new float3(x * step, Heights[heightIndex + borderedVerts], (z + 1) * step);

        float3 horizontal = right - left;
        float3 vertical = bottom - top;

        Normals[index] = math.normalize(math.cross(vertical, horizontal));
    }
}
