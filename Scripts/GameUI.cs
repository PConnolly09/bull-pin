using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI enemyText;
    public TextMeshProUGUI launchText;
    public TextMeshProUGUI goldText;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    public void UpdateEnemyCount(int remaining, int total)
    {
        enemyText.text = $"Enemies: {remaining}/{total}";
    }

    public void UpdateLaunchCount(int remaining)
    {
        launchText.text = $"Launches: {remaining}";
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }

    public void ShowVictory()
    {
        victoryPanel.SetActive(true);
    }

    private void OnEnable()
    {
        GameManager.Instance.OnGoldChanged += UpdateGoldText;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnGoldChanged -= UpdateGoldText;
    }

    void UpdateGoldText(int gold)
    {
        goldText.text = $"Gold: {gold}";
    }

}
