using UnityEngine;
using System.Collections.Generic;
using Project.Singleton;

namespace WorldGen.Terrain
{
    // ChunkManager.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        [Header("Data Structures")]
        readonly Dictionary<Vector2Int, ChunkData> chunksById = new();
        readonly HashSet<Vector2Int> activeIds = new();
        readonly HashSet<Vector2Int> wantedIds = new();
        readonly HashSet<Vector2Int> removeIds = new();
        readonly HashSet<Vector2Int> queuedIds = new();
        readonly HashSet<Vector2Int> visibleChunks = new();
        readonly Queue<Vector2Int> buildQueue = new();

        [SerializeField] private GameObject Player;
        [SerializeField] private Material defaultMat;

        private void Awake()
        {
            cam = Camera.main;
            InitalizeSeeds();
        }

        private void Update() 
        {
            UpdateCameraChunk();
            RefreshWantedChunks();
            BuildQueuedChunks(batchAmount);
        }

        private void LateUpdate()
        {
            UpdateVisibility();
        }

        private void InitalizeSeeds()
        {
            int seed = Random.Range(int.MinValue, int.MaxValue);
            Random.InitState(seed);
        }

        private Vector2Int WorldToCoord(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / ChunkSettings.ChunkSizeInUnits),
                                  Mathf.FloorToInt(position.z / ChunkSettings.ChunkSizeInUnits));
        }

        private Vector3 CoordToWorld(Vector2Int coord)
        {
            return new Vector3(coord.x * ChunkSettings.ChunkSizeInUnits, 
                               0f, 
                               coord.y * ChunkSettings.ChunkSizeInUnits);
        }

        private void OnDisable()
        {
            var keys = new List<Vector2Int>(chunksById.Keys);
            foreach(var k in keys)
            {
                var c = chunksById[k];
                if (c.GameObject) Destroy(c.GameObject);
                chunksById.Remove(k);
            }

            activeIds.Clear();
            visibleChunks.Clear();
        }
    }
}