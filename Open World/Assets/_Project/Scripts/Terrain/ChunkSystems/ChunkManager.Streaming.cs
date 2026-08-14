using Project.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

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

        [SerializeField] GameObject chunkPrefab;

        Camera cam;

        float timer;
        Vector2Int currentChunk;

        private void UpdateCameraChunk()
        {
            var newChunk = WorldToCoord(camPos);
            if (newChunk != currentChunk) currentChunk = newChunk;
        }

        private void BuildChunk(Vector2Int key)
        {
            if (!chunkDataByID.TryGetValue(key, out ChunkData data))
            {
                data = CreateChunkData(key);
            }

            if(!activeChunkViews.TryGetValue(key, out ChunkView view))
            {
                view = CreateChunkView(data);
            }

            activeIds.Add(key);
        }

        private ChunkView CreateChunkView(ChunkData data)
        {
            ChunkView view = chunkViewPool.Get();

            TerrainHeightGenerator.FillHeights(view.Heights, data.Coord);

            TerrainMeshGenerator.FillVerticies(view.Vertices, view.Heights);

            TerrainMeshGenerator.FillNormals(view.Heights, view.Normals);

            view.Mesh.SetVertices(view.Vertices);
            view.Mesh.SetNormals(view.Normals);
            view.Mesh.RecalculateBounds();

            view.Bind(data);

            var t = view.gameObject.transform;
            var go = view.gameObject;

            t.position = data.WorldPosition;
            t.SetParent(transform);

            go.name = $"Chunk_({data.Coord.x},{data.Coord.y})";
            go.SetActive(true);

            activeChunkViews[data.Coord] = view;

            return view;
        }

        private ChunkData CreateChunkData(Vector2Int key)
        {
            var worldPosition = CoordToWorld(key);

            var chunk = new ChunkData
            {
                Coord = key,
                WorldPosition = worldPosition,
                CullData = new ChunkCullData
                {
                    Visible = false,
                    Center = new Vector3(worldPosition.x + ChunkSettings.ChunkSizeInUnits * 0.5f + cullPadding, 50f, worldPosition.z + ChunkSettings.ChunkSizeInUnits * 0.5f + cullPadding),
                    Radius = new Vector3(ChunkSettings.ChunkSizeInUnits * 0.5f, 50f, ChunkSettings.ChunkSizeInUnits * 0.5f).magnitude
                }
            };

            chunkDataByID[key] = chunk;

            return chunk;
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
                var view = activeChunkViews[id];

                view.Unbind();

                chunkViewPool.Return(view);

                activeChunkViews.Remove(id);
                activeIds.Remove(id);
            }

            foreach (var id in wantedIds)
            {
                if (chunkDataByID.TryGetValue(id, out var chunkData))
                {
                    if (activeIds.Add(id))
                    {
                        CreateChunkView(chunkData);
                    }
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
                UpdateChunkVisibilty(chunkDataByID[id]);
                count++;
            }
        }
    }
}