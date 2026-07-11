using UnityEngine;

/// <summary>
/// 3D cursor that follows the gaze ray in world space.
/// When no gaze is tracked, defaults to the camera's forward direction.
/// </summary>
public class GazeCursor : MonoBehaviour
{
    public EyeTracker eyeTracker;

    [Tooltip("Cursor distance along the gaze ray")]
    public float defaultDistance = 2f;

    private Camera   vrCamera;
    private Renderer cursorRenderer;

    void Start()
    {
        vrCamera       = eyeTracker.vrCamera != null ? eyeTracker.vrCamera : Camera.main;
        cursorRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        if (vrCamera == null) return;

        Vector3 origin;
        Vector3 direction;

        if (eyeTracker.TryGetGazeRay(out origin, out direction))
        {
            if (cursorRenderer != null) cursorRenderer.enabled = true;
            PositionCursor(origin, direction);
        }
        else
        {
            if (cursorRenderer != null) cursorRenderer.enabled = true;
            origin    = vrCamera.transform.position;
            direction = vrCamera.transform.forward;
            PositionCursor(origin, direction);
        }
    }

    private void PositionCursor(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit))
            transform.position = hit.point;
        else
            transform.position = origin + direction * defaultDistance;

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
