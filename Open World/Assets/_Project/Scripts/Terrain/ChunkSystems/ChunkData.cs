using UnityEngine;

namespace WorldGen.Terrain
{
    public class ChunkData
    {
        public Mesh Mesh;

        public Vector2Int Coord;
        public Vector3 WorldPosition;

        public ChunkCullData CullData;

        // Unused
        public bool Modified;
    }
}