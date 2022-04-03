using UnityEngine;

/// <summary>
/// Denotes a simulated off-screen asteroid and manages some stats.
/// </summary>
public class SimulatedAsteroid
{
    public SimulatedAsteroid() { }

    public SimulatedAsteroid(float speed, Vector2 position, Vector2 direction, int id)
    {
        Speed = speed;
        Position = position;
        Direction = direction;
        ChunkCoordinates = Vector2Int.FloorToInt(position);
        PreviousChunkCoordinates = ChunkCoordinates;
        Id = id;
        IsRespawning = false;
        RespawnTimer = 0;
        IsSimulated = true;
    }

    public float Speed { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; set; }
    public Vector2Int ChunkCoordinates { get; set; }
    public Vector2Int PreviousChunkCoordinates { get; set; }
    public int Id { get; set; }
    public bool IsRespawning { get; set; }
    public float RespawnTimer { get; set; }
    public bool IsSimulated { get; set; }

    /// <summary>
    /// Updates the asteroids current chunk based on current position and checks if it's a new one.
    /// </summary>
    /// <returns> True if the asteroid has entered a new chunk, otherwise false. </returns>
    public bool HasEnteredNewChunk()
    {
        // I tried different ways to set this since it happens so often, it did not affect performance.
        ChunkCoordinates = Vector2Int.FloorToInt(Position);

        if (ChunkCoordinates != PreviousChunkCoordinates)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Updates the asteroid's current chunk coordinates. 
    /// </summary>
    public void UpdateChunkCoordinates()
    {
        ChunkCoordinates = Vector2Int.FloorToInt(Position);
    }

    /// <summary>
    /// Updates the asteroid's previous chunk coordinates. 
    /// </summary>
    public void UpdatePreviousChunkCoordinates()
    {
        PreviousChunkCoordinates = ChunkCoordinates;
    }
}
