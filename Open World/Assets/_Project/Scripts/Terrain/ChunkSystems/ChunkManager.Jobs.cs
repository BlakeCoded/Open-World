using System;
using Project.Singleton;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace WorldGen.Terrain
{
    // ChunkManager.Jobs.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        private MeshTicket ScheduleMeshJobs(Vector2Int id, Mesh mesh, int maxDetailVerts, int verts, int stride, float size, NoiseProfile noise)
        {
            int borderedVerts = verts + 2;
            var heights = new NativeArray<float>(borderedVerts * borderedVerts, Allocator.Persistent);

            var worldOrigin = new float2(id.x * size, id.y * size);

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

            NativeReference<float> minHeight = new NativeReference<float>(Allocator.Persistent);
            NativeReference<float> maxHeight = new NativeReference<float>(Allocator.Persistent);

            MinMaxHeightJob minMaxHeightJob = new MinMaxHeightJob()
            {
                Heights = heights,
                minHeight = minHeight,
                maxHeight = maxHeight,
            };

            JobHandle minMaxHandle = minMaxHeightJob.Schedule(heightHandle);

            var vertices = new NativeArray<float3>(verts * verts, Allocator.Persistent);

            VertexJob Vjob = new VertexJob()
            {
                Heights = heights,
                Vertices = vertices,
                SizeInWorldUnits = size,
                Verts = verts,
                Stride = stride,
            };

            JobHandle vertexHandle = Vjob.Schedule(verts * verts, 64, minMaxHandle);

            var normals = new NativeArray<float3>(verts * verts, Allocator.Persistent);

            NormalsJob Njob = new NormalsJob()
            {
                Heights = heights,
                Normals = normals,
                SizeInWorldUnits = size,
                Verts = verts,
                Stride = stride
            };

            JobHandle normalHandle = Njob.Schedule(verts * verts, 64, minMaxHandle);

            JobHandle meshHandle = JobHandle.CombineDependencies(vertexHandle, normalHandle);

            var ticket = new MeshTicket()
            {
                ID = id,
                Mesh = mesh,
                GenerationID = generationID,
                Heights = heights,
                Vertices = vertices,
                Normals = normals,
                minHeight = minHeight,
                maxHeight = maxHeight,
                Handle = meshHandle,
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
                    t.minHeight.Dispose();
                    t.maxHeight.Dispose();
                    meshTickets.RemoveAt(i);
                    continue;
                }

                t.Mesh.SetVertices(t.Vertices);
                t.Mesh.SetNormals(t.Normals);
                //t.Mesh.RecalculateBounds();

                t.OnComplete?.Invoke(t);

                t.Heights.Dispose();
                t.Vertices.Dispose();
                t.Normals.Dispose();
                t.minHeight.Dispose();
                t.maxHeight.Dispose();
                meshTickets.RemoveAt(i);

                meshesLoaded++;
            }
        }

        public static Mesh CreateTestMeshData(int verts)
        {
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);

            Mesh.MeshData meshData = meshDataArray[0];

            int vertexCount = verts * verts;

            meshData.SetVertexBufferParams(
                vertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position,
                VertexAttributeFormat.Float32,
                3),
                new VertexAttributeDescriptor(
                    VertexAttribute.Normal,
                    VertexAttributeFormat.Float32,
                    3),
                new VertexAttributeDescriptor(
                    VertexAttribute.TexCoord0,
                    VertexAttributeFormat.Float32,
                    2
                ));

            meshData.SetIndexBufferParams((verts - 1) * (verts - 1) * 6, IndexFormat.UInt32);

            Mesh mesh = new Mesh();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

            return mesh;
        }
    }
}