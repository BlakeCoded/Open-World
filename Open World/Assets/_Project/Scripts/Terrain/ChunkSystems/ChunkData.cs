using UnityEngine;

namespace WorldGen.Terrain
{
    public class ChunkData
    {
        public GameObject GameObject;
        public Vector2Int Coord;
        public ChunkRenderData RenderData;
        public ChunkCullData CullData;

        public void OnLoad() 
        {
            GameObject.SetActive(true);
        }
        public void OnUnload()
        {
            GameObject.SetActive(false);
        }

        public void SetCoord(Vector2Int coord) => Coord = coord;
        public void SetGameObject(GameObject go) => GameObject = go;
        public void SetMesh(Mesh mesh) => RenderData.Mesh = mesh;
        public void SetMeshRender(MeshRenderer meshRenderer) => RenderData.MeshRenderer = meshRenderer;
        public void SetMeshFilter(MeshFilter meshFilter) => RenderData.MeshFilter = meshFilter;
        public void SetMeshCollider(MeshCollider meshCollider) => RenderData.MeshCollider = meshCollider;
        public void SetMeshVisible(bool visable) { CullData.Visible = visable; RenderData.MeshRenderer.enabled = visable; }
        public void SetBounds(Bounds bounds) { CullData.Bounds = bounds; }
    }
}