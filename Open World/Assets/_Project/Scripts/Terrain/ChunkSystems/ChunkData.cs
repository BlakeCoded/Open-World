using UnityEngine;

namespace WorldGen.Terrain
{
    public class ChunkData
    {
        public Vector2Int Coord;
        public Vector3 WorldPosition;
        public CullData CullData;

        // Unused
        public bool Modified;
    }
}