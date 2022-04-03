using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the game state
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private Text _scoreText;
    [SerializeField] private Text _finalScoreText;
    [SerializeField] private GameObject _restartPanel;
    [SerializeField] private string _sceneName;

    // This is here in case "12. Make sure every asteroid speed and direction is persistent every game." means same starting state for every asteroid.
    [Header("Set 0 for random")]
    [SerializeField]
    private int _seed = 0;

    private int _score = 0;
    private bool _gamePaused = false;

    public bool GamePaused => _gamePaused;

    private void Awake()
    {
        if (_seed != 0)
        {
            Random.InitState(_seed);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// Since the score can only go up by 1, this updates the score and updates the text showing it.
    /// </summary>
    public void UpdateScore()
    {
        _score++;
        _scoreText.text = "Score: " + _score.ToString();
    }

    /// <summary>
    /// Pauses the simulation and game, opens game over panel.
    /// </summary>
    public void GameOver()
    {
        _gamePaused = true;
        Time.timeScale = 0;
        _finalScoreText.text = "Final score: " + _score.ToString() + "!";
        _restartPanel.SetActive(true);
    }

    /// <summary>
    /// Restarts the game.
    /// </summary>
    public void RestartGame()
    {
        _gamePaused = false;
        Time.timeScale = 1;
        SceneManager.LoadScene(_sceneName);
    }
}
