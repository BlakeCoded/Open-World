using Project.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

namespace WorldGen.Terrain
{
    // ChunkManager.Streaming.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        [Header("Streaming")]
        [SerializeField] int chunkViewRadius = 2;
        [SerializeField] int ColliderBuildRadius = 3;
        [SerializeField] float updateInterval = 0.25f;
        [SerializeField] int batchAmountChunks = 1;
        [SerializeField] int batchAmountColliders = 1;
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
                view = CreateChunkView(data, Noise);
            }

            activeIds.Add(key);
        }

        private ChunkView CreateChunkView(ChunkData data, NoiseProfile noise)
        {
            ChunkView view = chunkViewPool.Get();

            var go = view.gameObject;
            go.name = $"Chunk_({data.Coord.x},{data.Coord.y})";
            go.SetActive(true);

            var t = view.gameObject.transform;
            t.position = data.WorldPosition;
            t.SetParent(transform);

            int dx = Mathf.Abs(data.Coord.x - currentChunk.x);
            int dz = Mathf.Abs(data.Coord.y - currentChunk.y);
            int cd = Mathf.Max(dx, dz);

            int lod = GetChunkViewLOD(cd);

            TerrainHeightGenerator.FillHeights(view.Heights, data.Coord, noise, 1);
            TerrainMeshGenerator.FillVerticies(view.Vertices, view.Heights, 1);
            TerrainMeshGenerator.FillNormals(view.Normals, view.Heights, 1);

            view.Mesh.SetVertices(view.Vertices);
            view.Mesh.SetNormals(view.Normals);
            view.Mesh.RecalculateBounds();

            if(lod == ChunkSettings.ColliderLevelOfDetail && cd <= ColliderBuildRadius)
            {
                if (collidersToBuild.Add(data.Coord))
                {
                    colliderBuildQueue.Enqueue(data.Coord);
                }
            }

            //view.MeshCollider.sharedMesh = null;

            view.Bind(data);
            
            activeChunkViews[data.Coord] = view;

            return view;
        }

        private ChunkViewLODS CreateChunkViewLOD(ChunkData data, NoiseProfile noise)
        {
            ChunkViewLODS view = new ChunkViewLODS(); // imagine this is pooled.

            var go = view.gameObject;
            go.name = $"Chunk_({data.Coord.x},{data.Coord.y})";
            go.SetActive(true);

            var t = view.gameObject.transform;
            t.position = data.WorldPosition;
            t.SetParent(transform);

            int dx = Mathf.Abs(data.Coord.x - currentChunk.x);
            int dz = Mathf.Abs(data.Coord.y - currentChunk.y);
            int cd = Mathf.Max(dx, dz);

            view.CurrentLOD = GetChunkViewLOD(cd);
            var lodMeshData = view.GetLODMeshData(view.CurrentLOD);

            // generate heights
            TerrainHeightGenerator.FillHeights(lodMeshData.Heights, data.Coord, noise, lodMeshData.Stride);
            TerrainMeshGenerator.FillVerticies(lodMeshData.Vertices, lodMeshData.Heights, lodMeshData.Stride);
            TerrainMeshGenerator.FillNormals(lodMeshData.Normals, lodMeshData.Heights, lodMeshData.Stride);

            lodMeshData.Mesh.SetVertices(lodMeshData.Vertices);
            lodMeshData.Mesh.SetNormals(lodMeshData.Normals);
            lodMeshData.Mesh.RecalculateBounds();


            if (view.CurrentLOD == ChunkSettings.ColliderLevelOfDetail && cd <= ColliderBuildRadius)
            {
                if(collidersToBuild.Add(data.Coord))
                {
                    colliderBuildQueue.Enqueue(data.Coord);
                }
            }

            view.Bind(data);

            //activeChunkViews[data.Coord] = view; add view to active chunks

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

            int keepChunkRadius = chunkViewRadius + 4;

            foreach (var id in activeIds)
            {
                int dx = Mathf.Abs(id.x - currentChunk.x);
                int dz = Mathf.Abs(id.y - currentChunk.y);

                int cd = Mathf.Max(dx, dz);

                var view = activeChunkViews[id];

                if(cd <= ColliderBuildRadius)
                {
                    if (collidersToBuild.Add(id))
                    {
                        colliderBuildQueue.Enqueue(id);
                    }
                }

                if (cd <= chunkViewRadius)
                {
                    view.gameObject.SetActive(true);
                    continue;
                }
                else if (cd <= keepChunkRadius)
                {
                    view.gameObject.SetActive(false);
                    continue;
                }

                if (cd > keepChunkRadius)
                {
                    removeIds.Add(id);
                }
            }
                

            foreach (var id in removeIds)
            {
                var view = activeChunkViews[id];

                chunkViewPool.Return(view);

                activeChunkViews.Remove(id);
                activeIds.Remove(id);
            }

            foreach (var id in wantedIds)
            {
                if (chunkDataByID.TryGetValue(id, out var chunkData))
                {
                    if(activeIds.Add(id))
                    {
                        CreateChunkView(chunkData, Noise);
                        continue;
                    }

                    activeChunkViews[id].gameObject.SetActive(true);
                }
                else if(queuedIds.Add(id))
                {
                    buildQueue.Enqueue(id);
                }
            }
        }

        private void BuildQueuedChunks(int batch)
        {
            int count = 0;

            float startTime = Time.realtimeSinceStartup;

            while (count < batch && buildQueue.Count > 0)
            {
                if (Time.realtimeSinceStartup - startTime > 0.01f)
                {
                    //Debug.Log(count);
                    break;
                }

                var id = buildQueue.Dequeue();
                if (activeIds.Contains(id)) continue;

                var cd = id - currentChunk;
                if (Mathf.Max(Mathf.Abs(cd.x), Mathf.Abs(cd.y)) > chunkViewRadius) continue;

                //Profiler.BeginSample("Build Chunk");
                BuildChunk(id);
                //Profiler.EndSample();
                UpdateChunkVisibilty(chunkDataByID[id]);
                count++;
            }
        }

        private void BuildQueuedColliders(int batch)
        {
            int count = 0;

            while(count < batch && colliderBuildQueue.Count > 0)
            {
                var id = colliderBuildQueue.Dequeue();
                collidersToBuild.Remove(id);

                var view = activeChunkViews[id];

                var cd = view.Data.Coord - currentChunk;

                if (Mathf.Max(Mathf.Abs(cd.x), Mathf.Abs(cd.y)) > ColliderBuildRadius) continue;

                if (view.MeshCollider.sharedMesh != null) continue;

                view.BakeMeshCollider();

                count++;
            }
        }
    }
}