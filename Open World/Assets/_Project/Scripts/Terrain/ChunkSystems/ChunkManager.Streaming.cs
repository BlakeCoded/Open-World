using Project.Singleton;
using Unity.Mathematics;
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

            var newLOD = GetChunkViewLOD(cd);

            GenerateLOD(view, newLOD, data.Coord);

            if(view.MeshCollider.sharedMesh == null && view.CurrentLOD == ChunkSettings.ColliderLevelOfDetail && 
                cd <= ColliderBuildRadius && collidersToBuild.Add(data.Coord))
            {
                colliderBuildQueue.Enqueue(data.Coord);
            }

            view.Bind(data);

            activeChunkViews[data.Coord] = view;

            return view;
        }

        private void GenerateLOD(ChunkView view, int lod, Vector2Int id)
        {
            var lodMeshData = view.GetLODMeshData(lod);

            TerrainHeightGenerator.FillHeights(lodMeshData.Heights, lodMeshData.Verts, lodMeshData.Stride, id, Noise);
            TerrainMeshGenerator.FillVerticies(lodMeshData.Vertices, lodMeshData.Heights, lodMeshData.Verts, lodMeshData.Stride);
            //TerrainMeshGenerator.FillNormals(lodMeshData.Normals, lodMeshData.Heights, lodMeshData.Verts);

            lodMeshData.GeneratedFor = id;
            lodMeshData.Mesh.SetVertices(lodMeshData.Vertices);
            //lodMeshData.Mesh.SetNormals(lodMeshData.Normals);
            lodMeshData.Mesh.RecalculateNormals();
            lodMeshData.Mesh.RecalculateBounds();

            view.CurrentLOD = lod;
            view.SetLOD(lod);
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
                var newLOD = GetChunkViewLOD(cd);

                if(view.CurrentLOD != newLOD)
                {
                    view.CurrentLOD = newLOD;

                    if (view.MeshData[newLOD].GeneratedFor != id)
                    {
                        GenerateLOD(view, newLOD, id);
                        view.MeshData[newLOD].GeneratedFor = id;
                    }

                    view.SetLOD(newLOD);
                }

                if (view.MeshCollider.sharedMesh == null && cd <= ColliderBuildRadius && 
                    view.CurrentLOD == ChunkSettings.ColliderLevelOfDetail && collidersToBuild.Add(id))
                {
                    colliderBuildQueue.Enqueue(id);
                }

                if (cd <= chunkViewRadius)
                {
                    view.gameObject.SetActive(true);
                }
                else if (cd <= keepChunkRadius)
                {
                    view.gameObject.SetActive(false);
                }
                else
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

                Profiler.BeginSample("Build Chunk");
                BuildChunk(id);
                Profiler.EndSample();
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

                view.BakeMeshCollider();

                count++;
            }
        }
    }
}