using System.Collections;
using UnityEngine;

/// <summary>
/// Tarefa de Cálculo — features: vr_calc4, vr_calc5, vr_calc6
///
/// Três subtrações seriais (100−7). O participante vê o problema na instrução
/// e fixa na resposta correta entre 4 opções numéricas.
///
/// CONFIGURAÇÃO NO INSPECTOR:
///   Trials[0] → problema "100 − 7 =", opções ["93","92","94","97"], correta "93"
///   Trials[1] → problema "93 − 7 =",  opções ["86","85","87","84"], correta "86"
///   Trials[2] → problema "86 − 7 =",  opções ["79","78","80","77"], correta "79"
/// </summary>
public class CalculationTask : TaskBase
{
    [System.Serializable]
    public struct TrialCalculo
    {
        [Tooltip("Problema exibido na instrução (ex: '100 − 7 =')")]
        public string problema;
        [Tooltip("4 opções numéricas nos botões")]
        public string[] opcoes;
        [Tooltip("Resposta correta (deve ser igual a uma das opções)")]
        public string respostaCorreta;
        [Tooltip("Nome da feature no CSV")]
        public string nomeFeature;
    }

    [Header("Trials")]
    public TrialCalculo[] trials = new TrialCalculo[]
    {
        new TrialCalculo { problema = "100 − 7 =", opcoes = new[]{"93","92","94","97"}, respostaCorreta = "93", nomeFeature = "vr_calc4" },
        new TrialCalculo { problema = "93 − 7 =",  opcoes = new[]{"86","85","87","84"}, respostaCorreta = "86", nomeFeature = "vr_calc5" },
        new TrialCalculo { problema = "86 − 7 =",  opcoes = new[]{"79","78","80","77"}, respostaCorreta = "79", nomeFeature = "vr_calc6" },
    };

    [Header("Tempos")]
    public float pausaEntreTrials = 1f;

    private float[] scores;
    private int     trialAtual;

    protected override void Awake()
    {
        base.Awake();
        taskName      = "Cálculo";
        executionTime = 8f;
        scores        = new float[trials.Length];
    }

    // ── API para TestManager ─────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
        for (int i = 0; i < trials.Length; i++)
        {
            trialAtual = i;
            yield return ExecutarUmTrial(i);
            if (i < trials.Length - 1)
                yield return new WaitForSeconds(pausaEntreTrials);
        }
    }

    /// <summary>Score do trial idx (0–2) após conclusão (0–1).</summary>
    public float GetTrialScore(int idx) =>
        idx >= 0 && idx < scores.Length ? scores[idx] : 0f;

    // ── Trial ────────────────────────────────────────────────────────────────

    private IEnumerator ExecutarUmTrial(int idx)
    {
        var trial = trials[idx];
        uiManager.SetTaskStatus($"Cálculo ({idx + 1}/{trials.Length})");

        // Instrução com o problema durante preparação
        uiManager.ShowInstruction($"<b>{trial.problema}</b>\nOlhe para a resposta correta.");
        yield return new WaitForSeconds(preparationTime);

        // AOIs com as opções numéricas
        uiManager.SetupAOIs(trial.opcoes, trial.respostaCorreta);
        uiManager.ShowAOIs(true);

        // Problema permanece visível durante execução
        uiManager.ShowInstruction($"<b>{trial.problema}</b>");

        AOI aoiCorreta = uiManager.GetCorrectAOI();
        eyeTracker.SetCurrentCorrectAOI(aoiCorreta);
        eyeTracker.StartRecording();

        float decorrido = 0f;
        while (decorrido < executionTime)
        {
            decorrido += Time.deltaTime;
            uiManager.UpdateTimer(executionTime - decorrido);
            yield return null;
        }

        eyeTracker.StopRecording();
        uiManager.HideInstruction();
        scores[idx] = eyeTracker.GetCorrectAOIPercentage();

        uiManager.ShowAOIs(false);
    }

    // ── Implementações obrigatórias de TaskBase ──────────────────────────────

    protected override void SetupTrial()
    {
        if (trials.Length > trialAtual)
            uiManager.SetupAOIs(trials[trialAtual].opcoes, trials[trialAtual].respostaCorreta);
    }

    protected override float  CalculateScore()    => scores.Length > trialAtual ? scores[trialAtual] : 0f;
    protected override string GetFeatureName()    => trialAtual < trials.Length ? trials[trialAtual].nomeFeature : "vr_calc";
    protected override string GetInstructionText() => trials.Length > trialAtual ? trials[trialAtual].problema : "";
}
