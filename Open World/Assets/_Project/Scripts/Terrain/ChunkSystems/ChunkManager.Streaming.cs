using Project.Singleton;
using UnityEngine;

namespace WorldGen.Terrain
{
    // ChunkManager.Streaming.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        [Header("Streaming")]
        [SerializeField] int chunkViewRadius = 2;
        [SerializeField] float updateInterval = 0.25f;
        [SerializeField] int batchAmount = 5;
        [SerializeField] float cullPadding = 2f;

        Camera cam;

        float timer;
        Vector2Int currentChunk;

        private void UpdateCameraChunk()
        {
            var newChunk = WorldToCoord(cam.transform.position);
            if (newChunk != currentChunk) currentChunk = newChunk;
        }

        private void BuildChunk(Vector2Int key)
        {
            if (chunksById.ContainsKey(key)) return;

            GameObject go = new GameObject($"Chunk_({key.x},{key.y})");

            float[] heights = TerrainHeightGenerator.CreateHeights(ChunkSettings.ChunkSizeInUnits, ChunkSettings.ChunkVerticies, key);

            Mesh mesh = TerrainMeshGenerator.CreateMeshTerrain(ChunkSettings.ChunkSizeInUnits, ChunkSettings.ChunkVerticies, heights);

            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();
            var MeshCollider = go.AddComponent<MeshCollider>();

            meshFilter.mesh = mesh;
            MeshCollider.sharedMesh = mesh;
            meshRenderer.material = defaultMat;

            var renderData = new ChunkRenderData
            {
                Mesh = mesh,
                MeshFilter = meshFilter,
                MeshRenderer = meshRenderer,
                MeshCollider = MeshCollider
            };

            var t = go.transform;
            t.parent = transform;
            t.position = CoordToWorld(key);

            var chunk = new ChunkData
            {
                Coord = key,
                GameObject = go,
                RenderData = renderData,
                CullData = new ChunkCullData
                {
                    Visible = true,
                    Center = new Vector3(t.position.x + ChunkSettings.ChunkSizeInUnits * 0.5f + cullPadding, 50f, t.position.z + ChunkSettings.ChunkSizeInUnits * 0.5f + cullPadding),
                    Radius = new Vector3(ChunkSettings.ChunkSizeInUnits * 0.5f, 50f, ChunkSettings.ChunkSizeInUnits * 0.5f).magnitude
                }
            };

            chunksById[key] = chunk;
            activeIds.Add(key);

            chunk.OnLoad();
        }

        private void RefreshWantedChunks()
        {
            timer -= Time.deltaTime;
            if (timer >= 0f) return;
            timer = updateInterval;

            wantedIds.Clear();
            removeIds.Clear();

            for (int dx = -chunkViewRadius; dx <= chunkViewRadius; dx++)
                for (int dz = -chunkViewRadius; dz <= chunkViewRadius; dz++)
                {
                    var id = new Vector2Int(currentChunk.x + dx, currentChunk.y + dz);
                    int cd = Mathf.Max(Mathf.Abs(dz), Mathf.Abs(dx));
                    if (cd <= chunkViewRadius) wantedIds.Add(id);
                }

            foreach (var id in activeIds)
                if (!wantedIds.Contains(id))
                    removeIds.Add(id);

            foreach (var id in removeIds)
            {
                chunksById[id].OnUnload();
                activeIds.Remove(id);
            }

            foreach (var id in wantedIds)
            {
                if (chunksById.TryGetValue(id, out var chunk))
                {
                    if (activeIds.Add(id)) chunk.OnLoad();
                }
                else if (queuedIds.Add(id))
                {
                    buildQueue.Enqueue(id);
                }
            }
        }

        private void BuildQueuedChunks(int batch)
        {
            int count = 0;
            while (count < batch && buildQueue.Count > 0)
            {
                var id = buildQueue.Dequeue();
                if (activeIds.Contains(id)) continue;

                BuildChunk(id);
                UpdateChunkVisibilty(chunksById[id]);
                count++;
            }
        }
    }
}