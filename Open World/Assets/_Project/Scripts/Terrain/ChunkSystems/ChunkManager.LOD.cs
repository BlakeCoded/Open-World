using UnityEngine;
using Project.Singleton;


namespace WorldGen.Terrain
{
    // ChunkManager.LOD.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        private int GetChunkViewLOD(int chunkDistance)
        {
            var settings = StreamProfile.QualitySettings;

            var lod = chunkDistance switch
            {
                var d when d <= settings.LOD0MaxDistance => 0,
                var d when d <= settings.LOD1MaxDistance => 1,
                var d when d <= settings.LOD2MaxDistance => 2,
                _ => 3,
            };

            return lod;
        }
    }
}