using UnityEngine;

public class BattlefieldSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject bumperPrefab;

    [Header("Settings")]
    public Vector2 fieldSize = new Vector2(30, 20);
    public int enemyCount = 10;
    public int bumperCount = 6;
    public int seed = 0; // leave 0 for random

    void Start()
    {
        if (seed == 0)
            seed = Random.Range(1, int.MaxValue);

        Random.InitState(seed);
        Debug.Log($"Using seed: {seed}");

        SpawnBoundaries();
        SpawnEnemies();
        //SpawnBumpers();
    }

    void SpawnBoundaries()
    {
        Vector2 half = fieldSize / 2f;
        GameObject wallHolder = new GameObject("Walls");

        CreateWall(new Vector2(0, half.y + 0.5f), fieldSize.x, 1f, wallHolder.transform);
        CreateWall(new Vector2(0, -half.y - 0.5f), fieldSize.x, 1f, wallHolder.transform);
        CreateWall(new Vector2(half.x + 0.5f, 0), 1f, fieldSize.y, wallHolder.transform);
        CreateWall(new Vector2(-half.x - 0.5f, 0), 1f, fieldSize.y, wallHolder.transform);
    }

    void CreateWall(Vector2 pos, float width, float height, Transform parent)
    {
        GameObject wall = new GameObject("Wall");
        wall.transform.parent = parent;
        wall.transform.position = pos;
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);
    }

    void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 pos = new Vector2(
                Random.Range(-fieldSize.x / 2f, fieldSize.x / 2f),
                Random.Range(-fieldSize.y / 2f, fieldSize.y / 2f)
            );
            Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
    }

    //void SpawnBumpers()
    //{
    //    for (int i = 0; i < bumperCount; i++)
    //    {
    //        Vector2 pos = new Vector2(
    //            Random.Range(-fieldSize.x / 2f, fieldSize.x / 2f),
    //            Random.Range(-fieldSize.y / 2f, fieldSize.y / 2f)
    //        );
    //        Instantiate(bumperPrefab, pos, Quaternion.identity);
    //    }
    //}
}
