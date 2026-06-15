using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

public class EyeTracker : MonoBehaviour
{
    [Header("Raycasting no Canvas")]
    [Tooltip("GraphicRaycaster do WorldCanvas que contém as AOIs")]
    public GraphicRaycaster canvasRaycaster;

    [Header("Camera VR")]
    [Tooltip("Main Camera do XR Origin (se vazio usa Camera.main)")]
    public Camera vrCamera;

    [Header("Fixation Parameters")]
    [Tooltip("Deslocamento angular máximo (graus) entre frames para contar como fixação")]
    public float limiarAngularGraus = 1.5f;
    [Tooltip("Tempo mínimo parado (s) para iniciar acumulação")]
    public float duracaoMinimaFixacao = 0.12f;

    // ── Estado interno ───────────────────────────────────────────────────────

    private bool gravando;

    private AOI aoiAtual;
    private AOI aoiCorreta;

    // Controle de fixação
    private Vector3 direcaoAnterior;
    private float tempoParado;
    private bool estaFixando;
    private bool fixacaoRegistrada;

    // Acumuladores
    private float tempoNaCorreta;
    private float tempoTotalFixado;
    private float tempoTotalGravacao;

    // Eye tracking (OpenXR Eye Gaze Interaction)
    private InputAction gazeAction;

    // ── Ciclo de vida ────────────────────────────────────────────────────────

    void Awake()
    {
        gazeAction = new InputAction("EyeGaze", binding: "<EyeGaze>/pose", expectedControlType: "Pose");
        gazeAction.Enable();
    }

    void Start()
    {
        if (vrCamera == null) vrCamera = Camera.main;

        if (gazeAction.controls.Count == 0)
            Debug.LogWarning("[EyeTracker] NENHUM Eye Gaze detectado. Verifique:\n" +
                "  1) SRanipal rodando na bandeja do Windows\n" +
                "  2) Eye Gaze Interaction habilitado em Project Settings > XR > OpenXR > Features\n" +
                "  3) SteamVR ativo e headset acordado\n" +
                "  4) Eye tracking calibrado no SteamVR Dashboard");

        InputSystem.onDeviceChange += AoMudarDispositivo;
    }

    void OnDestroy()
    {
        gazeAction?.Disable();
        gazeAction?.Dispose();
        InputSystem.onDeviceChange -= AoMudarDispositivo;
    }

    private void AoMudarDispositivo(UnityEngine.InputSystem.InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            if (gazeAction.controls.Count == 0)
                Debug.LogWarning("[EyeTracker] Dispositivo conectado mas Eye Gaze ainda não detectado.");
    }

    // ── API Pública ──────────────────────────────────────────────────────────

    public System.DateTime RecordingStartTime { get; private set; }
    public System.DateTime RecordingEndTime   { get; private set; }

    /// <summary>Label enviado nos marcadores LSL trial_start/trial_end. Deve ser definido antes de StartRecording().</summary>
    public string CurrentTrialLabel { get; set; } = "";

    public void StartRecording()
    {
        RecordingStartTime  = System.DateTime.Now;
        LSLMarkerStream.Instance?.SendMarker($"trial_start,{CurrentTrialLabel}");
        gravando            = true;
        aoiAtual            = null;
        tempoNaCorreta      = 0f;
        tempoTotalFixado    = 0f;
        tempoTotalGravacao  = 0f;
        tempoParado         = 0f;
        estaFixando         = false;
        fixacaoRegistrada   = false;
        if (!TryGetGazeRay(out _, out direcaoAnterior))
            direcaoAnterior = vrCamera != null ? vrCamera.transform.forward : Vector3.forward;
    }

    public void StopRecording()
    {
        RecordingEndTime = System.DateTime.Now;
        LSLMarkerStream.Instance?.SendMarker($"trial_end,{CurrentTrialLabel}");
        gravando         = false;
        DesativarDestaque(aoiAtual);
        aoiAtual         = null;
    }

    public void SetCurrentCorrectAOI(AOI aoi) => aoiCorreta = aoi;

    // ── Métricas ─────────────────────────────────────────────────────────────

    public float GetTimeOnCorrectAOI()    => tempoNaCorreta;
    public float GetTotalFixatedTime()    => tempoTotalFixado;
    public float GetTotalRecordingTime()  => tempoTotalGravacao;

    public float GetCorrectAOIPercentage()
    {
        if (tempoTotalGravacao <= 0f) return 0f;
        return Mathf.Clamp01(tempoNaCorreta / tempoTotalGravacao);
    }

    // ── Loop principal ───────────────────────────────────────────────────────

    void Update()
    {
        if (!gravando) return;

        tempoTotalGravacao += Time.deltaTime;

        AOI aoiDetectada = DetectarAOISobOlhar();
        AtualizarDestaqueVisual(aoiDetectada);
        ProcessarFixacao(aoiDetectada);
    }

    // ── Detecção de Gaze ─────────────────────────────────────────────────────

    private AOI DetectarAOISobOlhar()
    {
        if (canvasRaycaster == null || EventSystem.current == null) return null;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = ObterPosicaoGaze()
        };

        var resultados = new List<RaycastResult>();
        canvasRaycaster.Raycast(pointer, resultados);

        foreach (var r in resultados)
        {
            var aoi = r.gameObject.GetComponent<AOI>()
                   ?? r.gameObject.GetComponentInParent<AOI>();
            if (aoi != null) return aoi;
        }
        return null;
    }

    // ── Acumulação de Fixação ────────────────────────────────────────────────

    private void ProcessarFixacao(AOI aoiDetectada)
    {
        if (!TryGetGazeRay(out _, out Vector3 direcaoAtual))
            direcaoAtual = vrCamera != null ? vrCamera.transform.forward : Vector3.forward;

        float angulo = Vector3.Angle(direcaoAtual, direcaoAnterior);
        direcaoAnterior = direcaoAtual;

        if (angulo <= limiarAngularGraus)
        {
            tempoParado += Time.deltaTime;
        }
        else
        {
            tempoParado       = 0f;
            estaFixando       = false;
            fixacaoRegistrada = false;
        }

        bool atingiuLimiar = tempoParado >= duracaoMinimaFixacao;

        if (atingiuLimiar && aoiDetectada != null)
        {
            if (!estaFixando || aoiDetectada != aoiAtual)
            {
                estaFixando       = true;
                fixacaoRegistrada = false;
            }

            float dt = Time.deltaTime;
            aoiDetectada.totalFixationTime += dt;
            aoiDetectada.wasLookedAt        = true;
            tempoTotalFixado               += dt;

            if (aoiDetectada.firstFixationTime < 0f)
                aoiDetectada.firstFixationTime = tempoTotalGravacao;

            if (!fixacaoRegistrada)
            {
                aoiDetectada.fixationCount++;
                fixacaoRegistrada = true;
            }

            if (aoiCorreta != null && aoiDetectada == aoiCorreta)
                tempoNaCorreta += dt;
        }
        else if (!atingiuLimiar)
        {
            estaFixando       = false;
            fixacaoRegistrada = false;
        }
    }

    // ── Feedback Visual ──────────────────────────────────────────────────────

    private void AtualizarDestaqueVisual(AOI aoiDetectada)
    {
        if (aoiDetectada == aoiAtual) return;
        DesativarDestaque(aoiAtual);
        aoiAtual = aoiDetectada;
        if (aoiAtual != null) aoiAtual.Highlight();
    }

    private void DesativarDestaque(AOI aoi)
    {
        if (aoi != null) aoi.Unhighlight();
    }

    // ── Posição de Gaze ──────────────────────────────────────────────────────

    /// <summary>
    /// Retorna a posição em screen-space para onde o olhar aponta.
    /// Usa OpenXR Eye Gaze quando disponível; fallback para mouse no editor.
    /// </summary>
    public Vector2 ObterPosicaoGaze()
    {
        Camera cam = vrCamera != null ? vrCamera : Camera.main;
        if (cam == null) return Vector2.zero;

        if (TryGetGazeRay(out Vector3 origem, out Vector3 direcao))
        {
            Vector3 pontoMundo = origem + direcao * 10f;
            return cam.WorldToScreenPoint(pontoMundo);
        }

        // Fallback: mouse (editor sem dispositivo)
        var mouse = Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
    }

    /// <summary>
    /// Obtém o raio de gaze do OpenXR Eye Gaze Interaction.
    /// Requer que "Eye Gaze Interaction" esteja habilitado em
    /// Project Settings > XR Plug-in Management > OpenXR > Features.
    /// </summary>
    public bool TryGetGazeRay(out Vector3 origem, out Vector3 direcao)
    {
        if (gazeAction != null && gazeAction.controls.Count > 0)
        {
            var pose = gazeAction.ReadValue<PoseState>();

            bool temDados = pose.isTracked ||
                (pose.trackingState & (UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation))
                == (UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation);

            if (temDados)
            {
                origem  = pose.position;
                direcao = pose.rotation * Vector3.forward;
                return true;
            }
        }

        origem  = Vector3.zero;
        direcao = Vector3.forward;
        return false;
    }
}
