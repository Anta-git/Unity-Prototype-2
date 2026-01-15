using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public List<Vector3> patrolPoints;
    private float cellSize;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GridGenerator gridGenerator = FindFirstObjectByType<GridGenerator>();
        patrolPoints = gridGenerator.GetWaypoints();
        cellSize = gridGenerator.getCellSize();
    }

    // Update is called once per frame
    void Update()
    {
        if (patrolPoints.Count > 0)
        {
            transform.position = Vector3.MoveTowards(transform.position, (new Vector3(patrolPoints[0].x * cellSize, 0f, patrolPoints[0].z * cellSize)), Time.deltaTime * 2);
            if (Vector3.Distance(transform.position, patrolPoints[0]) < 0.1f)
            {
                patrolPoints.RemoveAt(0);
            }
        }
    }
}
