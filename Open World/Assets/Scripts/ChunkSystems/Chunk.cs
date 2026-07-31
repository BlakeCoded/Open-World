using UnityEngine;

public class Chunk
{
    public Vector2Int Coord { get; }

    public GameObject GameObject { get; }

    public Chunk(Vector2Int coord, GameObject gameObject) 
    {
        this.Coord = coord;
        this.GameObject = gameObject;
    }
}