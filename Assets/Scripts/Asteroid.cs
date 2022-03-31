using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid
{
    public Asteroid() { }

    public Asteroid(float speed, Vector2 position, Vector2 direction, int id, bool isToBeDestroyed)
    {
        Speed = speed;
        Position = position;
        Direction = direction;
        ChunkCoordinates = new Vector2Int((int)position.x, (int)position.y); // TODO: temporary?
        PreviousChunkCoordinates = ChunkCoordinates;
        Id = id;
        IsToBeDestroyed = isToBeDestroyed;
    }

    public float Speed { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; set; }
    public Vector2Int ChunkCoordinates { get; set; }
    public Vector2Int PreviousChunkCoordinates { get; set; }
    public int Id { get; set; }
    public bool IsToBeDestroyed { get; set; }

    private Vector2Int _chunkCoordinates;

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <returns></returns>
    public bool HasEnteredNewChunk()
    {
        // This used to be the source of most of the garbage collection
        //ChunkCoordinates = new Vector2Int((int)Position.x, (int)Position.y);
        _chunkCoordinates.x = (int)Position.x;
        _chunkCoordinates.y = (int)Position.y;

        ChunkCoordinates = _chunkCoordinates;


        if (ChunkCoordinates != PreviousChunkCoordinates)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    public void UpdatePreviousChunkCoordinates()
    {
        PreviousChunkCoordinates = ChunkCoordinates;
    }
}
