using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GridGenerator gridGenerator;
    private Vector3 startPositionInWorld;
    public GameObject enemyPrefab;
    private int enemiesPerWave = 5;


    void Start()
    {
        startPositionInWorld = gridGenerator.GetStartWorldPos();
        StartCoroutine(SpawnEnemyRoutine());
    }

    private IEnumerator SpawnEnemyRoutine()
    {
        for(int i = 0; i < enemiesPerWave; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(Random.Range(1f, 3f));
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
