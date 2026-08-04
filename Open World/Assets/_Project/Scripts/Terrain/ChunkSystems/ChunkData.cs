using UnityEngine;

namespace Project.Terrain
{
    public class ChunkData
    {
        // Terrain GameObject
        public Transform Transform { get; set; }
        public GameObject GameObject { get; set; }
        public MeshObject MeshObject { get; set; }
        public MeshCollider MeshCollider { get; set; }

        // Chunk Info
        public Vector2Int Coord { get; set; }
        public Bounds Bounds { get; set; }

        public void OnLoad() 
        {
            GameObject.SetActive(true);
        }
        public void OnUnload()
        {
            GameObject.SetActive(false);
        }

        public void SetTransform(Transform transform) => Transform = transform;
        public void SetGameObject(GameObject go) => GameObject = go;
        public void SetMesh(Mesh mesh) => MeshObject.Mesh = mesh;
        public void SetMeshRender(MeshRenderer meshRenderer) => MeshObject.MeshRenderer = meshRenderer;
        public void SetMeshFilter(MeshFilter meshFilter) => MeshObject.MeshFilter = meshFilter;
        public void SetMeshCollider(MeshCollider meshCollider) => MeshCollider = meshCollider;
        public void SetCoord(Vector2Int coord) => Coord = coord;
        public void SetBounds(Bounds bounds) => Bounds = bounds;
    }
}