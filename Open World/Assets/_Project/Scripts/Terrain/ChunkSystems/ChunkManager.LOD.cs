using JetBrains.Annotations;
using Project.Singleton;
using UnityEngine;


namespace WorldGen.Terrain
{
    // ChunkManager.LOD.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        [Header("LOD")]
        [SerializeField] int lod1StartRadius = 4;
        [SerializeField] int lod2StartRadius = 10;

        private int GetChunkViewLOD(int chunkDistance)
        {
            int lod = chunkDistance switch
            {
                var d when d >= lod2StartRadius => 2,
                var d when d >= lod1StartRadius => 1,
                _ => 0
            };

            return lod;
        }
    }
}