using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace WorldGen.Terrain
{
    public static class TerrainHeightGenerator
    {
        public static float[] CreateHeights(Vector2Int chunkCoord, int stride, NoiseProfile noise)
        {
            float size = ChunkSettings.SizeInUnits;
            int verts = (ChunkSettings.ChunkVerticies - 1) / stride + 1;
            int borderedVerts = verts + 2;

            float[] heights = new float[borderedVerts * borderedVerts];

            float fullStep = size / (ChunkSettings.ChunkVerticies - 1);
            float step = fullStep * stride;

            int index = 0;

            for (int z = -1; z <= verts; z++)
            {
                float worldZ = chunkCoord.y * size + z * step;
                for (int x = -1; x <= verts; x++)
                {
                    float worldX = chunkCoord.x * size + x * step;

                    heights[index++] = SampleHeight(worldX, worldZ, noise);
                }
            }

            return heights;
        }

        public static void FillHeights(float[] heights, int verts, int stride, Vector2Int chunkCoord, NoiseProfile noise)
        {
            float size = ChunkSettings.SizeInUnits;
            //int verts = (ChunkSettings.ChunkVerticies - 1) / stride + 1;

            float fullStep = size / (ChunkSettings.ChunkVerticies - 1);
            float step = fullStep * stride;

            int index = 0;

            for (int z = -1; z <= verts; z++)
            {
                float worldZ = chunkCoord.y * size + z * step;
                for (int x = -1; x <= verts; x++)
                {
                    float worldX = chunkCoord.x * size + x * step;

                    heights[index++] = SampleHeight(worldX, worldZ, noise);
                }
            }
        }

        public static float SampleHeight(float worldX, float worldZ, NoiseProfile noise)
        {
            float noiseX = worldX * noise.Scale + ChunkManager.Instance.SeedOffsetX;
            float noiseZ = worldZ * noise.Scale + ChunkManager.Instance.SeedOffsetZ;

            float height = Noise.FractalNoise(noiseX, noiseZ, noise.Octaves, noise.Lacunarity, noise.Persistence) * noise.HeightMultiplier;

            return height;
        }
    }
}