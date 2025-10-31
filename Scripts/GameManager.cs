using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Start, Playing, Victory, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Start;

    [Header("Placement Settings")]
    public Camera mainCamera;
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

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
                Instance = this;

        bumperCosts["Standard"] = 25;
        bumperCosts["Splitter"] = 50;
        bumperCosts["Explosive"] = 75;

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        UpdateGoldUI();
    }

    private void Update()
    {
        if (isPlacing)
            HandlePlacement();
    }
    // Resource control
    public bool SpendGold(int amount)
    {
        if (playerCurrency < amount) return false;
        playerCurrency -= amount;
        UpdateGoldUI();
        return true;
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

    public void OnBullDestroyed()
    {
            SetState(GameState.GameOver);
    }

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

    void HandlePlacement()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayer))
        {
            Vector3 placePos = hit.point;

            // Optional: show a ghost placement preview here

            if (Input.GetMouseButtonDown(0)) // Left click to place
            {
                if (CanAfford(currentBumperPrefab))
                {
                    PlaceBumper(placePos);
                }
                else
                {
                    Debug.Log("Not enough resources!");
                }
            }
        }

        if (Input.GetMouseButtonDown(1)) // Right click to cancel
        {
            CancelPlacement();
        }
    }

    void PlaceBumper(Vector3 position)
    {
        Instantiate(currentBumperPrefab, position, Quaternion.identity);
        string bumperName = currentBumperPrefab.name;
        playerCurrency -= bumperCosts[bumperName];
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
        string bumperName = bumper.name;
        return playerCurrency >= bumperCosts[bumperName];
    }

}
