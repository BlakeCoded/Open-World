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
        readonly HashSet<Vector2Int> activeChunkIds = new();
        readonly HashSet<Vector2Int> wantedChunkIds = new();
        readonly List<Vector2Int> wantedChunkOrder = new();
        readonly HashSet<Vector2Int> removeChunkIds = new();
        readonly HashSet<Vector2Int> queuedChunkIds = new();
        readonly HashSet<Vector2Int> collidersToBuild = new();
        readonly HashSet<Vector2Int> visibleChunks = new();
        readonly Queue<Vector2Int> chunkBuildQueue = new();
        readonly Queue<Vector2Int> colliderBuildQueue = new();
        readonly List<MeshTicket> meshTickets = new();
        private ObjectPool<ChunkView> chunkViewPool;
        private uint generationID = 0;

        // Seed
        [SerializeField] private bool generateSeed;
        public int WorldSeed;
        [HideInInspector] public float SeedOffsetX;
        [HideInInspector] public float SeedOffsetZ;

        // Prefab
        [SerializeField] GameObject chunkPrefab;

        // Noise
        [SerializeField] private NoiseProfile Noise;

        // Timer
        [SerializeField] private TimeSampler timeSampler;
        private bool hastime = false;
        private int meshesLoaded = 0;

        private void Awake()
        {
            cam = Camera.main;

            InitalizeSeeds();
        }

        private void Start()
        {
            ApplyTerrainStreamingProfile();

            timeSampler.StartTimer();

            CreatePools();

            GenerateChunks();
        }

        private void Update() 
        {
            CacheCamera();

            RefreshChunkDelta();
            
            BuildQueuedChunks();
            FinalizeMeshTickets();

            BuildQueuedColliders();

            if(hastime == false)
            {
                int value = StreamProfile.StreamingSettings.ChunkViewRadius * 2;
                if (meshesLoaded >= (value * value))
                {
                    timeSampler.StopTimer();
                    hastime = true;
                }
            }
        }

        private void LateUpdate()
        {
            UpdateVisibility();
        }

        private void ApplyTerrainStreamingProfile()
        {
            var stream = StreamProfile.StreamingSettings;
            var quality = StreamProfile.QualitySettings;
            
            ChunkSettings.SizeInUnits = stream.ChunkSizeInUnits;
            ChunkSettings.ChunkVerticies = quality.ChunkVerts;
            ChunkSettings.MeshLevelsOfDetail = quality.ChunkLODCount;
            ChunkSettings.ColliderLevelOfDetail = quality.ColliderLevelOfDetail;
            ChunkSettings.TextureScale = quality.TextureScale;
        }

        private void InitalizeSeeds()
        {
            if(generateSeed == true) WorldSeed = Random.Range(int.MinValue, int.MaxValue);

            System.Random rand = new System.Random(WorldSeed);

            SeedOffsetX = (float)rand.NextDouble() * 100f;
            SeedOffsetZ = (float)rand.NextDouble() * 100f;
        }

        private void CreatePools()
        {
            chunkViewPool = new ObjectPool<ChunkView>(
                create: OnCreateChunkView,
                maxPoolSize: 10000,
                reset: OnReturnChunkView,
                dispose: OnDestroyChunkView);
        }

        private Vector2Int WorldToChunkCoord(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / ChunkSettings.SizeInUnits),
                                  Mathf.FloorToInt(position.z / ChunkSettings.SizeInUnits));
        }

        private Vector3 ChunkCoordToWorld(Vector2Int coord)
        {
            return new Vector3(coord.x * ChunkSettings.SizeInUnits, 
                               0f, 
                               coord.y * ChunkSettings.SizeInUnits);
        }

        public void OnReload()
        {
            generationID++;

            ResetStreaming();
        }

        private void ResetStreaming()
        {
            activeChunkIds.Clear();
            wantedChunkIds.Clear();
            wantedChunkOrder.Clear();

            foreach (var pair in activeChunkViews)
            {
                chunkViewPool.Return(pair.Value);
            }

            activeChunkViews.Clear();

            GenerateChunks();
        }
        
        private void OnChunkMeshCompleted(MeshTicket t, int lod)
        {
            if(!activeChunkViews.TryGetValue(t.ID, out var view)) return;

            var meshData = view.GetLODMeshData(lod);

            var centerY = (t.maxHeight.Value + t.minHeight.Value) * 0.5f;
            var height = t.maxHeight.Value - t.minHeight.Value;
            var size = ChunkSettings.SizeInUnits;

            meshData.Mesh.bounds = new Bounds(
                new Vector3(size * 0.5f, centerY, size * 0.5f),
                new Vector3(size, height, size)); 

            meshData.GeneratedFor = t.ID;

            view.IsVisible = true;
            view.SetLOD(lod);

            if(lod == ChunkSettings.ColliderLevelOfDetail)
            {
                int dx = t.ID.x - currentChunk.x;
                int dz = t.ID.y - currentChunk.y;

                if(dx < 0) dx = -dx;
                if(dz < 0) dz = -dz;

                int distance = dx > dz ? dx : dz;

                if(distance <= StreamProfile.QualitySettings.ChunkColliderBuildRadius)
                {
                    if(view.MeshCollider.sharedMesh == null)
                    {
                        if(collidersToBuild.Add(t.ID))
                        {
                            colliderBuildQueue.Enqueue(t.ID);
                        }
                    }
                }
            }
        }

        private ChunkView OnCreateChunkView()
        {
            var view = Instantiate(chunkPrefab).GetComponent<ChunkView>();

            view.Configure();

            return view;
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

        private void OnDisable()
        {
            var chunkDataKeys = new List<Vector2Int>(chunkDataByID.Keys);
            foreach(var k in chunkDataKeys)
            {
                var c = chunkDataByID[k];
                chunkDataByID.Remove(k);
            }

            activeChunkIds.Clear();
            visibleChunks.Clear();
        }
    }
}