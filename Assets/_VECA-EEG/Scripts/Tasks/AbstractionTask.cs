using System.Collections;
using UnityEngine;

/// <summary>
/// Tarefa de Abstração — feature: vr_abs
///
/// Dois trials: em cada um, o participante vê um par de conceitos na instrução
/// e deve fixar na categoria que os une entre 4 opções.
/// Score final = média dos 2 trials.
///
/// CONFIGURAÇÃO NO INSPECTOR:
///   Trials[0].Par              → "Trem e Bicicleta"
///   Trials[0].Opcoes           → ["Meio de Transporte","Animal","Cor","Alimento"]
///   Trials[0].RespostaCorreta  → "Meio de Transporte"
///   Trials[1].Par              → "Relógio e Régua"
///   Trials[1].Opcoes           → ["Instrumento de Medição","Ferramenta","Brinquedo","Roupa"]
///   Trials[1].RespostaCorreta  → "Instrumento de Medição"
/// </summary>
public class AbstractionTask : TaskBase
{
    [System.Serializable]
    public struct TrialAbstracao
    {
        [Tooltip("Par de conceitos exibido na instrução")]
        public string par;
        [Tooltip("4 categorias exibidas nos botões")]
        public string[] opcoes;
        [Tooltip("Categoria correta")]
        public string respostaCorreta;
    }

    [Header("Trials")]
    public TrialAbstracao[] trials = new TrialAbstracao[]
    {
        new TrialAbstracao
        {
            par             = "Trem e Bicicleta",
            opcoes          = new[] { "Transporte", "Animal", "Cor", "Alimento" },
            respostaCorreta = "Transporte"
        },
        new TrialAbstracao
        {
            par             = "Relógio e Régua",
            opcoes          = new[] { "Medição", "Ferramenta", "Brinquedo", "Roupa" },
            respostaCorreta = "Medição"
        }
    };

    [Header("Textos")]
    [TextArea(1, 3)]
    public string complementoInstrucao = "O que têm em comum?";

    [Header("Tempos")]
    public float pausaEntreTrials = 1f;

    private float[] scores;
    private int     trialAtual;

    protected override void Awake()
    {
        base.Awake();
        taskName      = "ABSTRAÇÃO";
        executionTime = 8f;
        scores        = new float[trials.Length];
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "TAREFA: ABSTRAÇÃO\n\n" +
                "Dois conceitos serão exibidos na instrução.\n\n" +
                "Entre as 4 opções na tela, fixe o olhar na categoria que melhor descreve o que eles têm em comum.";
    }

    // ── API para TestManager ─────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
        yield return StartCoroutine(IntroPhase());

        for (int i = 0; i < trials.Length; i++)
        {
            trialAtual = i;
            yield return ExecutarUmTrial(i);
            if (i < trials.Length - 1)
                yield return new WaitForSeconds(pausaEntreTrials);
        }
    }

    /// <summary>Média dos scores dos trials (0–1).</summary>
    public float GetScore()
    {
        if (scores.Length == 0) return 0f;
        float soma = 0f;
        foreach (var s in scores) soma += s;
        return soma / scores.Length;
    }

    // ── Trial ────────────────────────────────────────────────────────────────

    private IEnumerator ExecutarUmTrial(int idx)
    {
        var trial = trials[idx];
        uiManager.SetTaskStatus($"Abstração ({idx + 1}/{trials.Length})");

        // Instrução com o par durante a preparação
        string textoInstrucao = $"<b>{trial.par}</b>\n{complementoInstrucao}";
        uiManager.ShowInstruction(textoInstrucao);
        yield return new WaitForSeconds(preparationTime);

        // AOIs com as categorias
        uiManager.SetupAOIs(trial.opcoes, trial.respostaCorreta);
        uiManager.ShowAOIs(true);

        // Instrução permanece visível durante execução
        uiManager.ShowInstruction(textoInstrucao);

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

        bool correct = scores[idx] >= 0.5f;
        uiManager.ShowFeedback(correct ? "Correto!" : "Incorreto.", correct);
        yield return new WaitForSeconds(1.5f);
        uiManager.HideFeedback();
    }

    // ── Implementações obrigatórias de TaskBase ──────────────────────────────

    protected override void SetupTrial()
    {
        if (trials.Length > trialAtual)
            uiManager.SetupAOIs(trials[trialAtual].opcoes, trials[trialAtual].respostaCorreta);
    }

    protected override float  CalculateScore()    => scores.Length > trialAtual ? scores[trialAtual] : 0f;
    protected override string GetFeatureName()    => "vr_abs";
    protected override string GetInstructionText() =>
        trials.Length > trialAtual ? $"{trials[trialAtual].par}\n{complementoInstrucao}" : "";
}
