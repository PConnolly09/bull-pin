using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 5;
    public Vector2 areaSize = new Vector2(10, 5);

    void Awake()
    {
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 pos = new Vector2(
                transform.position.x + Random.Range(-areaSize.x / 2, areaSize.x / 2),
                transform.position.y + Random.Range(-areaSize.y / 2, areaSize.y / 2)
            );
            Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}
