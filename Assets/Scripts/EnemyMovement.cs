using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public List<Vector3> patrolPoints;
    public GridGenerator gridGenerator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        patrolPoints = gridGenerator.GetWaypoints();
    }

    // Update is called once per frame
    void Update()
    {
        //Waits 2 seconds before moving to next waypoint
        if (patrolPoints.Count > 0)
        {
            float step = 2 * Time.deltaTime; // Speed of movement
            transform.position = Vector3.MoveTowards(transform.position, patrolPoints[0], step);
            if (Vector3.Distance(transform.position, patrolPoints[0]) < 0.001f)
            {
                patrolPoints.RemoveAt(0); // Move to next waypoint
            }
        }
    }
}
