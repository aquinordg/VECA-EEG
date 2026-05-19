using System.Collections;
using System.Collections.Generic;
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
    public class TrialCalculo
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

    [Header("Aleatorização")]
    [Tooltip("Gera problemas aleatórios a cada sessão (sobrescreve os valores acima)")]
    public bool aleatorio = true;

    [Header("Tempos")]
    public float pausaEntreTrials = 1f;

    private float[]           scores;
    private System.DateTime[] _trialStartTimes;
    private System.DateTime[] _trialEndTimes;
    private int               trialAtual;

    private void Reset()
    {
        trials = new TrialCalculo[]
        {
            new TrialCalculo { problema = "100 − 7 =", opcoes = new[]{"93","92","94","97"}, respostaCorreta = "93", nomeFeature = "vr_calc4" },
            new TrialCalculo { problema = "93 − 7 =",  opcoes = new[]{"86","85","87","84"}, respostaCorreta = "86", nomeFeature = "vr_calc5" },
            new TrialCalculo { problema = "86 − 7 =",  opcoes = new[]{"79","78","80","77"}, respostaCorreta = "79", nomeFeature = "vr_calc6" },
        };
    }

    protected override void Awake()
    {
        base.Awake();
        taskName         = "CÁLCULO";
        executionTime    = 8f;
        scores           = new float[trials.Length];
        _trialStartTimes = new System.DateTime[trials.Length];
        _trialEndTimes   = new System.DateTime[trials.Length];
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TAREFA:</b> CÁLCULO\n\n" +
                "Uma conta de subtração será exibida na instrução.\n" +
                "Entre as 4 opções na tela, fixe o olhar na resposta correta.\n\n" +
                "<b>Exemplo:</b> \"10 − 8 =\" → olhe para o número 2.\n\n" +
                "Esta tarefa tem 3 rodadas.";
    }

    // ── API para TestManager ─────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
        if (aleatorio) GerarTrialsAleatorios();

        yield return StartCoroutine(IntroPhase());

        for (int i = 0; i < trials.Length; i++)
        {
            trialAtual = i;
            yield return ExecutarUmTrial(i);
            if (i < trials.Length - 1)
                yield return new WaitForSeconds(pausaEntreTrials);
        }
    }

    public (System.DateTime start, System.DateTime end) GetTrialTimes(int idx) =>
        (_trialStartTimes[idx], _trialEndTimes[idx]);

    /// <summary>Score do trial idx (0–2) após conclusão (0–1).</summary>
    public float GetTrialScore(int idx) =>
        idx >= 0 && idx < scores.Length ? scores[idx] : 0f;

    // ── Trial ────────────────────────────────────────────────────────────────

    private IEnumerator ExecutarUmTrial(int idx)
    {
        var trial = trials[idx];
        uiManager.SetTaskStatus($"CÁLCULO ({idx + 1}/{trials.Length})");

        // Instrução com o problema durante preparação
        uiManager.ShowInstruction($"Olhe para a resposta correta.\n\n<b>{trial.problema}</b>");
        yield return new WaitForSeconds(preparationTime);

        // AOIs com as opções numéricas
        uiManager.SetupAOIs(trial.opcoes, trial.respostaCorreta);
        uiManager.ShowAOIs(true);

        // Problema permanece visível durante execução
        uiManager.ShowInstruction($"Olhe para a resposta correta.\n\n<b>{trial.problema}</b>");

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
        _trialStartTimes[idx] = eyeTracker.RecordingStartTime;
        _trialEndTimes[idx]   = eyeTracker.RecordingEndTime;
        uiManager.HideInstruction();
        scores[idx] = eyeTracker.GetCorrectAOIPercentage();

        uiManager.ShowAOIs(false);

        bool correct = scores[idx] >= 0.5f;
        uiManager.ShowFeedback($"{scores[idx] * 100f:F0}% do tempo na resposta correta", correct);
        yield return new WaitForSeconds(1.5f);
        uiManager.HideFeedback();
    }

    // ── Geração Aleatória ────────────────────────────────────────────────────

    private void GerarTrialsAleatorios()
    {
        for (int i = 0; i < trials.Length; i++)
        {
            int a, b;
            do {
                a = Random.Range(1, 11); // 1–10
                b = Random.Range(1, 6);  // 1–5
            } while (a <= b);            // garante a > b

            bool soma      = Random.value >= 0.5f;
            int  resultado = soma ? a + b : a - b;
            string operador = soma ? "+" : "−";

            var distratores = GerarDistratores(resultado, 3);

            string featureName = trials[i].nomeFeature;
            trials[i] = new TrialCalculo
            {
                problema        = $"{a} {operador} {b} =",
                opcoes          = new[] { resultado.ToString(), distratores[0].ToString(),
                                          distratores[1].ToString(), distratores[2].ToString() },
                respostaCorreta = resultado.ToString(),
                nomeFeature     = featureName
            };
        }
    }

    private static List<int> GerarDistratores(int correto, int quantidade)
    {
        var pool = new List<int>();
        for (int d = 1; d <= 5; d++)
        {
            if (correto + d > 0) pool.Add(correto + d);
            if (correto - d > 0) pool.Add(correto - d);
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool.GetRange(0, Mathf.Min(quantidade, pool.Count));
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
