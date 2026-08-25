using System.Collections.Generic;
using System.Drawing;
using Unity.Collections;
using UnityEngine;

namespace WorldGen.Terrain
{
    public static class TerrainMeshGenerator
    {
        private static readonly Dictionary<int, Mesh> lodBaseMesh = new Dictionary<int, Mesh>();
        private static Mesh TerrainMesh;

        public static Mesh CreateMeshTriangle()
        {
            Mesh mesh = new Mesh();

            Vector3[] verticies =
            {
                new Vector3(0,0,0),
                new Vector3(0,0,1),
                new Vector3(1,0,0)
            };

            int[] triangles =
            {
                0,1,2
            };

            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        public static Mesh CreateMeshQuad()
        {
            Mesh mesh = new Mesh();

            Vector3[] verticies =
            {
                new Vector3(0,0,0),
                new Vector3(0,0,1),
                new Vector3(1,0,0),
                new Vector3(1,0,1),
            };

            int[] triangles =
            {
                0,1,2,
                1,3,2
            };

            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        public static Mesh CreateMeshPlane(float size, int verts)
        {
            Mesh mesh = new Mesh();

            int index = 0;
            float step = size / (verts - 1);

            Vector3[] verticies = new Vector3[verts * verts];

            for(int z = 0; z < verts; z++)
                for(int  x = 0; x < verts; x++)
                {
                    verticies[index++] = new Vector3(x * step, 0, z * step);
                }


            int[] triangles = new int[(verts - 1) * (verts - 1) * 6];

            int triangleIndex = 0;
            int value = verts - 1;

            for (int z = 0; z < value; z++)
                for (int x = 0; x < value; x++)
                {
                    int BL = z * verts + x;
                    int BR = BL + 1;
                    int TL = BL + verts;
                    int TR = TL + 1;

                    triangles[triangleIndex++] = BL;
                    triangles[triangleIndex++] = TL;
                    triangles[triangleIndex++] = BR;

                    triangles[triangleIndex++] = TL;
                    triangles[triangleIndex++] = TR;
                    triangles[triangleIndex++] = BR;
                }

            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        public static Mesh CreateBaseMesh(int verts, int stride)
        {
            if(!lodBaseMesh.TryGetValue(stride, out var mesh))
            {
                mesh = new Mesh();

                Vector3[] vertices = new Vector3[verts * verts];

                FillVerticiesBase(vertices, verts, ChunkSettings.SizeInUnits);
                var triangles = CreateTriangles(verts);
                var uvs = CreateUVs(verts, ChunkSettings.SizeInUnits, ChunkSettings.TextureScale);

                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.SetUVs(0, uvs);

                lodBaseMesh.Add(stride, mesh);

                return Object.Instantiate(mesh);
            }

            return Object.Instantiate(mesh);
        }

        public static Mesh CreateBaseTerrainMesh(int verts, float meshSize)
        {
            if(TerrainMesh == null)
            {
                var mesh = new Mesh();

                Vector3[] vertices = new Vector3[verts * verts];

                FillVerticiesBase(vertices, verts, meshSize);
                var triangles = CreateTriangles(verts);
                var uvs = CreateUVs(verts, meshSize, ChunkSettings.TextureScale);

                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.SetUVs(0, uvs);

                TerrainMesh = mesh;
                return Object.Instantiate(mesh);
            }

            return Object.Instantiate(TerrainMesh);
        }

        public static Vector3[] FillVerticiesBase(Vector3[] vertices, int verts, float size)
        {
            float step = size / (verts - 1);

            int index = 0;

            for (int z = 0; z < verts; z++)
                for (int x = 0; x < verts; x++)
                {
                    vertices[index++] = new Vector3(x * step, 0f, z * step);
                }

            return vertices;
        }

        public static void FillVerticies(Vector3[] vertices, float[] heights, int verts, int stride)
        {
            //int verts = (ChunkSettings.ChunkVerticies - 1) / stride + 1;

            int heightIndex = 0;
            int borderedVerts = verts + 2;

            int index = 0;

            for (int z = 0; z < verts; z++)
                for (int x = 0; x < verts; x++)
                {
                    heightIndex = (z + 1) * borderedVerts + (x + 1);

                    vertices[index].y = heights[heightIndex];

                    index++;
                }
        }

        public static void FillVerticiesFromNativeHeights(Vector3[] vertices, NativeArray<float> heights, int verts, int stride)
        {
            int heightIndex = 0;
            int borderedVerts = verts + 2;

            int index = 0;

            for (int z = 0; z < verts; z++)
                for (int x = 0; x < verts; x++)
                {
                    heightIndex = (z + 1) * borderedVerts + (x + 1);

                    vertices[index].y = heights[heightIndex];

                    index++;
                }
        }

        public static void FillNormals(Vector3[] normals, float[] heights, int verts)
        {
            //int verts = (ChunkSettings.ChunkVerticies - 1) / stride + 1;
            int borderedVerts = verts + 2;

            for (int z = 0; z < verts; z++)
            {
                for (int x = 0; x < verts; x++)
                {
                    int borderedX = x + 1;
                    int borderedZ = z + 1;

                    int center = borderedZ * borderedVerts + borderedX;

                    float left = heights[center - 1];
                    float right = heights[center + 1];

                    float down = heights[center - borderedVerts];
                    float up = heights[center + borderedVerts];

                    Vector3 normal = new Vector3(
                        left - right,
                        2f,
                        down - up
                    ).normalized;

                    normals[z * verts + x] = normal;
                }
            }
        }

        private static Vector3[] CreateBaseVerticies(float size, int verts)
        {
            int index = 0;
            float step = size / (verts - 1);

            Vector3[] verticies = new Vector3[verts * verts];

            for (int z = 0; z < verts; z++)
                for (int x = 0; x < verts; x++)
                {
                    verticies[index++] = new Vector3(x * step, 0f, z * step);
                }

            return verticies;
        }

        private static int[] CreateTriangles(int verts)
        {
            int[] triangles = new int[(verts - 1) * (verts - 1) * 6];

            int triangleIndex = 0;
            int value = verts - 1;

            for (int z = 0; z < value; z++)
                for (int x = 0; x < value; x++)
                {
                    int BL = z * verts + x;
                    int BR = BL + 1;
                    int TL = BL + verts;
                    int TR = TL + 1;

                    triangles[triangleIndex++] = BL;
                    triangles[triangleIndex++] = TL;
                    triangles[triangleIndex++] = BR;

                    triangles[triangleIndex++] = TL;
                    triangles[triangleIndex++] = TR;
                    triangles[triangleIndex++] = BR;
                }

            return triangles;
        }

        private static Vector2[] CreateUVs(int verts)
        {
            Vector2[] uvs = new Vector2[verts * verts];

            float inv = 1f / (verts - 1);

            int index;

            for (int z = 0; z < verts; z++)
            {
                for (int x = 0; x < verts; x++)
                {
                    index = z * verts + x;

                    uvs[index] = new Vector2(
                        x * inv,
                        z * inv);
                }
            }

            return uvs;
        }

        private static Vector2[] CreateUVs(int verts, float meshSize, float textureScale)
        {
            Vector2[] uvs = new Vector2[verts * verts];

            float worldStep = meshSize / (verts - 1);
            float uvStep = worldStep / textureScale;

            for (int z = 0; z < verts; z++)
            {
                for (int x = 0; x < verts; x++)
                {
                    int index = z * verts + x;

                    uvs[index] = new Vector2(
                        x * uvStep,
                        z * uvStep);
                }
            }

            return uvs;
        }

        public static Vector3[] CalculateNormals(int verts, float[] heights)
        {
            int borderedVerts = verts + 2;

            Vector3[] normals = new Vector3[verts * verts];

            for (int z = 0; z < verts; z++)
            {
                for (int x = 0; x < verts; x++)
                {
                    int borderedX = x + 1;
                    int borderedZ = z + 1;

                    int center = borderedZ * borderedVerts + borderedX;

                    float left = heights[center - 1];
                    float right = heights[center + 1];

                    float down = heights[center - borderedVerts];
                    float up = heights[center + borderedVerts];

                    Vector3 normal = new Vector3(
                        left - right,
                        2f,
                        down - up
                    ).normalized;

                    normals[z * verts + x] = normal;
                }
            }

            return normals;
        }
    }
}