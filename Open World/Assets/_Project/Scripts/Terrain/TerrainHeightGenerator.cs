using UnityEngine;

namespace Project.Terrain
{
    public static class TerrainHeightGenerator
    {
        public static float[] CreateHeights(float chunkSize, int verts, Vector2Int chunkCoord, float scale)
        {
            float[] heights = new float[verts * verts];

            int index = 0;

            for(int z = 0; z < verts; z++)
                for(int x = 0; x < verts; x++)
                {
                    float worldX = chunkCoord.x * chunkSize + x;
                    float worldY = chunkCoord.y * chunkSize + z;

                    heights[index++] = Mathf.PerlinNoise(worldX * scale, worldY * scale);
                }

            return heights;
        }
    }
}