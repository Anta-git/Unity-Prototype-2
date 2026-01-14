using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GridGenerator gridGenerator;
    private Vector3 startPositionInWorld;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPositionInWorld = gridGenerator.GetWorldPositionFromGridCoordinates(0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
