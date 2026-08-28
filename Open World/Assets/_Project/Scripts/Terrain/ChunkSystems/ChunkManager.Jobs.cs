using Project.Singleton;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
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

            NativeReference<float2> minMax = new NativeReference<float2>(Allocator.Persistent);

            MinMaxHeightJob minMaxHeightJob = new MinMaxHeightJob()
            {
                Heights = heights,
                MinMax = minMax,
            };

            JobHandle minMaxHandle = minMaxHeightJob.Schedule(heightHandle);

            //Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(mesh);

            //Mesh.MeshData meshData = meshDataArray[0];

            //var vertexData = meshData.GetVertexData<TerrainVertex>(0);

            var vertices = new NativeArray<float3>(verts * verts, Allocator.Persistent);

            VertexJob Vjob = new VertexJob()
            {
                Heights = heights,
                //Vertices = vertexData,
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
                //Vertices = vertexData,
                Normals = normals,
                SizeInWorldUnits = size,
                Verts = verts,
                Stride = stride
            };

            JobHandle normalHandle = Njob.Schedule(verts * verts, 64, heightHandle);

            JobHandle meshHandle = JobHandle.CombineDependencies(vertexHandle, normalHandle, minMaxHandle);

            var ticket = new MeshTicket()
            {
                ID = id,
                Mesh = mesh,
                GenerationID = generationID,
                Heights = heights,
                Vertices = vertices,
                Normals = normals,
                //meshDataArray = meshDataArray,
                MinMaxHeight = minMax,
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
                    //t.meshDataArray.Dispose();

                    t.MinMaxHeight.Dispose();
                    meshTickets.RemoveAt(i);
                    continue;
                }

                //using (new ProfilerMarker("Apply Mesh Data").Auto())
                //{
                //    Mesh.ApplyAndDisposeWritableMeshData(t.meshDataArray, t.Mesh);  
                //}

                //using (new ProfilerMarker("Apply Mesh Data").Auto())
                //{
                //    t.Mesh.SetVertices(t.Vertices);
                //    t.Mesh.SetNormals(t.Normals);
                //}

                t.Mesh.SetVertices(t.Vertices);
                t.Mesh.SetNormals(t.Normals);
                t.OnComplete?.Invoke(t);

                t.Heights.Dispose();
                t.Vertices.Dispose();
                t.Normals.Dispose();
                t.MinMaxHeight.Dispose();
                meshTickets.RemoveAt(i);

                meshesLoaded++;
            }
        }
    }
}