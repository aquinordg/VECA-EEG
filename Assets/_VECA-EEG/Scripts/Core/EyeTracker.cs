using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Simula eye-tracking usando o cursor do mouse.
/// Detecta "fixação" quando o mouse permanece estático por
/// <see cref="duracaoMinimaFixacao"/> segundos dentro de uma AOI.
///
/// FEATURE PRINCIPAL (artigo VECA):
///   score = timeOnCorrectAOI / tempoTotalGravacao
///
/// CONFIGURAÇÃO NO INSPECTOR:
///   - CanvasRaycaster → GraphicRaycaster do WorldCanvas
///   - (ajuste os thresholds conforme necessário)
///
/// MIGRAÇÃO PARA VIVE PRO EYE:
///   Substitua DetectarAOISobOlhar() para usar a API do SDK SRanipal/OpenXR
///   e remova os campos de simulação por mouse. O resto do pipeline
///   (acumulação, scoring) permanece igual.
/// </summary>
public class EyeTracker : MonoBehaviour
{
    [Header("Raycasting no Canvas")]
    [Tooltip("GraphicRaycaster do WorldCanvas que contém as AOIs")]
    public GraphicRaycaster canvasRaycaster;

    [Header("Parâmetros de Fixação (Simulação)")]
    [Tooltip("Deslocamento máximo (px) para ainda contar como estático")]
    public float limiarMovimento = 8f;
    [Tooltip("Tempo mínimo parado (s) para iniciar acumulação")]
    public float duracaoMinimaFixacao = 0.12f;

    // ── Estado interno ───────────────────────────────────────────────────────

    private bool gravando;

    private AOI aoiAtual;          // AOI sob o olhar agora
    private AOI aoiCorreta;        // AOI marcada como alvo pela tarefa

    // Controle de fixação
    private Vector2 posMouseAnterior;
    private float tempoParado;
    private bool estaFixando;
    private bool fixacaoRegistrada; // evita contar a mesma fixação duas vezes

    // Acumuladores (reiniciados a cada StartRecording)
    private float tempoNaCorreta;
    private float tempoTotalFixado;  // soma de todo tempo fixado em qualquer AOI
    private float tempoTotalGravacao;

    // ── API Pública ──────────────────────────────────────────────────────────

    public void StartRecording()
    {
        gravando            = true;
        tempoNaCorreta      = 0f;
        tempoTotalFixado    = 0f;
        tempoTotalGravacao  = 0f;
        tempoParado         = 0f;
        estaFixando         = false;
        fixacaoRegistrada   = false;
        posMouseAnterior    = PosicaoMouse();
    }

    public void StopRecording()
    {
        gravando = false;
        DesativarDestaque(aoiAtual);
        aoiAtual = null;
    }

    /// <summary>
    /// Define qual AOI é a resposta correta para o trial atual.
    /// Deve ser chamado ANTES de StartRecording() ou logo após.
    /// </summary>
    public void SetCurrentCorrectAOI(AOI aoi)
    {
        aoiCorreta = aoi;
    }

    // ── Métricas ─────────────────────────────────────────────────────────────

    /// <summary>Segundos totais que o olhar ficou sobre a AOI correta.</summary>
    public float GetTimeOnCorrectAOI() => tempoNaCorreta;

    /// <summary>Segundos totais de fixação em qualquer AOI.</summary>
    public float GetTotalFixatedTime() => tempoTotalFixado;

    /// <summary>Segundos totais da gravação (desde StartRecording).</summary>
    public float GetTotalRecordingTime() => tempoTotalGravacao;

    /// <summary>
    /// Feature do artigo: % do tempo de gravação gasto fixando na AOI correta.
    /// Retorna valor entre 0 e 1.
    /// </summary>
    public float GetCorrectAOIPercentage()
    {
        if (tempoTotalGravacao <= 0f) return 0f;
        return Mathf.Clamp01(tempoNaCorreta / tempoTotalGravacao);
    }

    // ── Loop Principal ───────────────────────────────────────────────────────

    void Update()
    {
        if (!gravando) return;

        tempoTotalGravacao += Time.deltaTime;

        AOI aoiDetectada = DetectarAOISobOlhar();
        AtualizarDestaqueVisual(aoiDetectada);
        ProcessarFixacao(aoiDetectada);
    }

    // ── Detecção de Gaze ─────────────────────────────────────────────────────

    /// <summary>
    /// Retorna a AOI que está sob o cursor do mouse (simulação de gaze).
    /// Usa o GraphicRaycaster do WorldCanvas para respeitar a hierarquia UI.
    /// </summary>
    private AOI DetectarAOISobOlhar()
    {
        if (canvasRaycaster == null || EventSystem.current == null) return null;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = PosicaoMouse()
        };

        var resultados = new List<RaycastResult>();
        canvasRaycaster.Raycast(pointer, resultados);

        foreach (var r in resultados)
        {
            // Procura AOI no próprio objeto ou nos pais
            var aoi = r.gameObject.GetComponent<AOI>()
                   ?? r.gameObject.GetComponentInParent<AOI>();
            if (aoi != null) return aoi;
        }
        return null;
    }

    // ── Acumulação de Fixação ────────────────────────────────────────────────

    private void ProcessarFixacao(AOI aoiDetectada)
    {
        Vector2 posAtual = PosicaoMouse();
        float deslocamento = Vector2.Distance(posAtual, posMouseAnterior);
        posMouseAnterior = posAtual;

        bool mouseParado = deslocamento <= limiarMovimento;

        if (mouseParado)
        {
            tempoParado += Time.deltaTime;
        }
        else
        {
            // Movimento quebra a fixação
            tempoParado         = 0f;
            estaFixando         = false;
            fixacaoRegistrada   = false;
        }

        bool atingiuLimiar = tempoParado >= duracaoMinimaFixacao;

        if (atingiuLimiar && aoiDetectada != null)
        {
            // Transição: começa a fixar nesta AOI
            if (!estaFixando || aoiDetectada != aoiAtual)
            {
                estaFixando       = true;
                fixacaoRegistrada = false; // nova fixação, registrar uma vez
            }

            float dt = Time.deltaTime;
            aoiDetectada.totalFixationTime += dt;
            aoiDetectada.wasLookedAt        = true;
            tempoTotalFixado               += dt;

            // Registrar início da primeira fixação
            if (aoiDetectada.firstFixationTime < 0f)
                aoiDetectada.firstFixationTime = tempoTotalGravacao;

            // Contar evento de fixação (uma única vez por bloco contínuo)
            if (!fixacaoRegistrada)
            {
                aoiDetectada.fixationCount++;
                fixacaoRegistrada = true;
            }

            // Acumular na AOI correta
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

    // Centraliza a leitura de posição para facilitar a migração para Vive Pro Eye
    private static Vector2 PosicaoMouse()
    {
        var mouse = Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
    }
}
