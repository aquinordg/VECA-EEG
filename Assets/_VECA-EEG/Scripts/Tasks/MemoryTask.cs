using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarefa de Memória — replica o protocolo do artigo VECA.
///
/// PROTOCOLO POR TRIAL:
///   Encoding  (encodingTime s) → mostra a imagem/rótulo alvo isolado
///   Storage   (storageDelay s) → tela em branco (intervalo de retenção)
///   Recall    (executionTime s)→ 4 AOIs na tela; participante fixa na correta
///
/// FEATURES GERADAS:
///   Trial 0 → vr_mem8
///   Trial 1 → vr_mem9
///   Trial 2 → vr_mem10
///
/// CONFIGURAÇÃO NO INSPECTOR:
///   TargetLabels        → 3 strings ("Leão", "Rinoceronte", "Camelo")
///   DistractorLabels    → lista de outros rótulos para preenchimento das AOIs
///   EncodingDisplay     → GameObject com Image para mostrar o sprite do alvo
///   EncodingImage       → Image dentro do EncodingDisplay
///   TargetSprites       → 3 Sprites (mesma ordem dos TargetLabels)
///   encodingTime        → 4  (padrão artigo)
///   executionTime       → 8  (padrão artigo, herdado de TaskBase)
///   preparationTime     → 3  (instrução antes de cada trial)
/// </summary>
public class MemoryTask : TaskBase
{
    [Header("Estímulos")]
    [Tooltip("Rótulos dos 3 alvos: índice 0=mem8, 1=mem9, 2=mem10")]
    public string[] targetLabels = { "Leão", "Rinoceronte", "Camelo" };

    [Tooltip("Sprites dos alvos (mesma ordem dos rótulos). Opcional.")]
    public Sprite[] targetSprites;

    [Tooltip("Pool de sprites distratores para o recall com imagens (mínimo 3)")]
    public Sprite[] distractorSprites;

    [Tooltip("Pool de rótulos distratores (usado quando sprites não estão configurados)")]
    public string[] distractorLabels = { "Elefante", "Zebra", "Girafa", "Tigre", "Lobo" };

    [Header("Display de Encoding")]
    [Tooltip("GameObject exibido apenas durante a fase de encoding")]
    public GameObject encodingDisplay;

    [Tooltip("Image dentro do encodingDisplay (opcional)")]
    public Image encodingImage;

    [Header("Tempos")]
    [Tooltip("Duração da exibição do alvo na fase de encoding (s)")]
    public float encodingTime = 4f;
    [Tooltip("Intervalo entre encoding e recall — tela em branco (s)")]
    public float storageDelay = 5f;
    [Tooltip("Pausa de feedback entre trials (s)")]
    public float pausaEntrieTrials = 0.8f;

    // ── Estado interno ───────────────────────────────────────────────────────

    private int trialAtual;
    private readonly float[] scores = new float[3];

    private static readonly string[] nomesFeature =
        { "vr_mem8", "vr_mem9", "vr_mem10" };

    protected override void Awake()
    {
        base.Awake();
        taskName      = "Memória";
        executionTime = 8f;
    }

    // ── API Pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Executa os 3 trials em sequência e preenche <see cref="GetTrialScore"/>.
    /// Deve ser chamado pelo TestManager via StartCoroutine.
    /// </summary>
    public IEnumerator RunAllTrials()
    {
        for (int i = 0; i < 3; i++)
        {
            trialAtual = i;
            yield return StartCoroutine(ExecutarUmTrial(i));
            yield return new WaitForSeconds(pausaEntrieTrials);
        }
    }

    /// <summary>Score do trial [0-2] após a conclusão (0–1).</summary>
    public float GetTrialScore(int index) =>
        index >= 0 && index < scores.Length ? scores[index] : 0f;

    // ── Trial Completo ───────────────────────────────────────────────────────

    private IEnumerator ExecutarUmTrial(int idx)
    {
        string alvo = targetLabels[idx];

        uiManager.SetTaskStatus($"Memória ({idx + 1}/3)");

        // ── FASE 1: ENCODING ─────────────────────────────────────────────────
        MostrarEncodingDisplay(idx, true);
        uiManager.ShowAOIs(false);
        uiManager.ShowInstruction($"Memorize esta imagem", encodingTime);

        yield return new WaitForSeconds(encodingTime);

        uiManager.HideInstruction();
        MostrarEncodingDisplay(idx, false);

        // ── FASE 2: STORAGE ──────────────────────────────────────────────────
        uiManager.ShowInstruction("...", storageDelay);
        yield return new WaitForSeconds(storageDelay);
        uiManager.HideInstruction();

        // ── FASE 3: RECALL ───────────────────────────────────────────────────
        ConfigurarAOIsDeRecall(idx, alvo);

        // Instrução permanece visível durante todo o recall (sem duração automática)
        uiManager.ShowInstruction("Fixe o olhar na resposta correta.");

        // Avisar EyeTracker qual é a AOI correta
        AOI aoiCorreta = uiManager.GetCorrectAOI();
        eyeTracker.SetCurrentCorrectAOI(aoiCorreta);
        eyeTracker.StartRecording();

        // Countdown de recall
        float decorrido = 0f;
        while (decorrido < executionTime)
        {
            decorrido += Time.deltaTime;
            uiManager.UpdateTimer(executionTime - decorrido);
            yield return null;
        }

        eyeTracker.StopRecording();
        uiManager.HideInstruction();

        // ── SCORE ────────────────────────────────────────────────────────────
        scores[idx] = eyeTracker.GetCorrectAOIPercentage();

        bool acertou = scores[idx] >= 0.5f; // >50% do tempo na correta
        uiManager.ShowFeedback(acertou ? "Correto!" : "Incorreto.", acertou);
        uiManager.ShowAOIs(false);

        yield return new WaitForSeconds(1.5f);
        uiManager.HideFeedback();
    }

    // ── Setup das AOIs de Recall ─────────────────────────────────────────────

    private void ConfigurarAOIsDeRecall(int idx, string alvo)
    {
        bool usarSprites = targetSprites != null && idx < targetSprites.Length
                        && distractorSprites != null && distractorSprites.Length >= 3;

        if (usarSprites)
            ConfigurarRecallComSprites(idx);
        else
            ConfigurarRecallComTexto(alvo);

        uiManager.ShowAOIs(true);
    }

    private void ConfigurarRecallComSprites(int idx)
    {
        Sprite alvoSprite = targetSprites[idx];

        // Pool de distratores: sprites distratores + outros alvos (exceto o atual)
        var pool = new List<Sprite>(distractorSprites);
        foreach (var s in targetSprites)
            if (s != alvoSprite && !pool.Contains(s))
                pool.Add(s);

        var opcoes = new List<Sprite> { alvoSprite };
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int r = Random.Range(0, pool.Count);
            opcoes.Add(pool[r]);
            pool.RemoveAt(r);
        }

        uiManager.SetupAOIs(opcoes.ToArray(), alvoSprite);
    }

    private void ConfigurarRecallComTexto(string alvo)
    {
        var pool = new List<string>(distractorLabels);
        foreach (var lbl in targetLabels)
            pool.Remove(lbl);

        foreach (var lbl in targetLabels)
            if (lbl != alvo && !pool.Contains(lbl))
                pool.Add(lbl);

        var opcoes = new List<string> { alvo };
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int r = Random.Range(0, pool.Count);
            opcoes.Add(pool[r]);
            pool.RemoveAt(r);
        }

        uiManager.SetupAOIs(opcoes.ToArray(), alvo);
    }

    // ── Display de Encoding ──────────────────────────────────────────────────

    private void MostrarEncodingDisplay(int idx, bool ativo)
    {
        if (encodingDisplay == null) return;
        encodingDisplay.SetActive(ativo);

        if (!ativo) return;

        // Atribuir sprite se disponível
        if (encodingImage != null && targetSprites != null && idx < targetSprites.Length)
            encodingImage.sprite = targetSprites[idx];
    }

    // ── Implementação de TaskBase (para uso com StartTask() de trial único) ──

    protected override void SetupTrial()
    {
        ConfigurarAOIsDeRecall(trialAtual, targetLabels[trialAtual]);
    }

    protected override float CalculateScore() =>
        eyeTracker.GetCorrectAOIPercentage();

    protected override string GetFeatureName() =>
        trialAtual < nomesFeature.Length ? nomesFeature[trialAtual] : "vr_mem";

    protected override string GetInstructionText() =>
        $"Onde estava: <b>{targetLabels[trialAtual]}</b>?";
}
