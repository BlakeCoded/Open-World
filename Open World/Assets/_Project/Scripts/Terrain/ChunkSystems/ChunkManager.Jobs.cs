using System;
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
        private MeshTicket ScheduleMeshJobs(Vector2Int id, float2 worldOrigin, Mesh mesh, int maxDetailVerts, int verts, int stride, float size, NoiseProfile noise)
        {
            int borderedVerts = verts + 2;
            var heights = new NativeArray<float>(borderedVerts * borderedVerts, Allocator.Persistent);

            HeightsJob Hjob = new HeightsJob()
            {
                Heights = heights,
                MaxDetailVerts = maxDetailVerts,
                BorderedVerts = borderedVerts,
                Stride = stride,
                SizeInWorldUnits = size,
                WorldOrigin = worldOrigin,
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

            JobHandle heightHandle = Hjob.Schedule(borderedVerts * borderedVerts, 64);

            var vertices = new NativeArray<float3>(verts * verts, Allocator.Persistent);

            VertexJob Vjob = new VertexJob()
            {
                Heights = heights,
                Vertices = vertices,
                SizeInWorldUnits = size,
                Verts = verts,
                Stride = stride,
            };

            JobHandle vertexHandle = Vjob.Schedule(verts * verts, 64, heightHandle);

            var normals = new NativeArray<float3>(verts * verts, Allocator.Persistent);

            NormalsJob Njob = new NormalsJob()
            {
                Heights = heights,
                Normals = normals,
                SizeInWorldUnits = size,
                Verts = verts,
                Stride = stride
            };

            JobHandle normalHandle = Njob.Schedule(verts * verts, 64, heightHandle);

            JobHandle meshHandle = JobHandle.CombineDependencies(vertexHandle, normalHandle);

            var ticket = new MeshTicket()
            {
                ID = id,
                GenerationID = generationID,
                Heights = heights,
                Vertices = vertices,
                Normals = normals,
                Handle = meshHandle,
                Mesh = mesh,
            };

            return ticket;
        }

        private void FinalizeMeshTickets()
        {
            float startTime = Time.realtimeSinceStartup;

            for (int i = meshTickets.Count - 1; i >= 0; i--)
            {
                if (Time.realtimeSinceStartup - startTime > 0.01f) break;

                var t = meshTickets[i];
                if (!t.Handle.IsCompleted) continue;

                t.Handle.Complete();

                if(t.GenerationID != generationID)
                {
                    t.Heights.Dispose();
                    t.Vertices.Dispose();
                    t.Normals.Dispose();
                    meshTickets.RemoveAt(i);
                    continue;
                }

                t.Mesh.SetVertices(t.Vertices);
                t.Mesh.SetNormals(t.Normals);
                t.Mesh.RecalculateBounds();

                t.OnComplete?.Invoke(t);

                t.Heights.Dispose();
                t.Vertices.Dispose();
                t.Normals.Dispose();
                meshTickets.RemoveAt(i);

                meshesLoaded++;
            }
        }
    }
}