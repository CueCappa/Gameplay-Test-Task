using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for controlling the player ship
/// </summary>
public class ShipManager : MonoBehaviour
{
    [SerializeField] private float shipSpeed;
    [SerializeField] private float shipRotationSpeed;

    private void Start()
    {
        // Start coroutine for automatically shooting every 0.5 seconds
        StartCoroutine(Shooting(0.5f));
    }

    private void Update()
    {
        // Direct check for input, no need for anything more advanced for a simple game like this
        // Check for moving forward
        if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            transform.position += transform.up * shipSpeed * Time.deltaTime;
        }

        // Check for turning left
        if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(Vector3.forward * shipRotationSpeed * Time.deltaTime);
        }

        // Check for turning right
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(Vector3.back * shipRotationSpeed * Time.deltaTime);
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
            Shoot();
        }
    }

    /// <summary>
    /// Shoots a bullet from the ship when called.
    /// </summary>
    private void Shoot()
    {
        // TODO: make pool of 6 bullets, since rate of fire is 0.5 seconds and bullet lifetime is 3, at 3 seconds first bullet deactivates and immediately shoots again
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Asteroid"))
        {
            Debug.Log("You died.");
        }
    }
}
