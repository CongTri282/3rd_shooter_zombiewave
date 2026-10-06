using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameOver = false;
    public int currentScore = 0;
    public int currentWave = 1;

    [Header("HUD UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI enemiesRemainingText;

    [Header("Game Over UI References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isGameOver = false;
        currentScore = 0;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        UpdateScoreUI();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddScore(int points)
    {
        if (isGameOver) return;

        currentScore += points;
        UpdateScoreUI();
    }

    public void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + currentScore;
        }
    }

    public void UpdateWaveUI(int waveNumber, int enemiesRemaining)
    {
        currentWave = waveNumber;
        if (waveText != null)
        {
            waveText.text = "WAVE: " + currentWave;
        }
        if (enemiesRemainingText != null)
        {
            enemiesRemainingText.text = $"Hostiles: {Mathf.Max(0, enemiesRemaining)}";
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (finalScoreText != null)
        {
            finalScoreText.text = $"FINAL SCORE: {currentScore}\nSURVIVED TO WAVE {currentWave}";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
