using System.Collections.Generic;
using Project.Singleton;
using UnityEngine;
using UnityEngine.Profiling;

namespace WorldGen.Terrain
{
    // ChunkManager.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        [Header("Data Structures")]
        readonly Dictionary<Vector2Int, ChunkData> chunkDataByID = new();
        readonly Dictionary<Vector2Int, ChunkView> activeChunkViews = new();
        readonly HashSet<Vector2Int> activeIds = new();
        readonly HashSet<Vector2Int> wantedIds = new();
        readonly HashSet<Vector2Int> removeIds = new();
        readonly HashSet<Vector2Int> queuedIds = new();
        readonly HashSet<Vector2Int> collidersToBuild = new();
        readonly HashSet<Vector2Int> visibleChunks = new();
        readonly Queue<Vector2Int> buildQueue = new();
        readonly Queue<Vector2Int> colliderBuildQueue = new();
        readonly List<MeshTicket> meshTickets = new();

        public int WorldSeed { get; private set; }
        public float SeedOffsetX;
        public float SeedOffsetZ;
        [SerializeField] GameObject chunkPrefab;
        [SerializeField] private NoiseProfile Noise;

        private ObjectPool<ChunkView> chunkViewPool;

        private void Awake()
        {
            InitalizeSeeds();

            CreatePools();

            cam = Camera.main;
        }

        private void Update() 
        {
            CacheCamera();

            UpdateCameraChunk();
            RefreshWantedChunks();
            BuildQueuedChunks(batchAmountChunks);
            FinalizeMeshTickets();
            BuildQueuedColliders(batchAmountColliders);
        }

        private void LateUpdate()
        {
            UpdateVisibility();
        }

        private void InitalizeSeeds()
        {
            WorldSeed = Random.Range(int.MinValue, int.MaxValue);

            System.Random rand = new System.Random(WorldSeed);

            SeedOffsetX = (float)rand.NextDouble() * 100f;
            SeedOffsetZ = (float)rand.NextDouble() * 100f;
        }

        private void CreatePools()
        {
            chunkViewPool = new ObjectPool<ChunkView>(
                create: OnCreateChunkView,
                maxPoolSize: 1000,
                reset: OnReturnChunkView,
                dispose: OnDestroyChunkView);

            chunkViewPool.PreWarm((chunkViewRadius * 2) + 1);
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
            var keys = new List<Vector2Int>(chunkDataByID.Keys);
            foreach(var k in keys)
            {
                var c = chunkDataByID[k];
                chunkDataByID.Remove(k);
            }

            activeIds.Clear();
            visibleChunks.Clear();
        }

        private ChunkView OnCreateChunkView()
        {
            return Instantiate(chunkPrefab).GetComponent<ChunkView>();
        }

        private void OnReturnChunkView(ChunkView view)
        {
            view.Unbind();
            view.gameObject.SetActive(false);
        }

        private void OnDestroyChunkView(ChunkView view)
        {
            Destroy(view.gameObject);
        }
    }
}