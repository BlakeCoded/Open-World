using System;

[Serializable]
public struct TerrainQualitySettings
{
    public int ChunkVerts;
    public int ChunkLODCount;
    public int LOD0MaxDistance;
    public int LOD1MaxDistance;
    public int LOD2MaxDistance;
    public int ChunkColliderBuildRadius;
    public int ColliderLevelOfDetail;
    public float TextureScale;
}