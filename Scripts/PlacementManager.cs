using System;
using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    [Header("Placement Settings")]
    public Camera mainCamera;
    public GameObject[] bumperPrefabs; // Assign your different bumpers
    public float minPlacementDistance = 1.5f;

    [Header("Bumper Costs")]
    public int[] bumperCosts;

    private GameObject currentPreview;
    private GameObject selectedPrefab;
    private bool isPlacing = false;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (!isPlacing) return;

        // Follow mouse
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        if (currentPreview != null)
            currentPreview.transform.position = mousePos;

        // Left click to place
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBumper(mousePos);
        }

        // Right click to cancel
        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }

    public void BeginPlacement(int bumperIndex)
    {
        if (bumperIndex < 0 || bumperIndex >= bumperPrefabs.Length) return;

        selectedPrefab = bumperPrefabs[bumperIndex];
        isPlacing = true;

        // Create a transparent preview
        currentPreview = Instantiate(selectedPrefab);
        SetPreviewVisual(currentPreview, true);
    }

    private void TryPlaceBumper(Vector2 position)
    {
        int cost = bumperCosts[Array.IndexOf(bumperPrefabs, selectedPrefab)];

        if (!GameManager.Instance.SpendGold(cost))
        {
            Debug.Log("Not enough gold!");
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, minPlacementDistance);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Bumper"))
            {
                Debug.Log("Too close to another bumper!");
                GameManager.Instance.AddGold(cost); // refund
                return;
            }
        }

        Instantiate(selectedPrefab, position, Quaternion.identity);
        Debug.Log("Placed " + selectedPrefab.name);
        CancelPlacement();
    }



    private void CancelPlacement()
    {
        if (currentPreview != null)
            Destroy(currentPreview);

        selectedPrefab = null;
        currentPreview = null;
        isPlacing = false;
    }

    private void SetPreviewVisual(GameObject obj, bool isPreview)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = isPreview ? 0.5f : 1f;
            sr.color = c;
        }

        // Disable bumpers’ colliders while previewing
        Collider2D col = obj.GetComponent<Collider2D>();
        if (col != null) col.enabled = !isPreview;
    }
}
