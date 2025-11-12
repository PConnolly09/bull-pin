using Unity.Cinemachine;
using UnityEngine;

public class CameraScaler : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private float baseSize = 5f;
    [SerializeField] private float sizeScale = 0.5f;

    public void AdjustCameraDistance(BattlefieldData data)
    {
        if (vcam == null)
        {
            Debug.LogWarning("CameraScaler: No CinemachineCamera assigned.");
            return;
        }

        float maxExtent = Mathf.Max(data.bounds.extents.x, data.bounds.extents.y);
        float newSize = baseSize + maxExtent * sizeScale;

        // This assumes your virtual camera is orthographic
        vcam.Lens.OrthographicSize = newSize;
    }
}
