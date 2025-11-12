using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instantiates colliders and placeholder prefabs from the BattlefieldData produced by BattlefieldGenerator.
/// Attach to an empty GameObject in scene. Assign prefabs and call GenerateAndInstantiate() or enable GenerateOnStart.
/// </summary>
public class BattlefieldInstantiator : MonoBehaviour
{
    [Header("Generator Settings")]
    public MapSize preset = MapSize.Medium;
    public int seed = 0;
    public bool generateOnStart = true;
    public GenerationOptions options;
    public CameraScaler cameraScaler;

    [Header("Prefabs / Placeholders (optional)")]
    public GameObject wallPrefab;            // should have BoxCollider2D (or will create collider)
    public GameObject launcherNodePrefab;    // small marker or empty transform
    public GameObject enemySpawnPrefab;      // marker for spawns
    public GameObject rockPrefab;            // optional visual for blocked rock

    [Header("Layers / Parenting")]
    public Transform worldParent;
    public LayerMask wallLayer = 0;

    private BattlefieldData currentData;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private BattlefieldData previewData;

    void Start()
    {
        if (options == null) options = GenerationOptions.Default();
        if (generateOnStart) GenerateAndInstantiate(seed);
    }
    private void OnValidate()
    {
        if (!Application.isPlaying)
            previewData = BattlefieldGenerator.Generate(seed, preset, options);
    }
    public void ClearInstantiated()
    {
        foreach (var o in spawnedObjects) if (o != null) DestroyImmediate(o);
        spawnedObjects.Clear();
    }

    public void GenerateAndInstantiate(int seedValue)
    {
        ClearInstantiated();
        if (seedValue == 0) seedValue = UnityEngine.Random.Range(1, int.MaxValue);
        seed = seedValue;

        currentData = BattlefieldGenerator.Generate(seed, preset, options);
        InstantiateFromData(currentData);
        Debug.Log($"Generated battlefield (seed {seed}) openCells={currentData.openCellCount} spawns={currentData.spawnCount}");

        BattlefieldCamera cam = Object.FindAnyObjectByType<BattlefieldCamera>();
        cam.battlefieldSize = BattlefieldGenerator.GetPresetSize(preset);
        cam.battlefieldAnchor = BattlefieldAnchor.Instance.transform;
        cameraScaler.AdjustCameraDistance(currentData);


    }

    void InstantiateFromData(BattlefieldData data)
    {
        // Create parent
        if (worldParent == null)
        {
            GameObject container = new GameObject("Battlefield");
            worldParent = container.transform;
        }

        // Instantiate walls
        foreach (var seg in data.wallSegments)
        {
            GameObject wallObj = null;
            if (wallPrefab != null)
            {
                wallObj = Instantiate(wallPrefab, seg.center, Quaternion.identity, worldParent);
                wallObj.transform.localScale = new Vector3(seg.size.x, seg.size.y, 1f);
            }
            else
            {
                wallObj = new GameObject("WallSegment");
                wallObj.transform.SetParent(worldParent);
                wallObj.transform.position = seg.center;
                BoxCollider2D col = wallObj.AddComponent<BoxCollider2D>();
                col.size = seg.size;
            }

            // Apply layer if provided
            if (wallLayer != 0)
                wallObj.layer = Mathf.RoundToInt(Mathf.Log(wallLayer.value, 2));
            spawnedObjects.Add(wallObj);
        }

        // Instantiate launcher nodes
        GameObject launcherParent = new GameObject("LauncherNodes");
        launcherParent.transform.SetParent(worldParent);
        foreach (var node in data.launcherNodes)
        {
            GameObject n = null;
            if (launcherNodePrefab != null) n = Instantiate(launcherNodePrefab, node.position, Quaternion.identity, launcherParent.transform);
            else
            {
                n = new GameObject("LauncherNode");
                n.transform.SetParent(launcherParent.transform);
                n.transform.position = node.position;
            }
            // store inward normal as a small visible arrow if prefab isn't present
            spawnedObjects.Add(n);
        }

        // Instantiate enemy spawn markers
        GameObject spawnParent = new GameObject("EnemySpawns");
        spawnParent.transform.SetParent(worldParent);
        foreach (var s in data.enemySpawns)
        {
            GameObject m = null;
            if (enemySpawnPrefab != null) m = Instantiate(enemySpawnPrefab, s, Quaternion.identity, spawnParent.transform);
            else
            {
                m = new GameObject("EnemySpawn");
                m.transform.SetParent(spawnParent.transform);
                m.transform.position = s;
                // add a small gizmo-like sprite if desired
            }
            spawnedObjects.Add(m);
        }

        // Optional: instantiate rocks for blocked cells
        if (rockPrefab != null)
        {
            GameObject rocksParent = new GameObject("Rocks");
            rocksParent.transform.SetParent(worldParent);
            int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (data.cells[x, y].terrain == TerrainType.BlockedRock)
                    {
                        Vector2 pos = data.cells[x, y].worldPos;
                        GameObject r = Instantiate(rockPrefab, pos, Quaternion.identity, rocksParent.transform);
                        spawnedObjects.Add(r);
                    }
                }
        }
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return; // disable gizmos when not running to avoid editor projection errors

        var data = Application.isPlaying ? currentData : previewData;
        if (data == null || data.cells == null) return;

        if (currentData == null || currentData.cells == null)
            return;

        int w = currentData.cells.GetLength(0);
        int h = currentData.cells.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                var cell = currentData.cells[x, y];
                switch (cell.terrain)
                {
                    case TerrainType.Open:
                    case TerrainType.OpenPocket:
                        Gizmos.color = new Color(0, 1, 0, 0.15f);
                        break;
                    case TerrainType.Funnel:
                        Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
                        break;
                    case TerrainType.BlockedRock:
                        Gizmos.color = new Color(1, 0, 0, 0.25f);
                        break;
                    case TerrainType.BlockedWallBuffer:
                        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.25f);
                        break;
                    default:
                        continue;
                }

                Gizmos.DrawCube(cell.worldPos, Vector3.one * currentData.cellSize * 0.9f);
            }
        }

        if (currentData.launcherNodes != null)
        {
            Gizmos.color = Color.magenta;
            foreach (var ln in currentData.launcherNodes)
            {
                Gizmos.DrawSphere(ln.position, currentData.cellSize * 0.25f);
                Gizmos.DrawLine(ln.position, ln.position + ln.inwardNormal * currentData.cellSize * 1.2f);
            }
        }

        if (currentData.wallSegments != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var s in currentData.wallSegments)
                Gizmos.DrawWireCube(s.center, new Vector3(s.size.x, s.size.y, 1f));
        }
    }

}
