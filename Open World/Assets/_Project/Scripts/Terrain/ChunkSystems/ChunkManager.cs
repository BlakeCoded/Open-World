using System.Collections.Generic;
using UnityEngine;
using Project.Singleton;
using Project.Terrain;
using System;

public class ChunkManager : MonoBehaviourSingleton<ChunkManager>
{
    readonly Dictionary<Vector2Int, ChunkData> chunksById = new();
    readonly HashSet<Vector2Int> activeIds = new();
    readonly HashSet<Vector2Int> visibleChunks = new();
    readonly HashSet<Vector2Int> wantedIds = new();
    readonly HashSet<Vector2Int> removeIds = new();
    readonly HashSet<Vector2Int> queuedIds = new();
    readonly Queue<Vector2Int> buildQueue = new();

    [Header("Streaming")]
    [SerializeField] int ChunkViewDistance = 2;
    [SerializeField] float updateInterval = 0.25f;
    [SerializeField] int batchAmount = 5;

    float refreshChunksTimer;
    Vector2Int currentChunk;

    [SerializeField] private GameObject Player;
    [SerializeField] private Material defaultMat;

    private void Awake()
    {
        // Init seeds
    }

    private void Update() 
    {
        UpdateCurrentChunk();
        RefreshWantedChunks();
        BuildQueuedChunks(batchAmount);
    }

    private void UpdateCurrentChunk()
    {
        currentChunk = WorldToCoord(Player.transform.position);
    }

    private void BuildChunk(Vector2Int key)
    {
        if(chunksById.ContainsKey(key)) return;

        GameObject go = new GameObject($"Chunk_{key.x}_{key.y}");

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

        float[] heights = TerrainHeightGenerator.CreateHeights(ChunkSettings.ChunkSizeInUnits, ChunkSettings.ChunkVerticies, key);

        Mesh mesh = TerrainMeshGenerator.CreateMeshTerrain(ChunkSettings.ChunkSizeInUnits, ChunkSettings.ChunkVerticies, heights);

        meshFilter.mesh = mesh;
        meshRenderer.material = defaultMat;

        var meshObject = new MeshObject
        {
            Mesh = mesh,
            MeshFilter = meshFilter,
            MeshRenderer = meshRenderer
        };

        var t = go.transform;
        t.parent = transform;
        t.position = CoordToWorld(key);

        var chunk = new ChunkData
        {
            Coord = key,
            GameObject = go,
            MeshObject = meshObject,
            Bounds = new Bounds(t.position, new Vector3(ChunkSettings.ChunkSizeInUnits, 100f, ChunkSettings.ChunkSizeInUnits)),
        };

        chunksById[key] = chunk;
        activeIds.Add(key);

        chunk.OnLoad();
    }

    private void RefreshWantedChunks()
    {
        refreshChunksTimer -= Time.deltaTime;
        if (refreshChunksTimer >= 0f) return;
        refreshChunksTimer = updateInterval;

        wantedIds.Clear();
        removeIds.Clear();

        for (int dx = -ChunkViewDistance; dx <= ChunkViewDistance; dx++)
            for (int dz = -ChunkViewDistance; dz <= ChunkViewDistance; dz++)
            {
                var id = new Vector2Int(currentChunk.x + dx, currentChunk.y + dz);
                int cd = Mathf.Max(Mathf.Abs(dz), Mathf.Abs(dx));
                if(cd <= ChunkViewDistance) wantedIds.Add(id); 
            }

        foreach(var id in activeIds)
            if(!wantedIds.Contains(id))
                removeIds.Add(id);

        foreach(var id in removeIds)
        {
            chunksById[id].OnUnload();
            activeIds.Remove(id);
        }

        foreach(var id in wantedIds)
        {
            if(chunksById.TryGetValue(id, out var chunk))
            {
                if(activeIds.Add(id)) chunk.OnLoad();
            }
            else if(queuedIds.Add(id))
            {
                buildQueue.Enqueue(id);
            }
        }
    }

    private void BuildQueuedChunks(int batch)
    {
        int count = 0;
        while(count < batch && buildQueue.Count > 0)
        {
            var id = buildQueue.Dequeue();
            if (activeIds.Contains(id)) continue;

            BuildChunk(id);
            count++;
        }
    }

    private Vector2Int WorldToCoord(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / ChunkSettings.ChunkSizeInUnits),
                              Mathf.FloorToInt(position.z / ChunkSettings.ChunkSizeInUnits));
    }

    private Vector3 CoordToWorld(Vector2Int coord)
    {
        return new Vector3(coord.x * ChunkSettings.ChunkSizeInUnits, 
                           0f, 
                           coord.y * ChunkSettings.ChunkSizeInUnits);
    }
}