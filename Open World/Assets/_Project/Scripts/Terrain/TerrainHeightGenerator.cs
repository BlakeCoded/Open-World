using UnityEngine;

namespace WorldGen.Terrain
{
    public static class TerrainHeightGenerator
    {
        public static float[] CreateHeights(float size, int verts, Vector2Int chunkCoord)
        {
            int borderedVerts = verts + 2;

            float[] heights = new float[borderedVerts * borderedVerts];

            int index = 0;
            float step = size / (verts - 1);

            for(int z = -1; z <= verts; z++)
                for(int x = -1; x <= verts; x++)
                {
                    float worldX = chunkCoord.x * size + x * step;
                    float worldZ = chunkCoord.y * size + z * step;

                    heights[index++] = SampleHeight(worldX, worldZ);
                }

            return heights;
        }

        const float NoiseOffset = 10000f;

        public static float SampleHeight(float worldX, float worldZ)
        {
            float noiseScale = 0.035f;
            float heightMultiplier = 20f;

            return Mathf.PerlinNoise((worldX + NoiseOffset) * noiseScale, (worldZ + NoiseOffset) * noiseScale) * heightMultiplier;
        }
    }
}