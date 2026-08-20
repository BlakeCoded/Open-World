using Project.Singleton;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace WorldGen.Terrain
{
    // ChunkManager.Jobs.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        private void ScheduleHeightJob(LODMeshData data, Vector2Int id, int lod, NoiseProfile noise)
        {
            int borderedVerts = data.Verts + 2;
            var heights = new NativeArray<float>(borderedVerts * borderedVerts, Allocator.TempJob);

            HeightsJob job = new HeightsJob()
            {
                Heights = heights,
                MaxDetailVerts = ChunkSettings.ChunkVerticies,
                Verts = borderedVerts,
                Stride = data.Stride,
                SizeInWorldUnits = ChunkSettings.ChunkSizeInUnits,
                ChunkID = new float2(id.x, id.y),
                NoiseSettings = new NoiseSettings
                {
                    Scale = noise.Scale,
                    Octaves = noise.Octaves,
                    Lacunarity = noise.Lacunarity,
                    Persistence = noise.Persistence,
                    HeightMultiplier = noise.HeightMultiplier,
                    OffsetX = SeedOffsetX,
                    OffsetZ = SeedOffsetZ
                }
            };

            JobHandle heightJob = job.Schedule(borderedVerts * borderedVerts, 64);

            var ticket = new MeshTicket()
            {
                ChunkID = id,
                LOD = lod,
                Heights = heights,
                Handle = heightJob,
                State = MeshTicketState.GeneratingHeights
            };

            meshTickets.Add(ticket);
        }

        private void FinalizeMeshTickets()
        {
            int count = 0;

            for (int i = meshTickets.Count - 1; i >= 0; i--)
            {
                if (count >= 5) break;

                var t = meshTickets[i];
                if (!t.Handle.IsCompleted) continue;

                t.Handle.Complete();

                if (!activeChunkViews.TryGetValue(t.ChunkID, out var view))
                {
                    t.Heights.Dispose();
                    meshTickets.RemoveAt(i);
                    continue;
                }

                var lodMeshData = view.GetLODMeshData(t.LOD);

                TerrainMeshGenerator.FillVerticiesFromNativeHeights(lodMeshData.Vertices, t.Heights, lodMeshData.Verts, lodMeshData.Stride);

                lodMeshData.GeneratedFor = t.ChunkID;
                lodMeshData.Mesh.SetVertices(lodMeshData.Vertices);
                lodMeshData.Mesh.RecalculateNormals();
                lodMeshData.Mesh.RecalculateBounds();

                int dx = Mathf.Abs(t.ChunkID.x - currentChunk.x);
                int dz = Mathf.Abs(t.ChunkID.y - currentChunk.y);
                int cd = Mathf.Max(dx, dz);

                if (view.MeshCollider.sharedMesh == null && view.CurrentLOD == ChunkSettings.ColliderLevelOfDetail &&
                cd <= ColliderBuildRadius && collidersToBuild.Add(t.ChunkID))
                {
                    colliderBuildQueue.Enqueue(t.ChunkID);
                }

                view.CurrentLOD = t.LOD;
                view.SetLOD(t.LOD);

                t.Heights.Dispose();
                meshTickets.RemoveAt(i);
            }
        }
    }
}