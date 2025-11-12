using UnityEngine;
using System.Linq;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private string bullTag = "Bull"; // customize as needed
    [SerializeField] private bool showInBuild = false;

    private GUIStyle style;
    private float deltaTime;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        style = new GUIStyle
        {
            fontSize = 14,
            normal = { textColor = Color.green }
        };
    }

    void Update()
    {
        if (!showInBuild && !Application.isEditor)
            return;

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        if (!showInBuild && !Application.isEditor)
            return;

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;

        Vector3 camPos = mainCamera ? mainCamera.transform.position : Vector3.zero;

        Vector3 mouse = Input.mousePosition;
        Vector3 worldMouse = Vector3.zero;
        bool mouseInFrustum = false;

        if (mainCamera != null)
        {
            // Check if inside screen
            mouseInFrustum = mouse.x >= 0 && mouse.y >= 0 &&
                             mouse.x <= Screen.width && mouse.y <= Screen.height;

            if (mouseInFrustum)
            {
                float z = Mathf.Abs(mainCamera.transform.position.z);
                worldMouse = mainCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, z));
            }
        }

        int particleCount = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
        int bullCount = GameObject.FindGameObjectsWithTag(bullTag).Length;

        string text =
            $"FPS: {fps:F1} ({msec:F1} ms)\n" +
            $"Camera Pos: {camPos}\n" +
            $"Mouse: {mouse}\n" +
            $"World Mouse: {worldMouse}\n" +
            $"In Frustum: {mouseInFrustum}\n" +
            $"Particles: {particleCount}\n" +
            $"Bulls: {bullCount}";

        GUI.Label(new Rect(10, 10, 400, 150), text, style);
    }
}
