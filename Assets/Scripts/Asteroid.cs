using UnityEngine;

/// <summary>
/// Denotes a real asteroid and manages the collision.
/// </summary>
public class Asteroid : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            // We set both this asteroid and the one it collided with inactive.
            gameObject.SetActive(false);
            collision.gameObject.SetActive(false);
        }
    }
}
