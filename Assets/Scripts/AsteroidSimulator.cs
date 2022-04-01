using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// This script is supposed to simulate all offscreen asteroids to save processing power, since unity colliders are expensive
/// </summary>
public class AsteroidSimulator : MonoBehaviour
{
    [SerializeField] private int _gridLength;
    [SerializeField] private int _gridWidth;

    [SerializeField] private float _minSpeed;
    [SerializeField] private float _maxSpeed;

    [SerializeField] private Transform player;
    [SerializeField] private AsteroidManager asteroidManager;

    [Header("Read my tooltip")]
    [Tooltip("This boolean is here to set whether the simulated asteroids should check if they collided with another asteroid in a diagonally neighbouring chunk. " +
        "This constitutes around 2.5% of all off-screen collisions but slows down performance by 35%. ")]
    [SerializeField]
    private bool diagonalChecks;

    private int _idCounter;
    private int _groupCounter;
    private const int _respawnTime = 1;

    // TODO: For testing purposes
    private int _regularCounter;
    private int _orthoCounter;
    private int _diagCounter;
    private float _testTimer;

    // For manipulation during spawning.
    private SimulatedAsteroid _asteroidToSpawn = new SimulatedAsteroid();

    // Creating x different groups of asteroids so we can split checking them into different frames.
    // This number is really important. The lower it is, the more accurate the off-screen simulation, but also less performant.
    // For best ratio keep between 6 and 12. Taking into account that our target FPS is 60, at 12 every asteroid moves and checks for collision every 0.2 seconds, or better if FPS is higher.
    // Every 0.2 seconds means some 95% of collisions are detected, taking into account the asteroid's size and max speed.
    
    // TODO: This number is not actually important at all - spreads the load but detection accuracy stays the same *because* it is frame dependent.
    // However, making it too large means the rest of the game affects simulation performance
    // Making it too small means the simulation affects the rest of the game's performance
    // If with 12 groups it runs below 60FPS - we have a problem
    private const int _numberOfGroups = 12;
    private List<SimulatedAsteroid>[] _allAsteroidsSplitIntoGroups = new List<SimulatedAsteroid>[_numberOfGroups]; // array size must be number of groups - const

    // Dictionary of all imaginary chunks with coordinates as key and a list of all asteroids in the chunk as value - used for collision checks between asteroids.
    private Dictionary<Vector2Int, List<SimulatedAsteroid>> _allChunks = new Dictionary<Vector2Int, List<SimulatedAsteroid>>();

    // List of asteroids re-used multiple times to manipulate dictionary values.
    private List<SimulatedAsteroid> _asteroidListForAddingToDict = new List<SimulatedAsteroid>();

    // Makes a piece of code more readable later.
    private List<SimulatedAsteroid> _currentGroupOfAsteroids = new List<SimulatedAsteroid>();

    // Constantly reused variables for readability when checking bordering chunks later on.
    private Vector2Int _northChunkCoordinates = new Vector2Int();
    private Vector2Int _southChunkCoordinates = new Vector2Int();
    private Vector2Int _eastChunkCoordinates = new Vector2Int();
    private Vector2Int _westChunkCoordinates = new Vector2Int();

    private Vector2Int _northEastChunkCoordinates = new Vector2Int();
    private Vector2Int _southEastChunkCoordinates = new Vector2Int();
    private Vector2Int _northWestChunkCoordinates = new Vector2Int();
    private Vector2Int _southWestChunkCoordinates = new Vector2Int();

    /// <summary>
    /// TODO: Split into initialization method and some other shit
    /// </summary>
    private void Start()
    {
        _idCounter = 0;
        _groupCounter = 0;

        // We initialize the empty asteroid groups
        for (int g = 0; g < _numberOfGroups; g++)
        {
            _allAsteroidsSplitIntoGroups[g] = new List<SimulatedAsteroid>();
        }

        // We create the asteroids and assign them to their groups.
        for (int i = 0; i < _gridWidth; i++)
        {
            for (int j = 0; j < _gridLength; j++)
            {
                _asteroidToSpawn = new SimulatedAsteroid(Random.Range(_minSpeed, _maxSpeed),
                                                new Vector2((i - _gridLength / 2) * 2, (j - _gridWidth / 2) * 2), // We set them 2 units apart each at start
                                                new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized,
                                                _idCounter);

                _idCounter++;


                // First we add the asteroid to its chunk's list.
                if (_allChunks.ContainsKey(_asteroidToSpawn.ChunkCoordinates))
                {
                    _allChunks[_asteroidToSpawn.ChunkCoordinates].Add(_asteroidToSpawn);
                }
                else
                {
                    _asteroidListForAddingToDict = new List<SimulatedAsteroid>();
                    _asteroidListForAddingToDict.Add(_asteroidToSpawn);
                    _allChunks.Add(_asteroidToSpawn.ChunkCoordinates, _asteroidListForAddingToDict);
                }

                // Now we determine the group the asteroid should go into.
                _allAsteroidsSplitIntoGroups[_groupCounter].Add(_asteroidToSpawn);

                _groupCounter++;
                if (_groupCounter == _numberOfGroups)
                {
                    _groupCounter = 0;
                }
            }
        }

        // After we're done with generation, we reset group counter so it can be reused in fixed update.
        _groupCounter = 0;
        _testTimer = 0;
    }

    private void Update()
    {
        //_orthoCounter = 0;
        //_diagCounter = 0;


        // Every x frames we check an asteroid group's state, where x is total number of groups
        // This splits the load across multiple frames, and the off-screen simulation does not have to be perfect.
        // Assuming the target framerate is 60, with the current asteroid and player max speed and current asteroid size, it is still reasonably accurate up to 15 groups.
        if (_groupCounter >= _numberOfGroups) // TODO: Unit profiler claims there's garbage allocation on this line, I don't see how that is possible
        {
            _groupCounter = 0;
        }

        CheckAsteroidStatesbyGroup(_groupCounter);
        _groupCounter++;

        if(_testTimer > 5)
        {
            Debug.Log(_testTimer.ToString("0.00") + " seconds has passed.");
            Debug.Log("Total: " + (_regularCounter + _orthoCounter + _diagCounter).ToString());
            _testTimer = 0;
        }
        else
        {
            _testTimer += Time.deltaTime;
        }
    }

    /// <summary>
    /// For testing purposes only
    /// </summary>
    private void OnApplicationQuit()
    {
        //Debug.Log("Regular: " + _regularCounter);
        //Debug.Log("ORTHO: " + _orthoCounter);
        //Debug.Log("DIAGONAL: " + _diagCounter);
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    private void CheckAsteroidStatesbyGroup(int groupId)
    {
        _currentGroupOfAsteroids = _allAsteroidsSplitIntoGroups[groupId]; // TODO: Unity profiler claims there's garbage allocation on this line

        // We do all checks in as few loops as possible
        for (int i = 0; i < _currentGroupOfAsteroids.Count; i++)
        {
            // Only simulate asteroids that are not on screen
            if (_currentGroupOfAsteroids[i].IsSimulated)
            {
                // Only check for asteroids which are not currently respawning
                // TODO: Swap order so they can spawn and be destroyed in same frame - task requirement.
                if (!_currentGroupOfAsteroids[i].IsRespawning)
                {
                    MoveAsteroid(_currentGroupOfAsteroids[i]);
                    CheckForNewChunk(_currentGroupOfAsteroids[i]);

                    // TODO: rename this shit
                    SimulatedAsteroid arbitraryAsteroid;
                    arbitraryAsteroid = CheckCollisions(_currentGroupOfAsteroids[i]);

                    // Check if the asteroid collided with something
                    if (_currentGroupOfAsteroids[i].IsRespawning)
                    {
                        // remove this asteroid and the one it collided with from chunk lists
                        _allChunks[_currentGroupOfAsteroids[i].ChunkCoordinates].Remove(_currentGroupOfAsteroids[i]);
                        _allChunks[arbitraryAsteroid.ChunkCoordinates].Remove(arbitraryAsteroid);
                    }

                    CheckIfAsteroidIsReal(_currentGroupOfAsteroids[i]);
                }
                else
                {
                    _currentGroupOfAsteroids[i].RespawnTimer += Time.deltaTime * _numberOfGroups;
                    CheckRespawn(_currentGroupOfAsteroids[i]);
                }
            }
        }
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="simulatedAsteroid"></param>
    private void MoveAsteroid(SimulatedAsteroid simulatedAsteroid)
    {
        simulatedAsteroid.Position += simulatedAsteroid.Direction * simulatedAsteroid.Speed * Time.deltaTime * _numberOfGroups;
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="simulatedAsteroid"></param>
    private void CheckForNewChunk(SimulatedAsteroid simulatedAsteroid)
    {
        if (simulatedAsteroid.HasEnteredNewChunk()) // TODO: Unity profiler claims there's garbage allocation on this line
        {
            List<SimulatedAsteroid> asteroidList;
            if (_allChunks.TryGetValue(simulatedAsteroid.ChunkCoordinates, out asteroidList))
            {
                asteroidList.Add(simulatedAsteroid);
            }
            else
            {
                _asteroidListForAddingToDict = new List<SimulatedAsteroid>();
                _asteroidListForAddingToDict.Add(simulatedAsteroid);
                _allChunks.Add(simulatedAsteroid.ChunkCoordinates, _asteroidListForAddingToDict);
            }

            if (_allChunks.TryGetValue(simulatedAsteroid.PreviousChunkCoordinates, out asteroidList))
            {
                asteroidList.Remove(simulatedAsteroid);

                // Does end up helping with optimization - removes unnecessary chunks
                // Even without asteroids destroying each other the number of chunks ends up being almost halved almost immediately and more importantly it doesn't grow indefinitely
                if (_allChunks[simulatedAsteroid.PreviousChunkCoordinates].Count == 0)
                {
                    _allChunks.Remove(simulatedAsteroid.PreviousChunkCoordinates);
                }
            }

            simulatedAsteroid.UpdatePreviousChunkCoordinates();
        }
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="simulatedAsteroid"></param>
    private void ReAddToNewChunk(SimulatedAsteroid simulatedAsteroid)
    {
        simulatedAsteroid.UpdateChunkCoordinates();
        List<SimulatedAsteroid> asteroidList;
        if (_allChunks.TryGetValue(simulatedAsteroid.ChunkCoordinates, out asteroidList))
        {
            asteroidList.Add(simulatedAsteroid);
        }
        else
        {
            _asteroidListForAddingToDict = new List<SimulatedAsteroid>();
            _asteroidListForAddingToDict.Add(simulatedAsteroid);
            _allChunks.Add(simulatedAsteroid.ChunkCoordinates, _asteroidListForAddingToDict);
        }

        if (simulatedAsteroid.HasEnteredNewChunk()) // TODO: Unity profiler claims there's garbage allocation on this line
        {
            if (_allChunks.TryGetValue(simulatedAsteroid.PreviousChunkCoordinates, out asteroidList))
            {
                asteroidList.Remove(simulatedAsteroid);

                // Does end up helping with optimization - removes unnecessary chunks
                // Even without asteroids destroying each other the number of chunks ends up being almost halved almost immediately and more importantly it doesn't grow indefinitely
                if (_allChunks[simulatedAsteroid.PreviousChunkCoordinates].Count == 0)
                {
                    _allChunks.Remove(simulatedAsteroid.PreviousChunkCoordinates);
                }
            }
        }

        simulatedAsteroid.UpdatePreviousChunkCoordinates();
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="simulatedAsteroid"></param>
    private SimulatedAsteroid CheckCollisions(SimulatedAsteroid simulatedAsteroid)
    {
        for (int j = 0; j < _allChunks[simulatedAsteroid.ChunkCoordinates].Count; j++)
        {
            if ((simulatedAsteroid.Position - _allChunks[simulatedAsteroid.ChunkCoordinates][j].Position).magnitude < 0.2f && (simulatedAsteroid.Position - _allChunks[simulatedAsteroid.ChunkCoordinates][j].Position).magnitude != 0)
            {
                _regularCounter++;
                simulatedAsteroid.IsRespawning = true;
                _allChunks[simulatedAsteroid.ChunkCoordinates][j].IsRespawning = true;
                return _allChunks[simulatedAsteroid.ChunkCoordinates][j];
            }
        }

        return CheckNeighbouringChunks(simulatedAsteroid.ChunkCoordinates, simulatedAsteroid);
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="chunkCoordinates"></param>
    /// <param name="i"></param>
    private SimulatedAsteroid CheckNeighbouringChunks(Vector2Int chunkCoordinates, SimulatedAsteroid simulatedAsteroid)
    {
        // I tried inverting the logic and setting an asteroid to be in its and all neighbouring chunks,
        // and then just checking each asteroid against every asteroid only its own chunk but it ends up being slower
        // even though chunk changes happen less often than collision checks - probably due to extra adds and removes from the dictionary

        List<SimulatedAsteroid> asteroidList;

        _northChunkCoordinates = chunkCoordinates;
        _northChunkCoordinates.y = chunkCoordinates.y + 1;

        // Dictionary optimization - TryGetValue here does 1 lookup, ContainsKey + manipulation would do 2.
        if (_allChunks.TryGetValue(_northChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                {
                    _orthoCounter++;
                    simulatedAsteroid.IsRespawning = true;
                    asteroidList[j].IsRespawning = true;
                    return asteroidList[j];
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        _southChunkCoordinates = chunkCoordinates;
        _southChunkCoordinates.y = chunkCoordinates.y - 1;

        if (_allChunks.TryGetValue(_southChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                {
                    _orthoCounter++;
                    simulatedAsteroid.IsRespawning = true;
                    asteroidList[j].IsRespawning = true;
                    return asteroidList[j];
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        _eastChunkCoordinates = chunkCoordinates;
        _eastChunkCoordinates.x = chunkCoordinates.x + 1;

        if (_allChunks.TryGetValue(_eastChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                {
                    _orthoCounter++;
                    simulatedAsteroid.IsRespawning = true;
                    asteroidList[j].IsRespawning = true;
                    return asteroidList[j];
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        _westChunkCoordinates = chunkCoordinates;
        _westChunkCoordinates.x = chunkCoordinates.x - 1;

        if (_allChunks.TryGetValue(_westChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                {
                    _orthoCounter++;
                    simulatedAsteroid.IsRespawning = true;
                    asteroidList[j].IsRespawning = true;
                    return asteroidList[j];
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        if (diagonalChecks)
        {
            _northEastChunkCoordinates = chunkCoordinates;
            _northEastChunkCoordinates.x = chunkCoordinates.x + 1;
            _northEastChunkCoordinates.y = chunkCoordinates.y + 1;

            if (_allChunks.TryGetValue(_northEastChunkCoordinates, out asteroidList))
            {
                for (int j = 0; j < asteroidList.Count; j++)
                {
                    if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                    {
                        _diagCounter++;
                        simulatedAsteroid.IsRespawning = true;
                        asteroidList[j].IsRespawning = true;
                        return asteroidList[j];
                        //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                    }
                }
            }

            _southEastChunkCoordinates = chunkCoordinates;
            _southEastChunkCoordinates.x = chunkCoordinates.x + 1;
            _southEastChunkCoordinates.y = chunkCoordinates.y - 1;


            if (_allChunks.TryGetValue(_southEastChunkCoordinates, out asteroidList))
            {
                for (int j = 0; j < asteroidList.Count; j++)
                {
                    if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                    {
                        _diagCounter++;
                        simulatedAsteroid.IsRespawning = true;
                        asteroidList[j].IsRespawning = true;
                        return asteroidList[j];
                        //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                    }
                }
            }

            _northWestChunkCoordinates = chunkCoordinates;
            _northWestChunkCoordinates.x = chunkCoordinates.x - 1;
            _northWestChunkCoordinates.y = chunkCoordinates.y + 1;

            if (_allChunks.TryGetValue(_northWestChunkCoordinates, out asteroidList))
            {
                for (int j = 0; j < asteroidList.Count; j++)
                {
                    if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                    {
                        _diagCounter++;
                        simulatedAsteroid.IsRespawning = true;
                        asteroidList[j].IsRespawning = true;
                        return asteroidList[j];
                        //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                    }
                }
            }

            _southWestChunkCoordinates = chunkCoordinates;
            _southWestChunkCoordinates.x = chunkCoordinates.x - 1;
            _southWestChunkCoordinates.y = chunkCoordinates.y - 1;

            if (_allChunks.TryGetValue(_southWestChunkCoordinates, out asteroidList))
            {
                for (int j = 0; j < asteroidList.Count; j++)
                {
                    if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                    {
                        _diagCounter++;
                        simulatedAsteroid.IsRespawning = true;
                        asteroidList[j].IsRespawning = true;
                        return asteroidList[j];
                        //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// TODO: Probably not needed, the chance of running into an undetected asteroid in a neighbouring *diagonal* chunk is very low.
    /// </summary>
    /// <param name="chunkCoordinates"></param>
    /// <param name="i"></param>
    private SimulatedAsteroid CheckDiagonallyNeighbouringChunks(Vector2Int chunkCoordinates, SimulatedAsteroid simulatedAsteroid)
    {
        _northEastChunkCoordinates = chunkCoordinates;
        _northEastChunkCoordinates.x = chunkCoordinates.x + 1;
        _northEastChunkCoordinates.y = chunkCoordinates.y + 1;

        _southEastChunkCoordinates = chunkCoordinates;
        _southEastChunkCoordinates.x = chunkCoordinates.x + 1;
        _southEastChunkCoordinates.y = chunkCoordinates.y - 1;

        _northWestChunkCoordinates = chunkCoordinates;
        _northWestChunkCoordinates.x = chunkCoordinates.x - 1;
        _northWestChunkCoordinates.y = chunkCoordinates.y + 1;

        _southWestChunkCoordinates = chunkCoordinates;
        _southWestChunkCoordinates.x = chunkCoordinates.x - 1;
        _southWestChunkCoordinates.y = chunkCoordinates.y - 1;

        List<SimulatedAsteroid> asteroidList;

        if (_allChunks.TryGetValue(_northEastChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                {
                    simulatedAsteroid.IsRespawning = true;
                    asteroidList[j].IsRespawning = true;
                    return asteroidList[j];
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }


        if (_allChunks.TryGetValue(_southEastChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                {
                    simulatedAsteroid.IsRespawning = true;
                    asteroidList[j].IsRespawning = true;
                    return asteroidList[j];
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }


        if (_allChunks.TryGetValue(_northWestChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                {
                    simulatedAsteroid.IsRespawning = true;
                    asteroidList[j].IsRespawning = true;
                    return asteroidList[j];
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }


        if (_allChunks.TryGetValue(_southWestChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (HasCollided(simulatedAsteroid.Position, asteroidList[j].Position))
                {
                    simulatedAsteroid.IsRespawning = true;
                    asteroidList[j].IsRespawning = true;
                    return asteroidList[j];
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        return null;
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="simulatedAsteroid"></param>
    private void CheckRespawn(SimulatedAsteroid simulatedAsteroid)
    {
        if (simulatedAsteroid.RespawnTimer >= _respawnTime)
        {
            simulatedAsteroid.RespawnTimer = 0;
            simulatedAsteroid.IsRespawning = false;
            RespawnAsteroid(simulatedAsteroid);
        }
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="simulatedAsteroid"></param>
    private void RespawnAsteroid(SimulatedAsteroid simulatedAsteroid)
    {
        Vector2 newPosition;

        bool isXPositive = Random.Range(0, 2) > 0;
        bool isYPositive = Random.Range(0, 2) > 0;
        // TODO: Change to just outside camera view, or rather, make constant
        if (isXPositive)
        {
            newPosition.x = player.position.x + Random.Range(6f, _gridWidth);
        }
        else
        {
            newPosition.x = player.position.x - Random.Range(6f, _gridWidth);
        }

        if (isYPositive)
        {
            newPosition.y = player.position.y + Random.Range(6f, _gridLength);
        }
        else
        {
            newPosition.y = player.position.y - Random.Range(6f, _gridLength);
        }

        // TODO: TEST DATA
        //if (isXPositive)
        //{
        //    newPosition.x = player.position.x + Random.Range(1f, 5f);
        //}
        //else
        //{
        //    newPosition.x = player.position.x - Random.Range(1f, 5f);
        //}

        //if (isYPositive)
        //{
        //    newPosition.y = player.position.y + Random.Range(1f, 5f);
        //}
        //else
        //{
        //    newPosition.y = player.position.y - Random.Range(1f, 5f);
        //}

        simulatedAsteroid.UpdatePosition(newPosition);

        ReAddToNewChunk(simulatedAsteroid);
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="asteroid1Position"></param>
    /// <param name="asteroid2Position"></param>
    /// <returns></returns>
    private bool HasCollided(Vector2 asteroid1Position, Vector2 asteroid2Position)
    {
        // Checking against 0.3f because that's 1 asteroid's diameter - and we're checking whether they're at least touching tips - meaning if they're 2 radiuses = 1 diameter away from each other.
        // TODO: also probably make constant
        if ((asteroid1Position - asteroid2Position).magnitude < 0.3f)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="asteroidPosition"></param>
    private void CheckIfAsteroidIsReal(SimulatedAsteroid simulatedAsteroid)
    {
        float distance = Vector2.Distance(simulatedAsteroid.Position, new Vector2(player.position.x, player.position.y));
        if (distance < 10) // asteroid is close enough
        {
            asteroidManager.MakeAsteroidReal(simulatedAsteroid);
            simulatedAsteroid.IsSimulated = false;
        }
    }
}
