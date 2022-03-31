using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script is supposed to simulate all offscreen asteroids to save processing power, since unity colliders are expensive
/// </summary>
public class AsteroidSimulator : MonoBehaviour
{
    [SerializeField] private int _gridLength;
    [SerializeField] private int _gridWidth;

    [SerializeField] private float _minSpeed;
    [SerializeField] private float _maxSpeed;

    private int _idCounter;
    private int _groupCounter;
    private bool _chunkChecker;

    // For manipulation during spawning.
    private Asteroid _asteroidToSpawn = new Asteroid();

    // Creating x different groups of asteroids so I can split checking them into different frames.
    private const int _numberOfGroups = 12;
    private List<Asteroid>[] _allAsteroidsSplitIntoGroups = new List<Asteroid>[_numberOfGroups]; // array size must be number of groups - const

    // List of asteroids used to manipulate dictionary values.
    private List<Asteroid> _asteroidListForAddingToDict = new List<Asteroid>();

    // Makes a small piece of code more readable later.
    private List<Asteroid> _currentGroupOfAsteroids = new List<Asteroid>();

    // Constantly reused variables for readability when checking bordering chunks later on.
    private Vector2Int _northChunkCoordinates = new Vector2Int();
    private Vector2Int _southChunkCoordinates = new Vector2Int();
    private Vector2Int _eastChunkCoordinates = new Vector2Int();
    private Vector2Int _westChunkCoordinates = new Vector2Int();

    private Vector2Int _northEastChunkCoordinates = new Vector2Int();
    private Vector2Int _southEastChunkCoordinates = new Vector2Int();
    private Vector2Int _northWestChunkCoordinates = new Vector2Int();
    private Vector2Int _southWestChunkCoordinates = new Vector2Int();

    // list of all chunks with coordinates as key and a list of all asteroids in the chunk as value
    private Dictionary<Vector2Int, List<Asteroid>> _allChunks = new Dictionary<Vector2Int, List<Asteroid>>();


    private void Start()
    {
        _idCounter = 0;
        _groupCounter = 0;

        // We initialize the empty asteroid groups
        for (int g = 0; g < _numberOfGroups; g++)
        {
            _allAsteroidsSplitIntoGroups[g] = new List<Asteroid>();
        }

        // We create the asteroids and assign them to their groups.
        for (int i = 0; i < _gridLength; i++)
        {
            for (int j = 0; j < _gridWidth; j++)
            {
                _asteroidToSpawn = new Asteroid(Random.Range(_minSpeed, _maxSpeed),
                                                new Vector2(i - _gridLength / 2, j - _gridWidth / 2),
                                                new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized,
                                                _idCounter,
                                                false);

                _idCounter++;


                // First we add the asteroid to its chunk's list.
                if (_allChunks.ContainsKey(_asteroidToSpawn.ChunkCoordinates))
                {
                    _allChunks[_asteroidToSpawn.ChunkCoordinates].Add(_asteroidToSpawn);
                }
                else
                {
                    _asteroidListForAddingToDict = new List<Asteroid>();
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
    }

    private void Update()
    {
        // Every x frames we check an asteroid group's state, where x is total number of groups
        // This splits the load across multiple frames, and the off-screen simulation does not have to be perfect.
        // By my calculations, with the current asteroid and player max speed and current asteroid size, it is still reasonably accurate up to 12 or 15 groups.
        if (_groupCounter >= _numberOfGroups)
        {
            _groupCounter = 0;
        }

        CheckAsteroidStatesbyGroup(_groupCounter);
        _groupCounter++;

    }

    private void FixedUpdate()
    {
        //CheckAsteroidStatesbyGroup();

        //_groupCounter++;

        //if (_groupCounter >= 5)
        //{
        //    _groupCounter = 0;
        //}
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    private void CheckAsteroidStatesbyGroup(int groupId)
    {
        _currentGroupOfAsteroids = _allAsteroidsSplitIntoGroups[groupId];

        for (int i = 0; i < _currentGroupOfAsteroids.Count; i++)
        {
            MoveAsteroid(_currentGroupOfAsteroids[i]);
            CheckForNewChunk(_currentGroupOfAsteroids[i]);
            CheckCollisionWithinChunk(_currentGroupOfAsteroids[i]);
        }
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="asteroid"></param>
    private void MoveAsteroid(Asteroid asteroid)
    {
        asteroid.Position += asteroid.Direction * asteroid.Speed * Time.fixedDeltaTime * _numberOfGroups;
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="asteroid"></param>
    private void CheckForNewChunk(Asteroid asteroid)
    {
        _chunkChecker = asteroid.HasEnteredNewChunk();
        if (_chunkChecker)
        {
            if (_allChunks.ContainsKey(asteroid.ChunkCoordinates))
            {
                _allChunks[asteroid.ChunkCoordinates].Add(asteroid);
            }
            else
            {
                _asteroidListForAddingToDict = new List<Asteroid>();
                _asteroidListForAddingToDict.Add(asteroid);
                _allChunks.Add(asteroid.ChunkCoordinates, _asteroidListForAddingToDict);
            }

            _allChunks[asteroid.PreviousChunkCoordinates].Remove(asteroid);

            // Does end up helping with optimization - removes unnecessary chunks
            // Even without asteroids destroying each other the number of chunks ends up being almost halved almost immediately and most importantly it doesn't grow
            if (_allChunks[asteroid.PreviousChunkCoordinates].Count == 0)
            {
                _allChunks.Remove(asteroid.PreviousChunkCoordinates);
            }

            asteroid.UpdatePreviousChunkCoordinates();
        }
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="asteroid"></param>
    private void CheckCollisionWithinChunk(Asteroid asteroid)
    {
        for (int j = 0; j < _allChunks[asteroid.ChunkCoordinates].Count; j++)
        {
            if ((asteroid.Position - _allChunks[asteroid.ChunkCoordinates][j].Position).magnitude < 0.3f)
            {
                //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
            }
        }

        CheckOrthogonallyNeighbouringChunks(asteroid.ChunkCoordinates, asteroid);
        //CheckDiagonallyNeighbouringChunks(asteroid.ChunkCoordinates, asteroid);
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="chunkCoordinates"></param>
    /// <param name="i"></param>
    private void CheckOrthogonallyNeighbouringChunks(Vector2Int chunkCoordinates, Asteroid asteroid)
    {
        _northChunkCoordinates = chunkCoordinates;
        _northChunkCoordinates.y = chunkCoordinates.y + 1;

        _southChunkCoordinates = chunkCoordinates;
        _southChunkCoordinates.y = chunkCoordinates.y - 1;

        _eastChunkCoordinates = chunkCoordinates;
        _eastChunkCoordinates.x = chunkCoordinates.x + 1;

        _westChunkCoordinates = chunkCoordinates;
        _westChunkCoordinates.x = chunkCoordinates.x - 1;

        List<Asteroid> asteroidList;

        // Dictionary optimization - TryGetValue here does 1 lookup, ContainsKey + manipulation would do 2.
        if (_allChunks.TryGetValue(_northChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (CheckCollision(asteroid.Position, asteroidList[j].Position))
                {
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        if (_allChunks.TryGetValue(_southChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (CheckCollision(asteroid.Position, asteroidList[j].Position))
                {
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        if (_allChunks.TryGetValue(_eastChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (CheckCollision(asteroid.Position, asteroidList[j].Position))
                {
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        if (_allChunks.TryGetValue(_westChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (CheckCollision(asteroid.Position, asteroidList[j].Position))
                {
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }
    }

    /// <summary>
    /// TODO: Probably not needed, the chance of running into an undetected asteroid in a neighbouring *diagonal* chunk is very low.
    /// </summary>
    /// <param name="chunkCoordinates"></param>
    /// <param name="i"></param>
    private void CheckDiagonallyNeighbouringChunks(Vector2Int chunkCoordinates, Asteroid asteroid)
    {
        List<Asteroid> asteroidList;

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


        if (_allChunks.TryGetValue(_northEastChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (CheckCollision(asteroid.Position, asteroidList[j].Position))
                {
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        if (_allChunks.TryGetValue(_southEastChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (CheckCollision(asteroid.Position, asteroidList[j].Position))
                {
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        if (_allChunks.TryGetValue(_northWestChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (CheckCollision(asteroid.Position, asteroidList[j].Position))
                {
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        if (_allChunks.TryGetValue(_southWestChunkCoordinates, out asteroidList))
        {
            for (int j = 0; j < asteroidList.Count; j++)
            {
                if (CheckCollision(asteroid.Position, asteroidList[j].Position))
                {
                    //Debug.Log("Asteroids " + _allAsteroids[i].Id + " and " + _allChunks[_allAsteroids[i].ChunkCoordinates][j].Id + " have been destroyed.");
                }
            }
        }

        //if (testCounter > 0)
        //{
        //    Debug.Log("Asteroida destroyed in DIAGONAL chunks.");
        //}
    }

    private bool CheckCollision(Vector2 asteroid1Position, Vector2 asteroid2Position)
    {
        // Checking against 0.3f because that's 1 asteroid's diameter - and we're checking whether they're touching tips.
        // TODO: also probably make constant
        if ((asteroid1Position - asteroid2Position).magnitude < 0.3f)
        {
            return true;
        }
        return false;
    }
}
