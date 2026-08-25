using UnityEngine;
using System.Collections.Generic;
using Project.Singleton;
using Unity.Mathematics;

namespace WorldGen.Terrain
{
    // ChunkManager.Streaming.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        [Header("Streaming")]
        [SerializeField] TerrainStreamingProfile StreamProfile;
        [SerializeField] float updateInterval = 0.25f;
        [SerializeField] float cullPadding = 2f;
        private Camera cam;
        private float timer;
        private Vector2Int currentChunk = new Vector2Int(int.MinValue, int.MaxValue);
        private Vector2Int currentTerrainRegion = new Vector2Int(int.MinValue, int.MaxValue);

        public void SetStreamingSettings(TerrainStreamingProfile settings) => StreamProfile = settings;

        private bool UpdateCameraChunkRegion()
        {
            var hasChanged = false;

            var newChunk = WorldToChunkCoord(camPos);
            if (newChunk != currentChunk)
            {
                currentChunk = newChunk;
                hasChanged = true;
            }

            var newRegion = WorldToTerrainCoord(camPos);
            if (newRegion != currentTerrainRegion)
            {
                currentTerrainRegion = newRegion;
                hasChanged = true;
            }

            return hasChanged;
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

            activeChunkIds.Add(key);
        }

        private void BuildTerrain(Vector2Int key)
        {
            if(!terrainDataByID.TryGetValue(key, out TerrainData data))
            {
                data = CreateTerrainData(key);
            }

            if(!activeTerrainViews.TryGetValue(key, out TerrainView view))
            {
                view = CreateTerrainView(data, Noise);
            }

            activeTerrainIds.Add(key);
        }

        private ChunkView CreateChunkView(ChunkData data, NoiseProfile noise)
        {
            ChunkView view = chunkViewPool.Get();

            var t = view.gameObject.transform;
            t.position = data.WorldPosition;
            t.SetParent(transform);

            var go = view.gameObject;
            go.name = $"Chunk_({data.Coord.x},{data.Coord.y})";
            go.SetActive(true);
            
            view.Bind(data);

            int dx = Mathf.Abs(data.Coord.x - currentChunk.x);
            int dz = Mathf.Abs(data.Coord.y - currentChunk.y);
            int cd = Mathf.Max(dx, dz);

            var newLOD = GetChunkViewLOD(cd);

            GenerateChunkLOD(view, newLOD, data.Coord, noise);

            activeChunkViews[data.Coord] = view;

            return view;
        }

        private TerrainView CreateTerrainView(TerrainData data, NoiseProfile noise)
        {
            TerrainView view = terrainViewPool.Get();

            var t = view.gameObject.transform;
            t.position = data.WorldPosition;
            t.SetParent(transform);

            var go = view.gameObject;
            go.name = $"Terrain_({data.RegionID.x},{data.RegionID.y})";
            go.SetActive(true);

            view.Bind(data);

            GenerateTerrain(view, data.RegionID, noise);

            activeTerrainViews[data.RegionID] = view;

            return view;
        }

        private ChunkData CreateChunkData(Vector2Int key)

        {
            var worldPosition = ChunkCoordToWorld(key);

            float size = ChunkSettings.SizeInUnits;

            var chunk = new ChunkData
            {
                Coord = key,
                WorldPosition = worldPosition,
                CullData = new CullData
                {
                    Visible = false,
                    Center = new Vector3(worldPosition.x + size * 0.5f + cullPadding, 50f, worldPosition.z + size * 0.5f + cullPadding),
                    Radius = new Vector3(size * 0.5f, 50f, size * 0.5f).magnitude
                }
            };

            chunkDataByID[key] = chunk;

            return chunk;
        }

        private TerrainData CreateTerrainData(Vector2Int key)
        {
            var worldPosition = TerrainCoordToWorld(key);

            var size = TerrainSettings.SizeInUnits;

            var terrain = new TerrainData
            {
                RegionID = key,
                WorldPosition = worldPosition,
                CullData = new CullData
                {
                    Visible = false,
                    Center = new Vector3(worldPosition.x + size * 0.5f + cullPadding, 50f, worldPosition.z + size * 0.5f + cullPadding),
                    Radius = new Vector3(size * 0.5f, 50f, size * 0.5f).magnitude
                }
            };

            terrainDataByID[key] = terrain;

            return terrain;
        }

        private void GenerateChunkLOD(ChunkView view, int lod, Vector2Int id, NoiseProfile noise)
        {
            var lodMeshData = view.GetLODMeshData(lod);

            view.GetOrCreateMesh(lodMeshData);

            float2 worldOrigin = new float2(id.x * ChunkSettings.SizeInUnits, id.y * ChunkSettings.SizeInUnits);

            var ticket = ScheduleMeshJobs(id, worldOrigin, lodMeshData.Mesh, ChunkSettings.ChunkVerticies, lodMeshData.Verts, lodMeshData.Stride, ChunkSettings.SizeInUnits, noise);

            ticket.OnComplete = t =>
            {
                OnChunkMeshCompleted(ticket, id, lod);
            };

            meshTickets.Add(ticket);
        }

        private void GenerateTerrain(TerrainView view, Vector2Int id, NoiseProfile noise)
        {
            float2 worldOrigin = new float2(id.x * TerrainSettings.SizeInUnits, id.y * TerrainSettings.SizeInUnits);

            var ticket = ScheduleMeshJobs(id, worldOrigin, view.Mesh, TerrainSettings.TerrainVerticies, TerrainSettings.TerrainVerticies, 1, TerrainSettings.SizeInUnits, noise);

            ticket.OnComplete = OnTerrainMeshCompleted;

            meshTickets.Add(ticket);
        }

        private void RefreshWantedChunks(bool update)
        {
            timer -= Time.deltaTime;
            if (timer >= 0f || update == false) return;
            timer = updateInterval;

            wantedChunkIds.Clear();
            wantedChunkOrder.Clear();
            removeChunkIds.Clear();

            int chunkViewRadius = StreamProfile.StreamingSettings.ChunkViewRadius;

            for(int radius = 0; radius <= chunkViewRadius; radius++) // loops over closest chunks -> furthest
            {
                for(int disx = -radius; disx <= radius; disx++) // top / bottom rows
                {
                    AddWantedChunk(disx, -radius);
                    AddWantedChunk(disx, radius);
                }

                for(int disz = -radius + 1; disz <= radius - 1; disz++) // left / right
                {
                    AddWantedChunk(-radius, disz);
                    AddWantedChunk(radius, disz);
                }
            }

            int dx;
            int dz;
            int cd;
            int newLOD;
            ChunkView view;

            foreach (var id in activeChunkIds)
            {
                dx = Mathf.Abs(id.x - currentChunk.x);
                dz = Mathf.Abs(id.y - currentChunk.y);
                cd = Mathf.Max(dx, dz);

                view = activeChunkViews[id];
                newLOD = GetChunkViewLOD(cd);

                if(view.CurrentLOD != newLOD)
                {
                    if (view.MeshData[newLOD].GeneratedFor != id)
                    {
                        GenerateChunkLOD(view, newLOD, id, Noise);
                    }
                    else if(view.MeshData[newLOD].GeneratedFor == id)
                    {
                        view.SetLOD(newLOD);
                    }
                }

                if (view.MeshCollider.sharedMesh == null && cd <= StreamProfile.QualitySettings.ChunkColliderBuildRadius && 
                    view.CurrentLOD == ChunkSettings.ColliderLevelOfDetail && collidersToBuild.Add(id))
                {
                    colliderBuildQueue.Enqueue(id);
                }

                if (cd <= chunkViewRadius)
                {
                    if(!view.gameObject.activeSelf) view.gameObject.SetActive(true);
                }
                else if (cd <= StreamProfile.StreamingSettings.ChunkKeepRadius)
                {
                    if (view.gameObject.activeSelf) view.gameObject.SetActive(false);
                }
                else
                {
                    removeChunkIds.Add(id);
                }
            }
                

            foreach (var id in removeChunkIds)
            {
                view = activeChunkViews[id];

                chunkViewPool.Return(view);

                activeChunkViews.Remove(id);
                activeChunkIds.Remove(id);
            }

            foreach (var id in wantedChunkOrder)
            {
                if (chunkDataByID.TryGetValue(id, out var chunkData))
                {
                    if(activeChunkIds.Add(id))
                    {
                        CreateChunkView(chunkData, Noise);
                        continue;
                    }

                    view = activeChunkViews[id];

                    if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
                }
                else if(queuedChunkIds.Add(id))
                {
                    chunkBuildQueue.Enqueue(id);
                }
            }
        }

        //private void RefreshWantedTerrain()
        //{
        //    wantedTerrainIds.Clear();
        //    wantedTerrainOrder.Clear();
        //    removeTerrainIds.Clear();

        //    int terrainRadius = StreamProfile.StreamingSettings.TerrrainViewRadius;
        //    int chunkRadius = StreamProfile.StreamingSettings.ChunkViewRadius;

        //    int totalRadiusInChunks = chunkRadius + (terrainRadius * TerrainSettings.TerrainRegionSizeInChunks);

        //    for(int z = -totalRadiusInChunks; z <= totalRadiusInChunks; z++)
        //    {
        //        for(int x = -totalRadiusInChunks; x <= totalRadiusInChunks; x++)
        //        {
        //            int cd = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));

        //            if (cd <= chunkRadius) continue;

        //            int chunkX = currentChunk.x + x;
        //            int chunkZ = currentChunk.y + z;

        //            var regionID = ChunkIDToTerrainRegion(chunkX, chunkZ);
                    
        //            wantedTerrainIds.Add(regionID);
        //        }
        //    }

        //    int keepRadius = totalRadiusInChunks + StreamProfile.StreamingSettings.TerrrainKeepRadius;

        //    foreach (var id in activeTerrainIds)
        //    {
        //        var centerChunkID = TerrainIdToCenterChunkId(id, TerrainSettings.TerrainRegionSizeInChunks);

        //        int dx = Mathf.Abs(centerChunkID.x - currentChunk.x);
        //        int dz = Mathf.Abs(centerChunkID.y - currentChunk.y);
        //        int distance = Mathf.Max(dx, dz);

        //        var view = activeTerrainViews[id];

        //        if(distance <= chunkRadius)
        //        {
        //            removeTerrainIds.Add(id);
        //        }
        //        else if(distance <= totalRadiusInChunks)
        //        {
        //            if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
        //        }
        //        else if(distance < keepRadius)
        //        {
        //            if (view.gameObject.activeSelf) view.gameObject.SetActive(false);
        //        }
        //        else
        //        {
        //            removeTerrainIds.Add(id);
        //        }
        //    }

        //    foreach(var id in removeTerrainIds)
        //    {
        //        var view = activeTerrainViews[id];

        //        terrainViewPool.Return(view);

        //        activeTerrainViews.Remove(id);
        //        activeTerrainIds.Remove(id);
        //    }

        //    foreach (var id in wantedTerrainIds)
        //    {
        //        if(terrainDataByID.TryGetValue(id, out var terrainData))
        //        {
        //            if (activeTerrainIds.Add(id))
        //            {
        //                CreateTerrainView(terrainData, Noise);
        //                continue;
        //            }

        //            var view = activeTerrainViews[id];

        //            if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
        //        }
        //        else if(queuedTerrainIds.Add(id))
        //        {
        //            terrainBuildQueue.Enqueue(id);
        //        }
        //    }
        //}

        private Vector2Int ChunkIDToTerrainRegion(int x, int z)
        {
            int size = TerrainSettings.TerrainRegionSizeInChunks;

            int regionX = Mathf.FloorToInt((float)x / size);
            int regionZ = Mathf.FloorToInt((float)z / size);

            return new Vector2Int(regionX, regionZ);
        }

        //private Vector2Int TerrainIdToCenterChunkId(Vector2Int id, int chunksPerTerrainRegion)
        //{
        //    return new Vector2Int(id.x * chunksPerTerrainRegion + chunksPerTerrainRegion / 2,
        //                          id.y * chunksPerTerrainRegion + chunksPerTerrainRegion / 2);
        //}

        //private void AddWantedTerrain(int dx, int dz)
        //{
        //    var id = new Vector2Int(currentTerrainRegion.x + dx, currentTerrainRegion.y + dz);

        //    if (wantedTerrainIds.Add(id)) wantedTerrainOrder.Add(id);
        //}

        private void AddWantedChunk(int dx, int dz)
        {
            var id = new Vector2Int(currentChunk.x + dx, currentChunk.y + dz);

            if(wantedChunkIds.Add(id)) wantedChunkOrder.Add(id);
        }

        private void BuildQueuedChunks()
        {
            float startTime = Time.realtimeSinceStartup;

            Vector2Int id;
            int dx;
            int dz;
            int cd;

            while (chunkBuildQueue.Count > 0)
            {
                if (Time.realtimeSinceStartup - startTime > 0.01f) break;

                id = chunkBuildQueue.Dequeue();
                if (activeChunkIds.Contains(id)) continue;

                dx = Mathf.Abs(id.x - currentChunk.x);
                dz = Mathf.Abs(id.y - currentChunk.y);
                cd = Mathf.Max(dx, dz);

                if (cd > StreamProfile.StreamingSettings.ChunkViewRadius) continue;

                BuildChunk(id);
                UpdateChunkVisibilty(chunkDataByID[id]);
            }
        }

        //private void BuildQueuedTerrain()
        //{
        //    float startTime = Time.realtimeSinceStartup;

        //    Vector2Int id;

        //    while (terrainBuildQueue.Count > 0)
        //    {
        //        if (Time.realtimeSinceStartup - startTime > 0.01f) break;

        //        id = terrainBuildQueue.Dequeue();
        //        if (activeTerrainIds.Contains(id)) continue;

        //        BuildTerrain(id);
        //        //terrainLoaded++;
        //    }
        //}

        private void BuildQueuedColliders()
        {
            float startTime = Time.realtimeSinceStartup;

            Vector2Int id;
            int dx;
            int dz;
            int cd;

            while (colliderBuildQueue.Count > 0)
            {
                if (Time.realtimeSinceStartup - startTime > 0.01f) break;

                id = colliderBuildQueue.Dequeue();
                collidersToBuild.Remove(id);

                if (!activeChunkViews.TryGetValue(id, out var view)) continue;
                
                dx = Mathf.Abs(id.x - currentChunk.x);
                dz = Mathf.Abs(id.y - currentChunk.y);

                cd = Mathf.Max(dx, dz);

                if (cd > StreamProfile.QualitySettings.ChunkColliderBuildRadius) continue;

                if (view.MeshCollider.sharedMesh != null) continue;

                view.BakeMeshCollider();
            }
        }
    }
}