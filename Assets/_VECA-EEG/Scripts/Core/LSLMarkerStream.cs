using UnityEngine;

/// <summary>
/// Singleton que mantém um LSL outlet de marcadores string.
/// Outros scripts chamam LSLMarkerStream.Instance?.SendMarker("...").
/// Se lsl.dll não estiver presente, desabilita silenciosamente (sem crash).
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
            Debug.Log("[LSL] Marker stream pronto. Aguardando receptor (BrainVision Recorder).");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LSL] Não foi possível inicializar: {e.Message}\n" +
                             "Verifique se lsl.dll está em Assets/Plugins/. Markers desativados.");
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
