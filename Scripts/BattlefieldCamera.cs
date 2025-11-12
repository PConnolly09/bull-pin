using Unity.Cinemachine;
using UnityEngine;

[ExecuteAlways]
public class BattlefieldCamera : MonoBehaviour
{
    public Transform battlefieldAnchor;
    public Vector2 battlefieldSize = new(50, 30); // from generator
    public float borderPadding = 2f; // extra space around edges

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!battlefieldAnchor) return;

        // Center camera on battlefield
        transform.position = new Vector3(
            battlefieldAnchor.position.x,
            battlefieldAnchor.position.y,
            transform.position.z
        );

        // Adjust zoom for orthographic camera
        if (cam.orthographic)
        {
            float aspect = cam.aspect;
            float halfHeight = (battlefieldSize.y / 2f) + borderPadding;
            float halfWidth = (battlefieldSize.x / 2f) + borderPadding;
            cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
        }

    }
}
