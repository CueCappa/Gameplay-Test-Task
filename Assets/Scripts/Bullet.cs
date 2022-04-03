using UnityEngine;

/// <summary>
/// Denotes a bullet and manages its state
/// </summary>
public class Bullet : MonoBehaviour
{
    [SerializeField] private int _lifetime;
    private float _timer;

    private GameManager _gm;

    private void Start()
    {
        _gm = FindObjectOfType<GameManager>();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lifetime)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            gameObject.SetActive(false);
            collision.gameObject.SetActive(false);
            _gm.UpdateScore();
        }
    }

    private void OnEnable()
    {
        _timer = 0;
    }
}
