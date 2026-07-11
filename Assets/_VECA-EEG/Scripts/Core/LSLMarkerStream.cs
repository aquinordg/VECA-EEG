using UnityEngine;

/// <summary>
/// Singleton that maintains an LSL string marker outlet.
/// Other scripts call LSLMarkerStream.Instance?.SendMarker("...").
/// If lsl.dll is not present, disables silently (no crash).
/// </summary>
public class LSLMarkerStream : MonoBehaviour
{
    public static LSLMarkerStream Instance { get; private set; }

    private LSL.StreamInfo   streamInfo;
    private LSL.StreamOutlet outlet;
    private bool             ready;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        try
        {
            streamInfo = new LSL.StreamInfo("VECA-Markers", "Markers", 1, 0.0, "VECA-EEG");
            outlet     = new LSL.StreamOutlet(streamInfo);
            ready      = true;
            Debug.Log("[LSL] Marker stream ready. Waiting for receiver (BrainVision Recorder).");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LSL] Could not initialize: {e.Message}\n" +
                             "Check that lsl.dll is in Assets/Plugins/. Markers disabled.");
        }
    }

    void OnDestroy()
    {
        outlet?.Dispose();
        streamInfo?.Dispose();
    }

    public void SendMarker(string marker)
    {
        if (!ready) return;
        outlet.PushSample(new[] { marker });
        Debug.Log($"[LSL] {marker}");
    }
}
