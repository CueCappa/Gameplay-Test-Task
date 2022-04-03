using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player ship and gun
/// </summary>
public class ShipManager : MonoBehaviour
{
    [SerializeField] private float _shipSpeed;
    [SerializeField] private float _shipRotationSpeed;

    [SerializeField] private GameManager _gm;

    [Header("Bullet settings")]
    [SerializeField] private int _amountOfBullets;
    [SerializeField] private float _bulletVelocity;
    [SerializeField] private Transform _bulletParent;
    [SerializeField] private Transform _bulletSpawn;
    [SerializeField] private GameObject _objectToPool;
    [SerializeField] private List<GameObject> _bulletPool = new List<GameObject>();

    private const float _refireDelay = 0.5f;

    private void Start()
    {
        GameObject bullet;
        for (int i = 0; i < _amountOfBullets; i++)
        {
            bullet = Instantiate(_objectToPool, _bulletParent);
            bullet.SetActive(false);
            _bulletPool.Add(bullet);
        }

        if (_gm == null)
        {
            _gm = FindObjectOfType<GameManager>();
        }

        // Start coroutine for automatically shooting every 0.5 seconds
        StartCoroutine(Shooting(_refireDelay));
    }

    private void Update()
    {
        // Direct check for input, no need for anything more advanced for simple controls like these
        // Check for moving forward
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            transform.position += transform.up * _shipSpeed * Time.deltaTime;
        }

        // Check for turning left
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(Vector3.forward * _shipRotationSpeed * Time.deltaTime);
        }

        // Check for turning right
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(Vector3.back * _shipRotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Asteroid"))
        {
            gameObject.SetActive(false);
            _gm.GameOver();
        }
    }

    /// <summary>
    /// Calls the shoot function every time refireDelay in seconds has passed.
    /// </summary>
    /// <param name="refireDelay"> The delay between shots. </param>
    /// <returns></returns>
    private IEnumerator Shooting(float refireDelay)
    {
        while (true)
        {
            yield return new WaitForSeconds(refireDelay);

            if (GetPooledBullet() != null)
            {
                Shoot(GetPooledBullet());
            }
        }
    }

    /// <summary>
    /// Get a bullet from the pool that is currently not in use. 
    /// </summary>
    /// <returns> An inactive bullet from the pool, or null if there is no one. </returns>
    private GameObject GetPooledBullet()
    {
        for (int i = 0; i < _amountOfBullets; i++)
        {
            if (!_bulletPool[i].activeInHierarchy)
            {
                return _bulletPool[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Shoots a bullet from the ship when called.
    /// </summary>
    /// <param name="bullet"> The bullet to shoot. </param>
    private void Shoot(GameObject bullet)
    {
        Rigidbody2D bulletRB = bullet.GetComponent<Rigidbody2D>();
        //Vector2 bulletVelocity = gameObject.transform.up * _bulletVelocity;

        bullet.SetActive(true);
        bullet.transform.position = _bulletSpawn.position;
        bullet.transform.rotation = gameObject.transform.rotation;
        bulletRB.velocity = gameObject.transform.up * _bulletVelocity;
    }
}
