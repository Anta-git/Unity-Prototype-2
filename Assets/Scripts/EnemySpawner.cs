using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GridGenerator gridGenerator;
    private Vector3 startPositionInWorld;
    public GameObject enemyPrefab;
    private int enemyCount = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPositionInWorld = gridGenerator.GetStartWorldPos();
    }

    // Update is called once per frame
    void Update()
    {
        Coroutine spawnCoroutine = StartCoroutine(SpawnEnemyRoutine());
    }
    private System.Collections.IEnumerator SpawnEnemyRoutine()
    {
        while (enemyCount < 5)
        {
            SpawnEnemy();
            enemyCount++;
            yield return new WaitForSeconds(5f); // Spawn an enemy every 5 seconds
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab != null)
        {
            Vector3 aboveWalls = (startPositionInWorld + new Vector3(0, 1, 0));
            Instantiate(enemyPrefab, aboveWalls, Quaternion.identity);
        }
        else
        {
            Debug.LogError("EnemyPrefab not found in Resources folder!");
        }
    }
}
