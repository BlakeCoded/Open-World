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
        readonly HashSet<Vector2Int> visibleChunks = new();
        readonly Queue<Vector2Int> buildQueue = new();

        [SerializeField] private GameObject Player;

        private ObjectPool<ChunkView> chunkViewPool;

        private void Awake()
        {
            cam = Camera.main;
            InitalizeSeeds();

            chunkViewPool = new ObjectPool<ChunkView>(
                create: OnChunkViewCreate,
                maxPoolSize: 1000,
                reset: OnChunkViewReset,
                dispose: OnChunkViewDestroy);
        }

        private void Update() 
        {
            CacheCamera();

            UpdateCameraChunk();
            RefreshWantedChunks();
            BuildQueuedChunks(batchAmount);
        }

        private void LateUpdate()
        {
            UpdateVisibility();

            //long allocated = Profiler.GetTotalAllocatedMemoryLong();
            //long reserved = Profiler.GetTotalReservedMemoryLong();
            //long mono = Profiler.GetMonoUsedSizeLong();

            //Debug.Log(
            //    $"Memory | " +
            //    $"Allocated: {allocated / 1048576f:F1} MB | " +
            //    $"Reserved: {reserved / 1048576f:F1} MB | " +
            //    $"Managed: {mono / 1048576f:F1} MB"
            //);
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
            var keys = new List<Vector2Int>(chunkDataByID.Keys);
            foreach(var k in keys)
            {
                var c = chunkDataByID[k];
                chunkDataByID.Remove(k);
            }

            activeIds.Clear();
            visibleChunks.Clear();
        }

        private ChunkView OnChunkViewCreate()
        {
            return Instantiate(chunkPrefab).GetComponent<ChunkView>();
        }

        private void OnChunkViewReset(ChunkView view)
        {
            view.Unbind();
            view.gameObject.SetActive(false);
        }

        private void OnChunkViewDestroy(ChunkView view)
        {
            Destroy(view.gameObject);
        }
    }
}