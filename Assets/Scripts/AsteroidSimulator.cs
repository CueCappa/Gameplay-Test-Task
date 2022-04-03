using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simulates all offscreen asteroids to save processing power, since unity colliders are expensive
/// </summary>
public class AsteroidSimulator : MonoBehaviour
{
    // Time in seconds after which a destroyed asteroid respawns.
    private const int _respawnTime = 1;

    [SerializeField] private int _gridLength;
    [SerializeField] private int _gridWidth;

    [SerializeField] private float _minSpeed;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _asteroidSize;

    [SerializeField] private Transform _player;
    [SerializeField] private AsteroidManager _asteroidManager;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Camera _mainCamera;

    [Header("Read the tooltips - performance enhancing drugs")]
    [Tooltip("This defines the number of off-screen calculation groups. Each frame 1 group of asteroids is checked for new chunks and collisions. " +
        "In short, this means one thing - since our target is 60 FPS and an accurate off-screen simulation - this number should be 12 or lower. But if the game does not run well, increase this number until 60 FPS." +
        "Because the off-screen simulation accuracy is less accurate when this number is higher and more accurate with higher FPS: running 12 groups at 30 or 24 groups at 60 FPS is the same.")]
    [SerializeField]
    private int _numberOfGroups;

    [Tooltip("This boolean is here to set whether the simulated asteroids should check if they collided with another asteroid in a diagonally neighbouring chunk. " +
        "This constitutes around 5% of all off-screen collisions but slows down performance by 35%. ")]
    [SerializeField]
    private bool _diagonalChecks;

    private int _asteroidIdCounter;
    private int _asteroidGroupCounter;

    private float _justOutsideCamera = 7;

    // I am leaving these in for your testing purposes as well
    private float _testTimer;
    private int _regularCounter;
    private int _orthoCounter;
    private int _diagCounter;

    // For manipulation during spawning.
    private SimulatedAsteroid _asteroidToSpawn = new SimulatedAsteroid();

    // A list containing calculation groups of simulation asteroids.
    // This used to be an array and the number of groups a constant, but then the number of groups could not be changed from inspector and it being a list does not detectably affect performance.
    private List<List<SimulatedAsteroid>> _allAsteroidsSplitIntoGroups = new List<List<SimulatedAsteroid>>();

    // Dictionary of all grid chunks with coordinates as key and a list of all asteroids in the chunk as value - used for collision checks between asteroids.
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
    /// TODO: Split into initialization methods
    /// </summary>
    private void Start()
    {
        _asteroidIdCounter = 0;
        _asteroidGroupCounter = 0;
        _justOutsideCamera = _mainCamera.orthographicSize * Screen.width / Screen.height + 1;

        // We initialize the empty asteroid groups
        for (int g = 0; g < _numberOfGroups; g++)
        {
            _allAsteroidsSplitIntoGroups.Add(new List<SimulatedAsteroid>());
        }

        _allAsteroidsSplitIntoGroups.TrimExcess();

        // We create the simulated asteroids and assign them to their groups.
        for (int i = 0; i < _gridWidth; i++)
        {
            for (int j = 0; j < _gridLength; j++)
            {
                _asteroidToSpawn = new SimulatedAsteroid(Random.Range(_minSpeed, _maxSpeed),
                                                new Vector2((i - _gridLength / 2) * 2, (j - _gridWidth / 2) * 2), // We set them 2 units apart each at start
                                                new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized,
                                                _asteroidIdCounter);

                _asteroidIdCounter++;

                // First we determine the calculation group the asteroid should go into.
                _allAsteroidsSplitIntoGroups[_asteroidGroupCounter].Add(_asteroidToSpawn);

                _asteroidGroupCounter++;
                if (_asteroidGroupCounter == _numberOfGroups)
                {
                    _asteroidGroupCounter = 0;
                }

                // Now we add the asteroid to its spawn chunk's list.
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
            }
        }

        // After we're done with generation, we reset group counter so it can be reused in fixed update.
        _asteroidGroupCounter = 0;
        _testTimer = 0;
    }

    private void Update()
    {
        // Every x frames we check an asteroid group's state, where x is total number of groups.
        if (_asteroidGroupCounter >= _numberOfGroups) // TODO: Unit profiler claims there's garbage allocation on this line, I don't see how that is possible
        {
            _asteroidGroupCounter = 0;
        }

        if (!_gameManager.GamePaused)
        {
            SimulateAsteroidByGroup(_asteroidGroupCounter);
            _asteroidGroupCounter++;
        }

        // I am leaving this in for your testing purposes as well, since I did not get an answer.
        if (_testTimer > 5)
        {
            Debug.Log(_testTimer.ToString("0.00") + " seconds has passed.");
            Debug.Log("Same-chunk collisions: " + _regularCounter);
            Debug.Log("Orthogonal collisions: " + _orthoCounter);
            Debug.Log("Diagonal collisions: " + _diagCounter);
            Debug.Log("Total asteroid collisions: " + (_regularCounter + _orthoCounter + _diagCounter).ToString());

            _testTimer = 0;
            _regularCounter = 0;
            _orthoCounter = 0;
            _diagCounter = 0;
        }
        else
        {
            _testTimer += Time.deltaTime;
        }
    }

    /// <summary>
    /// Goes through all of the asteroids in a given group, performs all necessary checks and actions for each.
    /// </summary>
    /// <param name="groupId"> The number designating the current group of asteroids. </param>
    private void SimulateAsteroidByGroup(int groupId)
    {
        // Unity profiler claims there's garbage allocation on this line, but if I don't use the variable the alloc just moves to the start of the for loop??
        _currentGroupOfAsteroids = _allAsteroidsSplitIntoGroups[groupId];

        // We do all necessary checks in a single loop
        for (int i = 0; i < _currentGroupOfAsteroids.Count; i++)
        {
            // Only simulate asteroids that are not on screen
            if (_currentGroupOfAsteroids[i].IsSimulated)
            {
                // Only check for asteroids which are not currently respawning
                if (!_currentGroupOfAsteroids[i].IsRespawning)
                {
                    MoveAsteroid(_currentGroupOfAsteroids[i]);
                    CheckForNewChunk(_currentGroupOfAsteroids[i]);

                    // It takes two to collide, this asteroid is the one the currently checked one has collided with
                    SimulatedAsteroid otherCollidedAsteroid;
                    otherCollidedAsteroid = CheckCollisions(_currentGroupOfAsteroids[i]);

                    // Check if the asteroid collided with something
                    if (_currentGroupOfAsteroids[i].IsRespawning)
                    {
                        // Remove this asteroid and the one it collided with from chunk lists
                        _allChunks[_currentGroupOfAsteroids[i].ChunkCoordinates].Remove(_currentGroupOfAsteroids[i]);
                        _allChunks[otherCollidedAsteroid.ChunkCoordinates].Remove(otherCollidedAsteroid);
                    }

                    // This checks if the asteroid is close enough to the player to stop simulating here and start being represented by a game object.
                    CheckIfAsteroidIsReal(_currentGroupOfAsteroids[i]);
                }
                else
                {
                    _currentGroupOfAsteroids[i].RespawnTimer += Time.deltaTime * _numberOfGroups;
                    // Check if the asteroid has been dead long enough and is ready to respawn.
                    CheckRespawn(_currentGroupOfAsteroids[i]);
                }
            }
        }
    }

    /// <summary>
    /// Moves the given asteroid. 
    /// </summary>
    /// <param name="simulatedAsteroid"> The asteroid that needs moving. </param>
    private void MoveAsteroid(SimulatedAsteroid simulatedAsteroid)
    {
        // We multiply by [_numberOfGroups] here because the movement happens every [_numberOfGroups] frames.
        simulatedAsteroid.Position += simulatedAsteroid.Direction * simulatedAsteroid.Speed * Time.deltaTime * _numberOfGroups;
    }

    /// <summary>
    /// Checks if the given asteroid has entered a new grid chunk. 
    /// </summary>
    /// <param name="simulatedAsteroid"> The asteroid that is being checked. </param>
    private void CheckForNewChunk(SimulatedAsteroid simulatedAsteroid)
    {
        if (simulatedAsteroid.HasEnteredNewChunk()) // Unity profiler claims there's garbage allocation on this line :/
        {
            // List for dictionary manipulation.
            List<SimulatedAsteroid> asteroidList;

            // Since we are immediately manipulating the given value, TryGetValue is more optimal than ContainsKey.
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

                // This helps quite a bit with optimization - removes unnecessary chunks
                // Even without asteroids destroying each other the number of chunks ends up being almost halved almost immediately and more importantly it doesn't grow indefinitely
                if (asteroidList.Count == 0)
                {
                    _allChunks.Remove(simulatedAsteroid.PreviousChunkCoordinates);
                }
            }

            simulatedAsteroid.UpdatePreviousChunkCoordinates();
        }
    }

    /// <summary>
    /// Adds the given asteroid to the chunk list where it just respawned. 
    /// </summary>
    /// <param name="simulatedAsteroid"> The asteroid to add to list. </param>
    private void AddToRespawnChunk(SimulatedAsteroid simulatedAsteroid)
    {
        simulatedAsteroid.UpdateChunkCoordinates();

        // List for dictionary manipulation.
        List<SimulatedAsteroid> asteroidList;

        // Since we are immediately manipulating the given value, TryGetValue is more optimal than ContainsKey.
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

        // This check is here just in case the asteroid has respawned in the same chunk it was destroyed in.
        if (simulatedAsteroid.HasEnteredNewChunk())
        {
            if (_allChunks.TryGetValue(simulatedAsteroid.PreviousChunkCoordinates, out asteroidList))
            {
                asteroidList.Remove(simulatedAsteroid);

                // Helps with optimization - removes unnecessary chunks
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
    /// Checks collision between the given asteroid and every asteroid in its chunk. 
    /// </summary>
    /// <param name="simulatedAsteroid"> Asteroid which needs checking. </param>
    /// <returns> A simulated asteroid if there was a collision, null if there was no collision. </returns>
    private SimulatedAsteroid CheckCollisions(SimulatedAsteroid simulatedAsteroid)
    {
        for (int j = 0; j < _allChunks[simulatedAsteroid.ChunkCoordinates].Count; j++)
        {
            if ((simulatedAsteroid.Position - _allChunks[simulatedAsteroid.ChunkCoordinates][j].Position).magnitude < 0.5f && (simulatedAsteroid.Position - _allChunks[simulatedAsteroid.ChunkCoordinates][j].Position).magnitude != 0)
            {
                _regularCounter++;
                simulatedAsteroid.IsRespawning = true;
                _allChunks[simulatedAsteroid.ChunkCoordinates][j].IsRespawning = true;
                return _allChunks[simulatedAsteroid.ChunkCoordinates][j];
            }
        }

        return CheckNeighbouringChunks(simulatedAsteroid.ChunkCoordinates, simulatedAsteroid, _diagonalChecks);
    }

    /// <summary>
    /// Checks collision between the given asteroid and every asteroid in neighbouring chunks.
    /// </summary>
    /// <param name="chunkCoordinates"> Which chunk's neighbours do we need to check. </param>
    /// <param name="simulatedAsteroid"> Which asteroid do we need to check for collisions. </param>
    /// <param name="checkDiagonal"> Do we check the diagonal neighbors or not? </param>
    /// <returns> A simulated asteroid if there was a collision, null if there was no collision. </returns>
    private SimulatedAsteroid CheckNeighbouringChunks(Vector2Int chunkCoordinates, SimulatedAsteroid simulatedAsteroid, bool checkDiagonal)
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
                    // Counter here counts number of detected collisions through orthogonal chunk borders.
                    // TODO: Not sure if to remove or if you guys want to use it to test the numbers as well.
                    _orthoCounter++;
                    simulatedAsteroid.IsRespawning = true;
                    asteroidList[j].IsRespawning = true;
                    return asteroidList[j];
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
                }
            }
        }

        if (checkDiagonal)
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
                        // Counter here counts number of detected collisions through diagonal chunk borders.
                        // This is what I used to see that diagonal collisions only constitute 5% of all collisions.
                        // TODO: Not sure if to remove or if you guys want to use it to test the numbers as well.
                        _diagCounter++;
                        simulatedAsteroid.IsRespawning = true;
                        asteroidList[j].IsRespawning = true;
                        return asteroidList[j];
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
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a given asteroid need respawning and if yes it calls the respawn function immediately. 
    /// </summary>
    /// <param name="simulatedAsteroid"> Asteroid that needs checking. </param>
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
    /// Respawns the asteroid at a random location outside view. 
    /// </summary>
    /// <param name="simulatedAsteroid"> Asteroid to respawn. </param>
    private void RespawnAsteroid(SimulatedAsteroid simulatedAsteroid)
    {
        Vector2 newPosition;

        bool isXPositive = Random.Range(0, 2) > 0;
        bool isYPositive = Random.Range(0, 2) > 0;

        if (isXPositive)
        {
            newPosition.x = _player.position.x + Random.Range(_justOutsideCamera, _gridWidth);
        }
        else
        {
            newPosition.x = _player.position.x - Random.Range(_justOutsideCamera, _gridWidth);
        }

        if (isYPositive)
        {
            newPosition.y = _player.position.y + Random.Range(_justOutsideCamera, _gridLength);
        }
        else
        {
            newPosition.y = _player.position.y - Random.Range(_justOutsideCamera, _gridLength);
        }

        simulatedAsteroid.Position = newPosition;

        AddToRespawnChunk(simulatedAsteroid);
    }

    /// <summary>
    /// Checks if 2 given asteroids are close enough to collide.
    /// </summary>
    /// <param name="asteroid1Position"> First asteroid to check. </param>
    /// <param name="asteroid2Position"> Second asteroid to check. </param>
    /// <returns></returns>
    private bool HasCollided(Vector2 asteroid1Position, Vector2 asteroid2Position)
    {
        // Checking against 0.5f because that's 1 asteroid's diameter - and we're checking whether they're at least touching tips - meaning if they're 2 radiuses = 1 diameter away from each other.
        if ((asteroid1Position - asteroid2Position).magnitude < _asteroidSize)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if an asteroid got close enough to the player to start using a game object instead.
    /// </summary>
    /// <param name="asteroidPosition"></param>
    private void CheckIfAsteroidIsReal(SimulatedAsteroid simulatedAsteroid)
    {
        float distance = Vector2.Distance(simulatedAsteroid.Position, new Vector2(_player.position.x, _player.position.y));

        if (distance < _justOutsideCamera) // asteroid is close enough
        {
            _asteroidManager.MakeAsteroidReal(simulatedAsteroid);
            simulatedAsteroid.IsSimulated = false;
        }
    }
}
