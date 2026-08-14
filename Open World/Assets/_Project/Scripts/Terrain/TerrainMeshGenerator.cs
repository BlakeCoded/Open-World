using UnityEngine;

namespace WorldGen.Terrain
{
    public static class TerrainMeshGenerator
    {
        public static readonly Vector3[] BaseVertices;
        public static readonly int[] Triangles;
        public static readonly Vector2[] UVs;

        static TerrainMeshGenerator()
        {
            BaseVertices = CreateBaseVerticies(ChunkSettings.ChunkSizeInUnits, ChunkSettings.ChunkVerticies);
            Triangles = CreateTriangles(ChunkSettings.ChunkVerticies);
            UVs = CreateUVs(ChunkSettings.ChunkVerticies);
        }

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

        public static Mesh CreateMeshTerrain(float size, int verts, float[] heights)
        {
            Mesh mesh = new Mesh(); // Can Store Meshes in a pool and reuses

            int index = 0;
            int heightIndex = 0;
            int borderedVerts = verts + 2;

            Vector3[] vertices = new Vector3[verts * verts]; // can get each view object to store a reusesable array to minimize allocations

            for (int z = 0; z < verts; z++)
                for (int x = 0; x < verts; x++)
                {
                    heightIndex = (z + 1) * borderedVerts + (x + 1);

                    vertices[index] = BaseVertices[index];
                    vertices[index].y = heights[heightIndex];

                    index++;
                }

            mesh.vertices = vertices;
            mesh.triangles = Triangles;
            mesh.SetUVs(0, UVs);
            //mesh.normals = CalculateNormals(verts, heights);

            return mesh;
        }

        public static Mesh CreateBaseMesh()
        {
            var mesh = new Mesh();

            mesh.SetVertices(BaseVertices);
            mesh.SetTriangles(Triangles, 0);
            mesh.SetUVs(0, UVs);
            mesh.RecalculateNormals();

            return mesh;
        }

        public static void FillVerticies(Vector3[] vertices, float[] heights)
        {
            int verts = ChunkSettings.ChunkVerticies;

            int index = 0;
            int heightIndex = 0;
            int borderedVerts = verts + 2;

            for (int z = 0; z < verts; z++)
                for (int x = 0; x < verts; x++)
                {
                    heightIndex = (z + 1) * borderedVerts + (x + 1);

                    vertices[index].y = heights[heightIndex];

                    index++;
                }
        }

        public static void FillNormals(float[] heights, Vector3[] normals)
        {
            int verts = ChunkSettings.ChunkVerticies;
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

            for (int z = 0; z < verts; z++)
            {
                for (int x = 0; x < verts; x++)
                {
                    int index = z * verts + x;

                    uvs[index] = new Vector2(
                        x * inv,
                        z * inv);
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