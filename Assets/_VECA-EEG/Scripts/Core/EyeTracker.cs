using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

public class EyeTracker : MonoBehaviour
{
    [Header("Canvas Raycasting")]
    [Tooltip("GraphicRaycaster of the WorldCanvas containing the AOIs")]
    public GraphicRaycaster canvasRaycaster;

    [Header("VR Camera")]
    [Tooltip("Main Camera of the XR Origin (if empty, uses Camera.main)")]
    public Camera vrCamera;

    [Header("Fixation Parameters")]
    [Tooltip("Maximum angular displacement (degrees) between frames to count as a fixation")]
    public float angularThresholdDeg = 1.5f;
    [Tooltip("Minimum dwell time (s) before accumulation begins")]
    public float minFixationDuration = 0.12f;

    // ── Internal state ───────────────────────────────────────────────────────

    private bool recording;

    private AOI currentAOI;
    private AOI correctAOI;

    // Fixation control
    private Vector3 previousDirection;
    private float   stoppedTime;
    private bool    isFixating;
    private bool    fixationRegistered;

    // Accumulators
    private float timeOnCorrect;
    private float totalFixationTimeAccum;
    private float totalRecordingTimeAccum;

    // Eye tracking (OpenXR Eye Gaze Interaction)
    private InputAction gazeAction;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        gazeAction = new InputAction("EyeGaze", binding: "<EyeGaze>/pose", expectedControlType: "Pose");
        gazeAction.Enable();
    }

    void Start()
    {
        if (vrCamera == null) vrCamera = Camera.main;

        if (gazeAction.controls.Count == 0)
            Debug.LogWarning("[EyeTracker] NO Eye Gaze detected. Check:\n" +
                "  1) SRanipal running in the Windows system tray\n" +
                "  2) Eye Gaze Interaction enabled in Project Settings > XR > OpenXR > Features\n" +
                "  3) SteamVR active and headset awake\n" +
                "  4) Eye tracking calibrated in the SteamVR Dashboard");

        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDestroy()
    {
        gazeAction?.Disable();
        gazeAction?.Dispose();
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            if (gazeAction.controls.Count == 0)
                Debug.LogWarning("[EyeTracker] Device connected but Eye Gaze still not detected.");
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public System.DateTime RecordingStartTime { get; private set; }
    public System.DateTime RecordingEndTime   { get; private set; }

    /// <summary>Label sent in LSL trial_start/trial_end markers. Must be set before StartRecording().</summary>
    public string CurrentTrialLabel { get; set; } = "";

    public void StartRecording()
    {
        RecordingStartTime      = System.DateTime.Now;
        LSLMarkerStream.Instance?.SendMarker($"trial_start,{CurrentTrialLabel}");
        recording               = true;
        currentAOI              = null;
        timeOnCorrect           = 0f;
        totalFixationTimeAccum  = 0f;
        totalRecordingTimeAccum = 0f;
        stoppedTime             = 0f;
        isFixating              = false;
        fixationRegistered      = false;
        if (!TryGetGazeRay(out _, out previousDirection))
            previousDirection = vrCamera != null ? vrCamera.transform.forward : Vector3.forward;
    }

    public void StopRecording()
    {
        RecordingEndTime = System.DateTime.Now;
        LSLMarkerStream.Instance?.SendMarker($"trial_end,{CurrentTrialLabel}");
        recording        = false;
        DeactivateHighlight(currentAOI);
        currentAOI       = null;
    }

    public void SetCurrentCorrectAOI(AOI aoi) => correctAOI = aoi;

    // ── Metrics ──────────────────────────────────────────────────────────────

    public float GetTimeOnCorrectAOI()    => timeOnCorrect;
    public float GetTotalFixatedTime()    => totalFixationTimeAccum;
    public float GetTotalRecordingTime()  => totalRecordingTimeAccum;

    public float GetCorrectAOIPercentage()
    {
        if (totalRecordingTimeAccum <= 0f) return 0f;
        return Mathf.Clamp01(timeOnCorrect / totalRecordingTimeAccum);
    }

    // ── Main loop ────────────────────────────────────────────────────────────

    void Update()
    {
        if (!recording) return;

        totalRecordingTimeAccum += Time.deltaTime;

        AOI detectedAOI = DetectGazedAOI();
        UpdateVisualHighlight(detectedAOI);
        ProcessFixation(detectedAOI);
    }

    // ── Gaze detection ───────────────────────────────────────────────────────

    private AOI DetectGazedAOI()
    {
        if (canvasRaycaster == null || EventSystem.current == null) return null;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = GetGazePosition()
        };

        var results = new List<RaycastResult>();
        canvasRaycaster.Raycast(pointer, results);

        foreach (var r in results)
        {
            var aoi = r.gameObject.GetComponent<AOI>()
                   ?? r.gameObject.GetComponentInParent<AOI>();
            if (aoi != null) return aoi;
        }
        return null;
    }

    // ── Fixation accumulation ────────────────────────────────────────────────

    private void ProcessFixation(AOI detectedAOI)
    {
        if (!TryGetGazeRay(out _, out Vector3 currentDirection))
            currentDirection = vrCamera != null ? vrCamera.transform.forward : Vector3.forward;

        float angle = Vector3.Angle(currentDirection, previousDirection);
        previousDirection = currentDirection;

        if (angle <= angularThresholdDeg)
        {
            stoppedTime += Time.deltaTime;
        }
        else
        {
            stoppedTime        = 0f;
            isFixating         = false;
            fixationRegistered = false;
        }

        bool thresholdReached = stoppedTime >= minFixationDuration;

        if (thresholdReached && detectedAOI != null)
        {
            if (!isFixating || detectedAOI != currentAOI)
            {
                isFixating         = true;
                fixationRegistered = false;
            }

            float dt = Time.deltaTime;
            detectedAOI.totalFixationTime += dt;
            detectedAOI.wasLookedAt        = true;
            totalFixationTimeAccum        += dt;

            if (detectedAOI.firstFixationTime < 0f)
                detectedAOI.firstFixationTime = totalRecordingTimeAccum;

            if (!fixationRegistered)
            {
                detectedAOI.fixationCount++;
                fixationRegistered = true;
            }

            if (correctAOI != null && detectedAOI == correctAOI)
                timeOnCorrect += dt;
        }
        else if (!thresholdReached)
        {
            isFixating         = false;
            fixationRegistered = false;
        }
    }

    // ── Visual feedback ──────────────────────────────────────────────────────

    private void UpdateVisualHighlight(AOI detectedAOI)
    {
        if (detectedAOI == currentAOI) return;
        DeactivateHighlight(currentAOI);
        currentAOI = detectedAOI;
        if (currentAOI != null) currentAOI.Highlight();
    }

    private void DeactivateHighlight(AOI aoi)
    {
        if (aoi != null) aoi.Unhighlight();
    }

    // ── Gaze position ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the screen-space position the gaze is pointing at.
    /// Uses OpenXR Eye Gaze when available; falls back to mouse in the editor.
    /// </summary>
    public Vector2 GetGazePosition()
    {
        Camera cam = vrCamera != null ? vrCamera : Camera.main;
        if (cam == null) return Vector2.zero;

        if (TryGetGazeRay(out Vector3 origin, out Vector3 direction))
        {
            Vector3 worldPoint = origin + direction * 10f;
            return cam.WorldToScreenPoint(worldPoint);
        }

        // Fallback: mouse (editor without a device)
        var mouse = Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
    }

    /// <summary>
    /// Retrieves the gaze ray from OpenXR Eye Gaze Interaction.
    /// Requires "Eye Gaze Interaction" to be enabled in
    /// Project Settings > XR Plug-in Management > OpenXR > Features.
    /// </summary>
    public bool TryGetGazeRay(out Vector3 origin, out Vector3 direction)
    {
        if (gazeAction != null && gazeAction.controls.Count > 0)
        {
            var pose = gazeAction.ReadValue<PoseState>();

            bool hasData = pose.isTracked ||
                (pose.trackingState & (UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation))
                == (UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation);

            if (hasData)
            {
                origin    = pose.position;
                direction = pose.rotation * Vector3.forward;
                return true;
            }
        }

        origin    = Vector3.zero;
        direction = Vector3.forward;
        return false;
    }
}
