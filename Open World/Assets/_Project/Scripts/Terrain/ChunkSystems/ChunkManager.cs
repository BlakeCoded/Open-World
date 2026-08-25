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
        // Chunk
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
        private ObjectPool<ChunkView> chunkViewPool;

        // Terrain
        readonly Dictionary<Vector2Int, TerrainData> terrainDataByID = new();
        readonly Dictionary<Vector2Int, TerrainView> activeTerrainViews = new();
        readonly HashSet<Vector2Int> activeTerrainIds = new();
        readonly HashSet<Vector2Int> wantedTerrainIds = new();
        readonly List<Vector2Int> wantedTerrainOrder = new();
        readonly HashSet<Vector2Int> removeTerrainIds = new();
        readonly HashSet<Vector2Int> queuedTerrainIds = new();
        readonly Queue<Vector2Int> terrainBuildQueue = new();
        private ObjectPool<TerrainView> terrainViewPool;

        // Global
        readonly List<MeshTicket> meshTickets = new();
        private uint generationID = 0;

        // Seed
        [SerializeField] private bool generateSeed;
        public int WorldSeed;
        [HideInInspector] public float SeedOffsetX;
        [HideInInspector] public float SeedOffsetZ;

        // Prefab
        [SerializeField] GameObject chunkPrefab;
        [SerializeField] GameObject terrainPrefab;

        // Noise
        [SerializeField] private NoiseProfile Noise;

        // Timer
        [SerializeField] private TimeSampler timeSampler;
        private bool hastime = false;
        private int meshesLoaded = 0;
        private int terrainLoaded = 0;

        private void Awake()
        {
            ApplyTerrainStreamingProfile();

            InitalizeSeeds();

            timeSampler.StartTimer();

            CreatePools();

            cam = Camera.main;
        }

        private void Update() 
        {
            CacheCamera();

            var update = UpdateCameraChunkRegion();
            RefreshWantedChunks(update);
            
            BuildQueuedChunks();
            FinalizeMeshTickets();

            BuildQueuedColliders();

            if(hastime == false)
            {
                int value = (StreamProfile.StreamingSettings.ChunkViewRadius * 2) + 1;
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

            // static variables no need to calculate or change
            ChunkSettings.SizeInUnits = stream.ChunkSizeInUnits;
            ChunkSettings.ChunkVerticies = quality.ChunkVerts;
            ChunkSettings.MeshLevelsOfDetail = quality.ChunkLODCount;
            ChunkSettings.ColliderLevelOfDetail = quality.ColliderLevelOfDetail;
            ChunkSettings.TextureScale = quality.TextureScale;

            int lowestStride = 1 << (quality.ChunkLODCount - 1);

            TerrainSettings.TerrainRegionSizeInChunks = stream.TerrainRegionSizeInChunks;
            TerrainSettings.SizeInUnits = stream.TerrainRegionSizeInChunks * stream.ChunkSizeInUnits;
            TerrainSettings.TerrainVerticies = GetTerrainVerts(quality.ChunkVerts, lowestStride, stream.TerrainRegionSizeInChunks);
        }

        private int GetTerrainVerts(int chunkMaxVerts, int lowestLODStride, int regionSizeInChunks)
        {
            int lowestLODVerts = (chunkMaxVerts - 1) / lowestLODStride + 1;

            int intervalsPerChunk = lowestLODVerts - 1;

            return intervalsPerChunk * regionSizeInChunks + 1;
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

            int value = (StreamProfile.StreamingSettings.ChunkViewRadius * 2) + 1;

            chunkViewPool.PreWarm(value * value);

            terrainViewPool = new ObjectPool<TerrainView>(
                create: OnCreateTerrainView,
                maxPoolSize: 100,
                reset: OnReturnTerrainView,
                dispose: OnDestroyTerrainView);

            //terrainViewPool.PreWarm(1);
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

        private Vector2Int WorldToTerrainCoord(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / TerrainSettings.SizeInUnits),
                                  Mathf.FloorToInt(position.z / TerrainSettings.SizeInUnits));
        }

        private Vector3 TerrainCoordToWorld(Vector2Int coord)
        {
            return new Vector3(coord.x * TerrainSettings.SizeInUnits,
                               0f,
                               coord.y * TerrainSettings.SizeInUnits);
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

            RefreshWantedChunks(true);
        }

        #region Chunk

        private void OnChunkMeshCompleted(MeshTicket t, Vector2Int id, int lod)
        {
            if(!activeChunkViews.TryGetValue(t.ID, out var view)) return;

            var meshData = view.GetLODMeshData(lod);

            meshData.GeneratedFor = id;

            view.SetLOD(lod);
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

        #endregion

        #region Terrain
        private void OnTerrainMeshCompleted(MeshTicket t)
        {
            if(!activeTerrainViews.TryGetValue(t.ID, out var view)) return;

            view.SetMesh();
        }
        
        private TerrainView OnCreateTerrainView()
        {
            var view = Instantiate(terrainPrefab).GetComponent<TerrainView>();

            view.Configure();

            return view;
        }

        private void OnReturnTerrainView(TerrainView view)
        {
            view.UnBind();
            view.gameObject.SetActive(false);
        }

        private void OnDestroyTerrainView(TerrainView view)
        {
            Destroy(view.gameObject);
        }

        #endregion

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

            var terrainDataKeys = new List<Vector2Int>(terrainDataByID.Keys);

            foreach(var k in terrainDataKeys)
            {
                var c = terrainDataByID[k];
                terrainDataByID.Remove(k);
            }

            activeTerrainIds.Clear();
        }
    }
}