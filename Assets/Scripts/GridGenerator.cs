using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class PathSegment
{
    public Vector2Int direction;  // X,Z: right=(1,0), forward=(0,1), left=(-1,0), back=(0,-1)
    public int steps;             // How many cells to move
}

public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public GameObject enemySpawnerPrefab;
    public GameObject defendPointPrefab;
    public int width = 20;            // X direction
    public int depth = 15;            // Z direction
    public float cellSize = 1f;       // Distance between cell centers
    public Vector2Int startPos = new Vector2Int(1, 7);  // Grid (X,Z); (0,0)=bottom-left when viewed from +Y

    [Header("Path Mode")]
    public bool useRandomPath = true; // Enable random generation
    public int randomSeed = 0;        // 0 = time-based; fixed for reproducible

    [Header("Random Path Settings")]
    [Range(30, 200)] public int minPathLength = 50;
    [Range(50, 300)] public int maxPathLength = 300;
    [Range(10, 200)] public int maxRetries = 200;
    public Vector2Int endZoneSize = new Vector2Int(3, 3); // Must end in top-right zone (width-endZoneSize.x, depth-endZoneSize.y)

    [Header("Manual Path (Ignored if Random Enabled)")]
    public PathSegment[] pathSegments = new PathSegment[0];

    [Header("Generation")]
    public bool clearOnGenerate = true;
    public bool generateOnStart = true;

    [Header("Runtime Access")]
    public Vector2Int endPos;               // Auto-set after generation (for enemy goal)
    public List<Vector2Int> generatedPath;  // Full path cells (for viz/AI)

    private bool[,] walkable;   // true = path (no wall tile) todo: replace with floor tile

    void Start()
    {
        if (generateOnStart) GenerateGrid();
    }

    [ContextMenu("Load Example Path")]
    public void LoadExamplePath()
    {
        // PERFECT example for 20x15 grid: Starts (1,7) → Ends (17,13)
        pathSegments = new PathSegment[]
        {
            new PathSegment { direction = new Vector2Int(1, 0), steps = 10 },   // Right +X to (11,7)
            new PathSegment { direction = new Vector2Int(0, 1), steps = 2 },    // Forward +Z to (11,9)
            new PathSegment { direction = new Vector2Int(-1, 0), steps = 6 },   // Left -X to (5,9)
            new PathSegment { direction = new Vector2Int(0, 1), steps = 3 },    // Forward +Z to (5,12)
            new PathSegment { direction = new Vector2Int(1, 0), steps = 8 },    // Right +X to (13,12)
            new PathSegment { direction = new Vector2Int(0, 1), steps = 1 },    // Forward +Z to (13,13)
            new PathSegment { direction = new Vector2Int(1, 0), steps = 4 }     // Right +X to (17,13) = Exit!
        };

        useRandomPath = false; // Switch to manual
        Debug.Log("✅ Example Manual Path Loaded! Hit 'Generate Grid' next.");
    }

    [ContextMenu("Generate Random Path")]
    public void GenerateRandomPath()
    {
        useRandomPath = true;
        pathSegments = new PathSegment[0]; // Clear manual
        GenerateGrid(); // Auto-generates random
    }

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Assign tilePrefab!");
            return;
        }

        if (clearOnGenerate) ClearGrid();

        // Auto-random if enabled + no manual path
        if (useRandomPath && (pathSegments == null || pathSegments.Length == 0))
        {
            GenerateRandomPathInternal();
        }
        else if (!useRandomPath && (pathSegments == null || pathSegments.Length == 0))
        {
            Debug.LogWarning("⚠️ No path! Loading example manual...");
            LoadExamplePath();
        }

        // Use manual path
        if (!useRandomPath)
        {
            GeneratePathFromSegments();
        }

        // Mark walkable & build grid
        walkable = new bool[width, depth];
        HashSet<Vector2Int> uniquePath = new HashSet<Vector2Int>(generatedPath);
        foreach (var cell in uniquePath)
            walkable[cell.x, cell.y] = true;

        // Instantiate WALL tiles everywhere EXCEPT path (XZ plane, Y=0)
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (!walkable[x, z])
                {
                    Vector3 worldPos = new Vector3(x * cellSize, 0f, z * cellSize);
                    Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                }
            }
        }

        // Instantiate Enemy Spawner 
        if (enemySpawnerPrefab != null)
        {
            Vector3 spawnerPos = GridToWorldPos(startPos);
            Instantiate(enemySpawnerPrefab, spawnerPos, Quaternion.identity, transform);
        }

        //Instantiate Defend Point
        if (defendPointPrefab != null)
        {
            Vector3 defendPointPos = GridToWorldPos(endPos);
            Instantiate(defendPointPrefab, defendPointPos, Quaternion.identity, transform);
        }

        Debug.Log($"✅ 3D Grid generated! Mode: {(useRandomPath ? "Random" : "Manual")}. {transform.childCount} walls. Path: {uniquePath.Count} cells. Start: {GetStartWorldPos()}, End: {GetEndWorldPos()}");
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != transform)
            {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }
        Debug.Log("🧹 Grid cleared!");
    }

    private void GenerateRandomPathInternal()
    {
        if (randomSeed == 0) Random.InitState((int)System.DateTime.Now.Ticks);
        else Random.InitState(randomSeed);

        int retries = 0;
        bool success = false;

        while (retries < maxRetries && !success)
        {
            generatedPath = new List<Vector2Int> { startPos };
            HashSet<Vector2Int> visited = new HashSet<Vector2Int> { startPos };
            Vector2Int current = startPos;
            int pathLength = 1;

            Vector2Int[] directions = {
                new Vector2Int(1, 0),   // Right
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(0, 1),   // Forward
                new Vector2Int(0, -1)   // Back
            };

            Vector2Int endZoneStart = new Vector2Int(width - endZoneSize.x, depth - endZoneSize.y);

            while (pathLength < maxPathLength)
            {
                List<Vector2Int> validDirections = new List<Vector2Int>();
                Vector2Int heuristicDir = Vector2Int.zero;
                float bestDist = float.MaxValue;

                foreach (var dir in directions)
                {
                    Vector2Int next = current + dir;
                    if (IsValidMove(next, visited))
                    {
                        validDirections.Add(dir);
                        float distToEnd = Mathf.Sqrt(Mathf.Pow(next.x - endZoneStart.x, 2) + Mathf.Pow(next.y - endZoneStart.y, 2));
                        if (distToEnd < bestDist)
                        {
                            bestDist = distToEnd;
                            heuristicDir = dir;
                        }
                    }
                }

                if (validDirections.Count == 0)
                {
                    break; // Stuck
                }

                // 30% heuristic, 70% random (from valid only)
                Vector2Int chosenDir = (Random.value < 0.3f && heuristicDir != Vector2Int.zero) ? heuristicDir : validDirections[Random.Range(0, validDirections.Count)];

                Vector2Int nextPos = current + chosenDir;
                current = nextPos;
                generatedPath.Add(current);
                visited.Add(current);
                pathLength++;

                // Success: Reached end zone AND long enough
                if (current.x >= width - endZoneSize.x && current.y >= depth - endZoneSize.y && pathLength >= minPathLength)
                {
                    success = true;
                    break;
                }
            }

            // Retry if too short or not in zone
            if (generatedPath.Count < minPathLength || !(current.x >= width - endZoneSize.x && current.y >= depth - endZoneSize.y))
            {
                retries++;
                success = false;
            }
        }

        if (!success)
        {
            Debug.LogWarning($"⚠️ Random path failed after {maxRetries} retries. Falling back to example manual path.");
            LoadExamplePath();
            GeneratePathFromSegments();
        }
        else
        {
            endPos = generatedPath[generatedPath.Count - 1];
            Debug.Log($"✅ Random path success! Length: {generatedPath.Count} (seed: {randomSeed})");
        }
    }

    private bool IsValidMove(Vector2Int pos, HashSet<Vector2Int> visited)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < depth && !visited.Contains(pos);
    }

    private void GeneratePathFromSegments()
    {
        generatedPath = new List<Vector2Int>();
        Vector2Int current = startPos;
        generatedPath.Add(current);

        foreach (var segment in pathSegments)
        {
            for (int i = 0; i < segment.steps; i++)
            {
                current += segment.direction;
                if (current.x < 0 || current.x >= width || current.y < 0 || current.y >= depth)
                {
                    Debug.LogError($"Manual path out of bounds at {current}!");
                    return;
                }
                generatedPath.Add(current);
            }
        }
        endPos = current;
    }

    // Grid <-> World conversions (Y=0 plane)
    public Vector3 GridToWorldPos(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * cellSize, 0f, gridPos.y * cellSize);
    }

    public List<Vector3> GetWaypoints()
    {
        return generatedPath.Select(cell => GridToWorldPos(cell)).ToList();
    }

    public Vector2Int WorldToGridPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.z / cellSize)
        );
    }

    public Vector3 GetStartWorldPos() => GridToWorldPos(startPos);
    public Vector3 GetEndWorldPos() => GridToWorldPos(endPos);

    // For enemy AI: Check if cell is walkable (no wall)
    public bool IsWalkable(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < width && gridPos.y >= 0 && gridPos.y < depth && walkable[gridPos.x, gridPos.y];
    }

    public bool IsWalkable(Vector3 worldPos)
    {
        return IsWalkable(WorldToGridPos(worldPos));
    }

    // Bonus: Visualize path in Scene view (Gizmos)
    private void OnDrawGizmos()
    {
        if (generatedPath != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < generatedPath.Count - 1; i++)
            {
                Gizmos.DrawLine(GridToWorldPos(generatedPath[i]), GridToWorldPos(generatedPath[i + 1]));
            }
        }
    }
}