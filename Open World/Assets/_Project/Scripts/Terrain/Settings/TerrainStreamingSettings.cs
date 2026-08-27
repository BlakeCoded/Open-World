using System;
using UnityEngine;

[Serializable]
public struct TerrainStreamingSettings
{
    public int ChunkViewRadius;
    public int ChunkKeepRadius;
    public float ChunkSizeInUnits;
}
