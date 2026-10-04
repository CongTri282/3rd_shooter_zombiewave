using System.Collections.Generic;
using UnityEngine;
using System.Collections;

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

    public void OnEnemyKilled()
    {
        enemyCount--;

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
            SpawnEnemy(NORMAL_ENEMY, wave.normalEnemies);
            SpawnEnemy(BIG_ENEMY, wave.bigEnemies);
            SpawnEnemy(SMALL_ENEMY, wave.smallEnemies);
        }
        else
        {
            // Endless fallback once the player beats all configured waves
            SpawnEnemy(NORMAL_ENEMY, waveNumber * 2);
        }
    }

    void SpawnEnemy(int prefabIndex, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject prefab = enemyPrefabs[prefabIndex];
            Instantiate(prefab, GenerateSpawnPosition(), prefab.transform.rotation);
            enemyCount++;
        }
    }

    private IEnumerator SpawnPowerUpRoutine()
    {
        while (true)
        {
            // 1. Wait 10 to 20 seconds before spawning
            float waitTime = Random.Range(10f, 20f);
            yield return new WaitForSeconds(waitTime);

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
}