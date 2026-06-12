using UnityEngine;

/// <summary>
/// Cursor 3D que segue o raio de gaze no espaço do mundo.
/// Quando não há gaze rastreado, fica na direção frontal da câmera.
/// </summary>
public class GazeCursor : MonoBehaviour
{
    public EyeTracker eyeTracker;

    [Tooltip("Distância do cursor ao longo do raio de gaze")]
    public float defaultDistance = 2f;

    private Camera vrCamera;
    private Renderer cursorRenderer;

    void Start()
    {
        vrCamera       = eyeTracker.vrCamera != null ? eyeTracker.vrCamera : Camera.main;
        cursorRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        if (vrCamera == null) return;

        Vector3 origem;
        Vector3 direcao;

        if (eyeTracker.TryGetGazeRay(out origem, out direcao))
        {
            if (cursorRenderer != null) cursorRenderer.enabled = true;
            PositionarCursor(origem, direcao);
        }
        else
        {
            if (cursorRenderer != null) cursorRenderer.enabled = true;
            origem  = vrCamera.transform.position;
            direcao = vrCamera.transform.forward;
            PositionarCursor(origem, direcao);
        }
    }

    private void PositionarCursor(Vector3 origem, Vector3 direcao)
    {
        RaycastHit hit;
        if (Physics.Raycast(origem, direcao, out hit))
            transform.position = hit.point;
        else
            transform.position = origem + direcao * defaultDistance;

        transform.rotation = Quaternion.LookRotation(direcao);
    }
}
