using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using TMPro;

[System.Serializable]
public struct WaveConfig
{
    public int normalEnemies;
    public int bigEnemies;
    public int smallEnemies;
}

public class SpawnManager : MonoBehaviour
{
    // Singleton instance so enemies can easily notify the SpawnManager when they die
    public static SpawnManager Instance { get; private set; }

    private const int NORMAL_ENEMY = 0;
    private const int BIG_ENEMY = 1;
    private const int SMALL_ENEMY = 2;

    [Header("Prefabs & Arena Boundaries")]
    public GameObject powerUpPrefab;
    public GameObject[] enemyPrefabs;
    public float spawnRangeX = 24f;
    public float spawnRangeZ = 24f;

    [Header("Gate Spawn Points")]
    public Transform[] spawnPoints;
    public float spawnScatterRadius = 1.2f; // Slight offset so enemies at the same gate don't overlap
    private int lastSpawnIndex = -1;

    [Header("Wave Configuration")]
    public List<WaveConfig> waves;
    public int currentWave = 1;
    private int enemyCount = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnWave(currentWave);
        StartCoroutine(SpawnPowerUpRoutine());
    }

    public void OnEnemyKilled(int pointsPerKill)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        enemyCount--;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(pointsPerKill);
            GameManager.Instance.UpdateWaveUI(currentWave, enemyCount);
        }

        if (enemyCount <= 0)
        {
            currentWave++;
            SpawnWave(currentWave);
        }
    }

    void SpawnWave(int waveNumber)
    {
        // Convert 1-based waveNumber to 0-based list index (Wave 1 = Index 0)
        int waveIndex = waveNumber - 1;

        if (waveIndex < waves.Count)
        {
            // Spawn the exact counts configured in the Inspector for this wave
            WaveConfig wave = waves[waveIndex];
            SpawnEnemyAtGate(NORMAL_ENEMY, wave.normalEnemies);
            SpawnEnemyAtGate(BIG_ENEMY, wave.bigEnemies);
            SpawnEnemyAtGate(SMALL_ENEMY, wave.smallEnemies);
        }
        else
        {
            // Endless fallback once the player beats all configured waves
            SpawnEnemyAtGate(NORMAL_ENEMY, waveNumber * 2);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateWaveUI(currentWave, enemyCount);
        }
    }

    private IEnumerator SpawnPowerUpRoutine()
    {
        while (GameManager.Instance == null || !GameManager.Instance.isGameOver)
        {
            // 1. Wait 10 to 20 seconds before spawning
            float waitTime = Random.Range(10f, 20f);
            yield return new WaitForSeconds(waitTime);

            if  (GameManager.Instance != null && GameManager.Instance.isGameOver)
            {
                yield break; // Stop spawning if the game is over
            }

            // 2. Spawn the power-up
            Instantiate(powerUpPrefab, GenerateSpawnPosition(), powerUpPrefab.transform.rotation);
        }
    }

    private Vector3 GenerateSpawnPosition()
    {
        float spawnPosX = Random.Range(-spawnRangeX, spawnRangeX);
        float spawnPosZ = Random.Range(-spawnRangeZ, spawnRangeZ);
        return new Vector3(spawnPosX, 1f, spawnPosZ);
    }

    void SpawnEnemyAtGate(int prefabIndex, int amount)
    {
        if (amount <= 0 || prefabIndex < 0 || prefabIndex >= enemyPrefabs.Length)
        {
            return; // Nothing to spawn or invalid prefab index
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No SpawnPoints assigned to SpawnManager!");
            return;
        }

        GameObject prefab = enemyPrefabs[prefabIndex];

        for (int i = 0; i < amount; i++)
        {
            // 1. Pick a gate for EACH enemy (distributes the wave across all 4 gates)
            int index = Random.Range(0, spawnPoints.Length);
            if (spawnPoints.Length > 1 && index == lastSpawnIndex)
            {
                index = (index + 1) % spawnPoints.Length;
            }
            lastSpawnIndex = index;

            Transform chosenGate = spawnPoints[index];

            // 2. Add a small horizontal offset around the gate so multiple enemies don't clip into each other
            Vector2 randomCircle = Random.insideUnitCircle * spawnScatterRadius;
            Vector3 spawnPos = chosenGate.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // 3. Spawn facing the gate's inward +Z direction
            Instantiate(prefab, spawnPos, chosenGate.rotation);
            enemyCount++;
        }
    }
}