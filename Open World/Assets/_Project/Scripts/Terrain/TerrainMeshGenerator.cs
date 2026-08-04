using UnityEngine;

namespace Project.Terrain
{
    public static class TerrainMeshGenerator
    {
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
            Mesh mesh = new Mesh();

            int index = 0;
            float step = size / (verts - 1);

            Vector3[] verticies = new Vector3[verts * verts];

            for (int z = 0; z < verts; z++)
                for (int x = 0; x < verts; x++)
                {
                    verticies[index++] = new Vector3(x * step, heights[index - 1], z * step);
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
    }
}