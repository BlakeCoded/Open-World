using Unity.Mathematics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct TerrainVertex
{
    public float3 Position;
    public float3 Normal;
    public float2 UV;
}