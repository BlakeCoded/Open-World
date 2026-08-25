using UnityEngine;
using Project.Singleton;


namespace WorldGen.Terrain
{
    // ChunkManager.LOD.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        [SerializeField] private float[] lodWeights = { 1f, 1f, 2f, 4f };
        //private int GetChunkViewLOD(int chunkDistance)
        //{
        //    int lodCount = StreamProfile.QualitySettings.ChunkLODCount;

        //    if (lodCount <= 1) return 0;

        //    float radius = StreamProfile.StreamingSettings.ChunkViewRadius;

        //    int lod = Mathf.FloorToInt(chunkDistance / radius * lodCount);

        //    return Mathf.Clamp(lod, 0, lodCount - 1);
        //}

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