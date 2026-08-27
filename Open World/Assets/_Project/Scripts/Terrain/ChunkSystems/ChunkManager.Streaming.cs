using UnityEngine;
using System.Collections.Generic;
using Project.Singleton;
using Unity.Mathematics;
using JetBrains.Annotations;

namespace WorldGen.Terrain
{
    // ChunkManager.Streaming.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        [Header("Streaming")]
        [SerializeField] TerrainStreamingProfile StreamProfile;
        [SerializeField] float cullPadding = 2f;
        private Camera cam;
        private Vector2Int currentChunk = new Vector2Int(int.MinValue, int.MaxValue);
        private Vector2Int previousChunk = new Vector2Int(int.MinValue, int.MaxValue);

        public void SetStreamingSettings(TerrainStreamingProfile settings) => StreamProfile = settings;

        private bool UpdateCameraChunk()
        {
            var hasChanged = false;

            var newChunk = WorldToChunkCoord(camPos);
            if (newChunk != currentChunk)
            {
                previousChunk = currentChunk;
                currentChunk = newChunk;
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

        private void GenerateChunkLOD(ChunkView view, int lod, Vector2Int id, NoiseProfile noise)
        {
            var lodMeshData = view.GetLODMeshData(lod);

            view.GetOrCreateMesh(lodMeshData);

            var ticket = ScheduleMeshJobs(id, lodMeshData.Mesh, ChunkSettings.ChunkVerticies, lodMeshData.Verts, lodMeshData.Stride, ChunkSettings.SizeInUnits, noise);

            ticket.OnComplete = t =>
            {
                OnChunkMeshCompleted(ticket, lod);
            };

            meshTickets.Add(ticket);
        }

        private void GenerateChunks()
        {
            wantedChunkIds.Clear();
            wantedChunkOrder.Clear();

            int viewRadius = StreamProfile.StreamingSettings.ChunkViewRadius;
            int min = -viewRadius;
            int max = viewRadius - 1;

            for (int radius = 0; radius <= viewRadius; radius++)
            {
                int minX = Mathf.Max(-radius, min);
                int maxX = Mathf.Min(radius, max);

                int minZ = Mathf.Max(-radius, min);
                int maxZ = Mathf.Min(radius, max);

                for (int x = minX; x <= maxX; x++)
                {
                    AddWantedChunk(x, minZ);
                    AddWantedChunk(x, maxZ);
                }

                for (int z = minZ + 1; z <= maxZ - 1; z++)
                {
                    AddWantedChunk(minX, z);
                    AddWantedChunk(maxX, z);
                }
            }

            foreach (var id in wantedChunkOrder)
            {
                if(queuedChunkIds.Add(id))
                {
                    chunkBuildQueue.Enqueue(id);
                }
            }
        }

        private void RefreshChunkDelta()
        {
            var updateChunks = UpdateCameraChunk();
            if (!updateChunks) return;

            wantedChunkIds.Clear();
            wantedChunkOrder.Clear();
            removeChunkIds.Clear();

            int currentX = currentChunk.x;
            int currentZ = currentChunk.y;
            int previousX = previousChunk.x;
            int previousZ = previousChunk.y;
            int deltaX = currentX - previousX;
            int deltaZ = currentZ - previousZ;

            int viewRadius = StreamProfile.StreamingSettings.ChunkViewRadius;
            int min = -viewRadius;
            int max = viewRadius - 1;

            switch (deltaX) // if moving right / left
            {
                case 1:
                    for (int z = min; z <= max; z++)
                    {
                        // remove chunk in left column
                        AddToRemoveChunk(previousX + min, previousZ + z);

                        // add chunks in right column
                        AddWantedChunk(currentX + max, currentZ + z);
                    }
                    break;
                case -1:
                    for (int z = min; z <= max; z++)
                    {
                        // remove chunks in right column
                        AddToRemoveChunk(previousX + max, previousZ + z);

                        // add chunks to left column
                        AddWantedChunk(currentX + min, currentZ + z);
                    }
                    break;
            }

            switch (deltaZ) // if moving up / down
            {
                case 1:
                    for (int x = min; x <= max; x++)
                    {
                        // remove chunks in bottom row
                        AddToRemoveChunk(previousX + x, previousZ + min);

                        // add chunks to top row
                        AddWantedChunk(currentX + x, currentZ + max);
                    }
                    break;
                case -1:
                    for (int x = min; x <= max; x++)
                    {
                        // remove chunks in top row
                        AddToRemoveChunk(previousX + x, previousZ + max);

                        // add chunks to bottom row
                        AddWantedChunk(currentX + x, currentZ + min);
                    }
                    break;
            }

            foreach(var id in removeChunkIds)
            {
                var view = activeChunkViews[id];

                chunkViewPool.Return(view);

                activeChunkViews.Remove(id);
                activeChunkIds.Remove(id);
            }
            
            CheckLODBoundary(StreamProfile.QualitySettings.LOD0MaxDistance);
            CheckLODBoundary(StreamProfile.QualitySettings.LOD0MaxDistance + 1);

            CheckLODBoundary(StreamProfile.QualitySettings.LOD1MaxDistance);
            CheckLODBoundary(StreamProfile.QualitySettings.LOD1MaxDistance + 1);

            CheckLODBoundary(StreamProfile.QualitySettings.LOD2MaxDistance);
            CheckLODBoundary(StreamProfile.QualitySettings.LOD2MaxDistance + 1);

            CheckColliderBoundary(StreamProfile.QualitySettings.ChunkColliderBuildRadius);
        }

        private void AddWantedChunk(int dx, int dz)
        {
            var id = new Vector2Int(dx, dz);

            if(wantedChunkIds.Add(id))
            {
                wantedChunkOrder.Add(id);
                ProcessWantedChunk(id);
            }
        }

        private void AddToRemoveChunk(int dx, int dz)
        {
            var id = new Vector2Int(dx, dz);

            ProcessChunkRemoval(id);
        }

        private void CheckLODBoundary(int radius)
        {
            for(int x = -radius; x <= radius; x++) // top bottom
            {
                CheckLOD(currentChunk.x + x, currentChunk.y + radius);
                CheckLOD(currentChunk.x + x, currentChunk.y - radius);
            }

            for(int z  = -radius - 1; z <= radius - 1; z++) // left right
            {
                CheckLOD(currentChunk.x - radius, currentChunk.y + z);
                CheckLOD(currentChunk.x + radius, currentChunk.y + z);
            }
        }

        private void CheckColliderBoundary(int radius)
        {
            for(int x = -radius; x <= radius; x++) // top bottom
            {
                CheckCollider(currentChunk.x + x, currentChunk.y + radius);
                CheckCollider(currentChunk.x + x, currentChunk.y - radius);
            }

            for (int z = -radius - 1; z <= radius - 1; z++) // left right
            {
                CheckCollider(currentChunk.x - radius, currentChunk.y + z);
                CheckCollider(currentChunk.x + radius, currentChunk.y + z);
            }
        }

        private void CheckCollider(int x, int z)
        {
            var id = new Vector2Int(x, z);

            if (!activeChunkViews.TryGetValue(id, out var view)) return;

            int dx = x - currentChunk.x;
            int dz = z - currentChunk.y;

            if (dx < 0) dx = -dx;
            if (dz < 0) dz = -dz;

            int distance = dx > dz ? dx : dz;

            int colliderRadius = StreamProfile.QualitySettings.ChunkColliderBuildRadius;

            if (distance > colliderRadius) return;

            if (view.MeshCollider.sharedMesh != null) return;

            if (collidersToBuild.Add(id)) colliderBuildQueue.Enqueue(id);
        }

        private void CheckLOD(int x, int z)
        {
            var id = new Vector2Int(x, z);

            if (!activeChunkViews.TryGetValue(id, out var view)) return;

            int dx = x - currentChunk.x;
            int dz = z - currentChunk.y;

            if(dx < 0) dx = -dx;
            if(dz < 0) dz = -dz;

            int distance = dx > dz ? dx : dz;

            int newLOD = GetChunkViewLOD(distance);

            if (view.CurrentLOD == newLOD) return;

            if (view.MeshData[newLOD].GeneratedFor != id)
            {
                GenerateChunkLOD(view, newLOD, id, Noise);
            }
            else
            {
                view.SetLOD(newLOD);
            }
        }

        private void ProcessChunkRemoval(Vector2Int id)
        {
            if(!activeChunkViews.TryGetValue(id, out var view)) return;

            int dx = id.x - currentChunk.x;
            int dz = id.y - currentChunk.y;

            if (dx < 0) dx = -dx;
            if (dz < 0) dz = -dz;

            int distance = dx > dz ? dx : dz;

            int keepRadius = StreamProfile.StreamingSettings.ChunkViewRadius + StreamProfile.StreamingSettings.ChunkKeepRadius;

            if (distance > keepRadius)
            {
                removeChunkIds.Add(id);
                return;
            }

            if(view.IsVisible != false)
            {
                view.IsVisible = false;
                view.gameObject.SetActive(false);
            }
        }

        private void ProcessWantedChunk(Vector2Int id)
        {
            if(!activeChunkViews.TryGetValue(id, out var view))
            {
                if(!chunkDataByID.TryGetValue(id, out var chunkData))
                {
                    if(queuedChunkIds.Add(id))
                    {
                        chunkBuildQueue.Enqueue(id);
                    }

                    return;
                }

                activeChunkIds.Add(id);
                CreateChunkView(chunkData, Noise);

                view = activeChunkViews[id];
            }

            int dx = id.x - currentChunk.x;
            int dz = id.y - currentChunk.y;

            if(dx < 0) dx = -dx;
            if(dz < 0) dz = -dz;

            int distance = dx > dz ? dx : dz;

            UpdateChunkVisibilty(view, id, distance);
        }

        private void UpdateChunkVisibilty(ChunkView view, Vector2Int id, int distance)
        {
            int viewRadius = StreamProfile.StreamingSettings.ChunkViewRadius;

            bool inView = distance <= viewRadius;

            if(view.IsVisible != inView)
            {
                view.IsVisible = inView;
                view.gameObject.SetActive(inView);
            }
        }

        private void BuildQueuedChunks()
        {
            float startTime = Time.realtimeSinceStartup;

            Vector2Int id;
            int dx;
            int dz;
            int cd;
            int currentX = currentChunk.x;
            int currentZ = currentChunk.y;
            int viewRadius = StreamProfile.StreamingSettings.ChunkViewRadius;

            while (chunkBuildQueue.Count > 0)
            {
                if (Time.realtimeSinceStartup - startTime > 0.01f) break;

                id = chunkBuildQueue.Dequeue();
                if (activeChunkIds.Contains(id)) continue;

                dx = id.x - currentX;
                if (dx < 0) dx = -dx;
                dz = id.y - currentZ;
                if (dz < 0) dz = -dz;

                cd = dx > dz ? dx : dz;

                if (cd > viewRadius) continue;

                BuildChunk(id);
                UpdateChunkVisibilty(chunkDataByID[id]);
            }
        }

        private void BuildQueuedColliders()
        {
            float startTime = Time.realtimeSinceStartup;

            Vector2Int id;
            int dx;
            int dz;
            int cd;
            int currentX = currentChunk.x;
            int currentZ = currentChunk.y;
            int colliderRadius = StreamProfile.QualitySettings.ChunkColliderBuildRadius;

            while (colliderBuildQueue.Count > 0)
            {
                if (Time.realtimeSinceStartup - startTime > 0.01f) break;

                id = colliderBuildQueue.Dequeue();
                collidersToBuild.Remove(id);

                if (!activeChunkViews.TryGetValue(id, out var view)) continue;

                dx = id.x - currentX;
                if (dx < 0) dx = -dx;
                dz = id.y - currentZ;
                if (dz < 0) dz = -dz;

                cd = dx > dz ? dx : dz;

                if (cd > colliderRadius) continue;

                view.BakeMeshCollider();
            }
        }
    }
}