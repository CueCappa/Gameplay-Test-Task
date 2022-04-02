using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Denotes an asteroid and handles its behaviour, for now.
/// </summary>
public class AsteroidManager : MonoBehaviour
{
    [SerializeField] int _amountOfAsteroids;
    [SerializeField] private GameObject _objectToPool;
    [SerializeField] private Transform _asteroidParent;
    [SerializeField] private List<GameObject> _asteroidPool = new List<GameObject>();

    [SerializeField] private Transform player;

    private List<GameObject> _usedAsteroids = new List<GameObject>();

    // Dictionary connecting simulated asteroids and their game object counterparts - has to be whole SimulatedAsteroid object because we have to modify it from here
    // Because I'm rushing to finish this part at this point and this is less important to optimize than other things
    private Dictionary<GameObject, SimulatedAsteroid> _link = new Dictionary<GameObject, SimulatedAsteroid>();

    private void Start()
    {
        GameObject temp;
        for (int i = 0; i < _amountOfAsteroids; i++)
        {
            temp = Instantiate(_objectToPool, _asteroidParent);
            temp.SetActive(false);
            _asteroidPool.Add(temp);
        }
    }

    private void Update()
    {
        for (int i = 0; i < _amountOfAsteroids; i++)
        {
            if (!_asteroidPool[i].activeInHierarchy)
            {
                if (_link.ContainsKey(_asteroidPool[i]))
                {
                    _link[_asteroidPool[i]].IsSimulated = true;
                    _link[_asteroidPool[i]].IsRespawning = true;
                    _link.Remove(_asteroidPool[i]);
                }
            }
            else
            {
                float distance = Vector2.Distance(player.position, _asteroidPool[i].transform.position);
                if (distance > 6) // check if too far from player
                {
                    _link[_asteroidPool[i]].IsSimulated = true;
                    _link[_asteroidPool[i]].Position = new Vector2(_asteroidPool[i].transform.position.x, _asteroidPool[i].transform.position.y);
                    _link.Remove(_asteroidPool[i]);
                    _asteroidPool[i].SetActive(false);
                }
                else if (true) // check if collided
                {

                }
            }
        }
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <returns></returns>
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
    /// TODO: 
    /// </summary>
    /// <param name="simulatedAsteroid"></param>
    public void MakeAsteroidReal(SimulatedAsteroid simulatedAsteroid)
    {
        GameObject asteroid = GetPooledAsteroid();

        if (asteroid != null)
        {
            // Since GetPooledAsteroid will only give us inactive asteroids, it means that asteroid is already dead
            // But since we can't remove it from the dictionary from the astroid itself, it is best to do that check here, as well as in update.
            // To make sure everything is correct, we also set its simulated properties here.
            if (_link.ContainsKey(asteroid))
            {
                _link[asteroid].IsSimulated = true;
                _link[asteroid].IsRespawning = true;
                _link.Remove(asteroid);
            }

            asteroid.SetActive(true);
            Rigidbody2D roidRB = asteroid.GetComponent<Rigidbody2D>();
            asteroid.transform.position = new Vector3(simulatedAsteroid.Position.x, simulatedAsteroid.Position.y, 0);
            Vector2 velocity = simulatedAsteroid.Direction * simulatedAsteroid.Speed;
            roidRB.velocity = velocity;
            simulatedAsteroid.IsSimulated = false;
            _link.Add(asteroid, simulatedAsteroid);
        }
        else
        {
            // TODO: expand pool by 1
        }
    }

    /// <summary>
    /// TODO: 
    /// </summary>
    /// <param name="asteroid"></param>
    public void SimulateAsteroid(GameObject asteroid)
    {
        _link.Remove(asteroid);
    }
}
