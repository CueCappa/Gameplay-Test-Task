using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all asteroids close enough to the player.
/// </summary>
public class AsteroidManager : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Transform _player;

    [Header("Asteroid pool settings")]
    [SerializeField] private int _amountOfAsteroids;
    [SerializeField] private GameObject _objectToPool;
    [SerializeField] private Transform _asteroidParent;
    [SerializeField] private List<GameObject> _asteroidPool = new List<GameObject>();

    private float _justOutsideCamera;

    // Dictionary connecting simulated asteroids and their game object counterparts - has to be whole SimulatedAsteroid object because we have to modify it from here
    // Because I'm rushing to finish this part at this point and this is less important to optimize than other things
    private Dictionary<GameObject, SimulatedAsteroid> _linkToSimulation = new Dictionary<GameObject, SimulatedAsteroid>();

    private void Start()
    {
        _justOutsideCamera = _mainCamera.orthographicSize * Screen.width / Screen.height + 1;

        GameObject asteroid;
        for (int i = 0; i < _amountOfAsteroids; i++)
        {
            asteroid = Instantiate(_objectToPool, _asteroidParent);
            asteroid.SetActive(false);
            _asteroidPool.Add(asteroid);
        }
    }

    private void Update()
    {
        for (int i = 0; i < _asteroidPool.Count; i++)
        {
            // First we check if the asteroid is inactive - meaning it's dead
            if (!_asteroidPool[i].activeInHierarchy)
            {
                // If it's inactive, we make sure it's not in any lists and dictionaries it shouldn't be in.
                if (_linkToSimulation.ContainsKey(_asteroidPool[i]))
                {
                    _linkToSimulation[_asteroidPool[i]].IsSimulated = true;
                    _linkToSimulation[_asteroidPool[i]].IsRespawning = true;
                    _linkToSimulation.Remove(_asteroidPool[i]);
                }
            }
            else
            {
                // If it's active, we check if it got too far from the player, if it did we start only simulating it in the background again.
                float distance = Vector2.Distance(_player.position, _asteroidPool[i].transform.position);
                if (distance > _justOutsideCamera)
                {
                    _linkToSimulation[_asteroidPool[i]].IsSimulated = true;
                    _linkToSimulation[_asteroidPool[i]].Position = new Vector2(_asteroidPool[i].transform.position.x, _asteroidPool[i].transform.position.y);
                    _linkToSimulation.Remove(_asteroidPool[i]);
                    _asteroidPool[i].SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Get an asteroid from the pool that is currently not in use. 
    /// </summary>
    /// <returns> An inactive asteroid from the pool, or null if there is no one. </returns>
    public GameObject GetPooledAsteroid()
    {
        for (int i = 0; i < _amountOfAsteroids; i++)
        {
            if (!_asteroidPool[i].activeInHierarchy)
            {
                return _asteroidPool[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Takes a simualated asteroid's current status and applies them to a game object.
    /// </summary>
    /// <param name="simulatedAsteroid"> Simulated asteroid that needs to become real. </param>
    public void MakeAsteroidReal(SimulatedAsteroid simulatedAsteroid)
    {
        GameObject asteroid = GetPooledAsteroid();

        // If all asteroids are in use we expand the pool by one - just in case
        if (asteroid == null)
        {
            asteroid = Instantiate(_objectToPool, _asteroidParent);
            _asteroidPool.Add(asteroid);
        }
        // If there is an inactive asteroid we use that one.
        else
        {
            // Since GetPooledAsteroid will only give us inactive asteroids, it means that the asteroid is already dead.
            // But since we can't remove it from the dictionary from the asteroid itself, it is best to do that check here just in case, as well as in update.
            // To make sure everything is correct and to avoid any bugs, we also set its simulated properties here.
            if (_linkToSimulation.ContainsKey(asteroid))
            {
                _linkToSimulation[asteroid].IsSimulated = true;
                _linkToSimulation[asteroid].IsRespawning = true;
                _linkToSimulation.Remove(asteroid);
            }
        }

        // Now that we are sure the asteroid is completely dead and removed from all necessary lists - we make it live again.
        asteroid.SetActive(true);

        Rigidbody2D roidRB = asteroid.GetComponent<Rigidbody2D>();
        Vector2 velocity = simulatedAsteroid.Direction * simulatedAsteroid.Speed;

        asteroid.transform.position = new Vector3(simulatedAsteroid.Position.x, simulatedAsteroid.Position.y, 0);
        roidRB.velocity = velocity;
        simulatedAsteroid.IsSimulated = false;
        _linkToSimulation.Add(asteroid, simulatedAsteroid);
    }
}
