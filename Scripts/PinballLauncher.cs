using UnityEngine;

public class PinballLauncher : MonoBehaviour
{
    [SerializeField] private Transform pullHandle; // visual plunger
    [SerializeField] private float maxPullDistance = 1.5f;
    [SerializeField] private float launchForce = 1200f;
    [SerializeField] private KeyCode launchKey = KeyCode.Space;
    [SerializeField] private BullController bullPrefab;
    [SerializeField] private Transform spawnPoint;

    private BullController currentBull;
    private float currentPull;
    private bool isPulling;

    void Start()
    {
        SpawnBull();
    }

    void Update()
    {
        if (Input.GetKeyDown(launchKey)) StartPull();
        if (Input.GetKey(launchKey)) ChargePull();
        if (Input.GetKeyUp(launchKey)) Launch();
    }

    void StartPull()
    {
        isPulling = true;
        currentPull = 0f;
    }

    void ChargePull()
    {
        if (!isPulling) return;

        currentPull += Time.deltaTime;
        currentPull = Mathf.Clamp(currentPull, 0f, maxPullDistance);

        // move the plunger visually
        pullHandle.localPosition = new Vector3(0, -currentPull, 0);
    }

    void Launch()
    {
        if (currentBull == null) return;

        isPulling = false;

        Rigidbody2D rb = currentBull.GetComponent<Rigidbody2D>();
        rb.isKinematic = false;

        float force = (currentPull / maxPullDistance) * launchForce;
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        pullHandle.localPosition = Vector3.zero;

        // Notify the GameManager that the round’s begun
        GameManager.Instance.BullLaunched();

        currentBull = null;
    }

    void SpawnBull()
    {
        currentBull = Instantiate(bullPrefab, spawnPoint.position, Quaternion.identity);
        currentBull.GetComponent<Rigidbody2D>().isKinematic = true;
    }
}
