using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Denotes an asteroid and handles its behaviour, for now.
/// </summary>
public class AsteroidManager : MonoBehaviour
{
    // TODO: gotta be able to change the random constraints somewhere
    [SerializeField] private float asteroidSpeedMax;
    [SerializeField] private float asteroidSpeedMin;

    [SerializeField] private float asteroidSpeed;
    [SerializeField] private Vector2 asteroidDirection;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;

    private GameObject _player;
    private float _distanceToPlayer;

    private void Awake()
    {
        // randomize asteroid direction and speed on game start
        // TODO: this needs to be remembered between games apparently?
        asteroidSpeed = Random.Range(asteroidSpeedMin, asteroidSpeedMax);
        asteroidDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }

    private void Start()
    {
        rb.velocity = asteroidDirection * asteroidSpeed;
    }

    private void Update()
    {
        // move asteroid
        //rb.MovePosition(rb.position + asteroidDirection * asteroidSpeed * Time.deltaTime);
    }
}
