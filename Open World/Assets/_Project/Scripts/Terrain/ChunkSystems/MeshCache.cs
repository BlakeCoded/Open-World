using System.Collections.Generic;
using Project.Singleton;
using UnityEditor;
using UnityEngine;

public static class MeshCache
{
    private static readonly Dictionary<Vector2Int, Mesh> cache = new();

    public static bool TryGetMesh(Vector2Int key, out Mesh mesh)
    {
        return cache.TryGetValue(key, out mesh);
    }

    public static void AddMesh(Vector2Int key, Mesh mesh)
    {
        cache[key] = mesh;
    }

    public static void RemoveMesh(Vector2Int key)
    {
        cache.Remove(key);
    }

    public static void Clear()
    {
        cache.Clear();
    }
}
