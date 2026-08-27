using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MinMaxHeightJob : IJob
{
    [ReadOnly] public NativeArray<float> Heights;
    public NativeReference<float> minHeight;
    public NativeReference<float> maxHeight;
    public void Execute()
    {
        float min = float.MaxValue; 
        float max = float.MinValue;

        for(int i = 0;  i < Heights.Length; i++)
        {
            float value = Heights[i];

            min = math.min(min, value);
            max = math.max(max, value);
        }

        minHeight.Value = min;
        maxHeight.Value = max;
    }
}
