using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static readonly int ChunkSize = 64;
    public static int ViewDistance = 2;

    private readonly Dictionary<Vector2Int, Chunk> chunks = new();

    [SerializeField] private GameObject Player;

    private void Awake()
    {
        Vector2Int playerChunk = WorldToCoord(Player.transform.position);

        for (int x = -ViewDistance; x <= ViewDistance; x++)
        {
            for (int y = -ViewDistance; y <= ViewDistance; y++)
            {
                Vector2Int coord = playerChunk + new Vector2Int(x, y);

                if(!chunks.ContainsKey(coord))
                {
                    Chunk chunk = CreateChunk(coord);

                    chunks.Add(coord, chunk);
                }
            }
        }
    }

    private Chunk CreateChunk(Vector2Int coord)
    {
        GameObject chunkObject = GameObject.CreatePrimitive(PrimitiveType.Plane);

        chunkObject.transform.position = CoordToWorld(coord);
        chunkObject.transform.localScale = Vector3.one * (ChunkSize / 10f);
        
        return new Chunk(coord, chunkObject);
    }

    private Vector2Int WorldToCoord(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / ChunkSize),
                              Mathf.FloorToInt(position.z / ChunkSize));
    }

    private Vector3 CoordToWorld(Vector2Int coord)
    {
        return new Vector3(coord.x *  ChunkSize, 
                           0f, 
                           coord.y * ChunkSize);
    }
}