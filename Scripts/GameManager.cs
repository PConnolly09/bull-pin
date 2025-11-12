using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Start, Playing, Victory, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Start;

    [Header("References")]
    public Camera mainCamera;
    [SerializeField] public GameObject bullPrefab;
   
    public Transform BullLauncherPosition { get; private set; }

    [Header("Placement Settings")]
    public LayerMask placementLayer;
    public GameObject currentBumperPrefab;
    public bool isPlacing = false;

    [Header("Resources")]
    public int playerCurrency = 100;
    public Dictionary<string, int> bumperCosts = new Dictionary<string, int>();
    public int goldPerEnemy = 10;

    public event Action<int> OnGoldChanged;
    public event Action<GameState> OnGameStateChanged;

    private int totalEnemies;
    private int enemiesRemaining;


    private BullController currentBull;
    private bool bullActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
                Instance = this;

        bumperCosts["Standard"] = 25;
        bumperCosts["Scatter"] = 50;
        bumperCosts["Explosive"] = 75;

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        UpdateGoldUI();
        SetState(GameState.Start);
    }

    private void Update()
    {
        if (!bullActive) isPlacing = true;
        if (isPlacing)
            HandlePlacement();

        if (CurrentState == GameState.Start && Input.GetKeyDown(KeyCode.Space))
        {
            SetState(GameState.Playing);
            SpawnBull();
        }
    }
    // Resource control
    public bool SpendGold(int amount)
    {
        if (playerCurrency < amount) return false;
        playerCurrency -= amount;
        UpdateGoldUI();
        return true;
    }

    public void SelectLaunchPad(LaunchPad pad)
    {
        BullLauncherPosition = pad.transform;
        Debug.Log($"Launch pad selected: {pad.name}");
    }

    public void AddGold(int amount)
    {
        playerCurrency += amount;
        UpdateGoldUI();
    }

    private void UpdateGoldUI()
    {
        OnGoldChanged?.Invoke(playerCurrency);
    }

    // Enemy management
    public void RegisterEnemy()
    {
        totalEnemies++;
        enemiesRemaining++;
    }

    public void EnemyDied()
    {
        enemiesRemaining--;
        AddGold(goldPerEnemy);

        if (enemiesRemaining <= 0)
            SetState(GameState.Victory);
    }

    // ---------- Bull Lifecycle ----------

    public void SpawnBull()
    {
        if (bullPrefab == null || BullLauncherPosition == null)
        {
            Debug.LogWarning("Bull prefab or spawn point missing!");
            return;
        }

        if (currentBull != null)
        {
            Destroy(currentBull.gameObject);
        }

        GameObject bullObj = Instantiate(bullPrefab, BullLauncherPosition.position, Quaternion.identity);
        currentBull = bullObj.GetComponent<BullController>();
        isPlacing = false;
        bullActive = true;
    }

    public void OnBullDespawned(BullController bull)
    {
        bullActive = false;
        StartCoroutine(SpawnNewBullAfterDelay());
    }

    private IEnumerator SpawnNewBullAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        SpawnBull();
    }


    // ---------- Game State Management ----------
    public void SetState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);

        switch (CurrentState)
        {
            case GameState.Start:
                Time.timeScale = 0;
                break;
            case GameState.Playing:
                Time.timeScale = 1;
                break;
            case GameState.GameOver:
                Time.timeScale = 0;
                break;
            case GameState.Victory:
                Time.timeScale = 0;
                break;
        }
    }

    public void RestartGame()
    {
        // Simplest version for now
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // ---------- Placement Handling ----------
    void HandlePlacement()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (!mainCamera || !mainCamera.isActiveAndEnabled)
            return;

        Vector3 mousePos = Input.mousePosition;

        // Make sure the cursor is on-screen
        if (mousePos.x < 0 || mousePos.y < 0 ||
            mousePos.x > Screen.width || mousePos.y > Screen.height)
            return;

        // Adjust the z-distance dynamically based on camera position
        float depthToPlane = Mathf.Abs(mainCamera.transform.position.z);
        mousePos.z = depthToPlane;

        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePos);
        worldPoint.z = 0f;

        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, placementLayer);
        if (hit.collider != null)
        {
            Vector3 placePos = hit.point;
            if (Input.GetMouseButtonDown(0))
            {
                if (CanAfford(currentBumperPrefab))
                    PlaceBumper(placePos);
                else
                    Debug.Log("Not enough resources!");
            }
        }

        if (Input.GetMouseButtonDown(1))
            CancelPlacement();
    }




    void PlaceBumper(Vector3 position)
    {
        position.z = 0f; // lock to 2D plane

        Instantiate(currentBumperPrefab, position, Quaternion.identity);
        string bumperName = currentBumperPrefab.name;

        if (bumperCosts.TryGetValue(bumperName, out int cost))
            playerCurrency -= cost;
        else
            Debug.LogWarning($"Bumper '{bumperName}' not found in cost dictionary!");

        isPlacing = false;
    }

    public void BeginPlacement(GameObject bumperPrefab)
    {
        currentBumperPrefab = bumperPrefab;
        isPlacing = true;
    }

    public void CancelPlacement()
    {
        currentBumperPrefab = null;
        isPlacing = false;
    }

    bool CanAfford(GameObject bumper)
    {
        Bumper bumperComponent = bumper.GetComponent<Bumper>();
        return playerCurrency >= bumperComponent.GetCost();
    }

}
