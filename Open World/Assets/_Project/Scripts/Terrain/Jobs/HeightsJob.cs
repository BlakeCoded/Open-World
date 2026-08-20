using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;

[BurstCompile]
public struct HeightsJob : IJobParallelFor
{
    public float2 ChunkID;
    public NativeArray<float> Heights;
    public NoiseSettings NoiseSettings;
    public float SizeInWorldUnits;
    public int MaxDetailVerts;
    public int Verts;
    public int Stride;

    public void Execute(int index)
    {
        int x = index % Verts - 1;
        int z = index / Verts - 1;

        float fullStep = SizeInWorldUnits / (MaxDetailVerts - 1);
        float step = fullStep * Stride;

        float worldX = ChunkID.x * SizeInWorldUnits + x * step;
        float worldZ = ChunkID.y * SizeInWorldUnits + z * step;

        Heights[index] = Noise.SampleHeight(worldX, worldZ, NoiseSettings);
    }
}