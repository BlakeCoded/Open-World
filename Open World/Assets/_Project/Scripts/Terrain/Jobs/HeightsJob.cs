using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct HeightsJob : IJobParallelFor
{
    public NativeArray<float> Heights;
    public NoiseSettings NoiseSettings;
    public float2 WorldOrigin;
    public float SizeInWorldUnits;
    public int MaxDetailVerts;
    public int BorderedVerts;
    public int Stride;

    public void Execute(int index)
    {
        int x = index % BorderedVerts - 1;
        int z = index / BorderedVerts - 1;

        float fullStep = SizeInWorldUnits / (MaxDetailVerts - 1);
        float step = fullStep * Stride;

        float worldX = WorldOrigin.x + x * step;
        float worldZ = WorldOrigin.y + z * step;

        Heights[index] = Noise.SampleHeight(worldX, worldZ, NoiseSettings);
    }
}