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

    private float[]           scores;
    private System.DateTime[] _trialStartTimes;
    private System.DateTime[] _trialEndTimes;
    private int               trialAtual;

    protected override void Awake()
    {
        base.Awake();
        taskName         = "ABSTRAÇÃO";
        executionTime    = 8f;
        scores           = new float[trials.Length];
        _trialStartTimes = new System.DateTime[trials.Length];
        _trialEndTimes   = new System.DateTime[trials.Length];
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TAREFA:</b> ABSTRAÇÃO\n\n" +
                "Dois conceitos serão exibidos na instrução.\n" +
                "Entre as 4 opções, fixe o olhar na categoria que melhor\n" +
                "descreve o que eles têm em comum.\n\n" +
                "<b>Exemplo:</b> \"Casa e Ponte\" → olhe para \"Construções\".\n\n" +
                "Esta tarefa tem 2 rodadas.";
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

    public (System.DateTime start, System.DateTime end) GetTrialTimes(int idx) =>
        (_trialStartTimes[idx], _trialEndTimes[idx]);

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
        var    trial      = trials[idx];
        var    locTrial   = Loc?.abstractionTrials?.Length > idx ? Loc.abstractionTrials[idx] : null;
        string par        = locTrial?.par             ?? trial.par;
        string[] opcoes   = locTrial?.opcoes          ?? trial.opcoes;
        string resposta   = locTrial?.respostaCorreta ?? trial.respostaCorreta;
        string complement = Loc?.abstractionComplement ?? complementoInstrucao;

        uiManager.SetTaskStatus($"{Loc?.taskAbstraction ?? taskName} ({idx + 1}/{trials.Length})");

        string textoInstrucao = $"<b>{par}</b>\n{complement}";
        uiManager.ShowInstruction(textoInstrucao);
        yield return new WaitForSeconds(preparationTime);

        uiManager.SetupAOIs(opcoes, resposta);
        uiManager.ShowAOIs(true);

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
        _trialStartTimes[idx] = eyeTracker.RecordingStartTime;
        _trialEndTimes[idx]   = eyeTracker.RecordingEndTime;
        uiManager.HideInstruction();
        scores[idx] = eyeTracker.GetCorrectAOIPercentage();

        uiManager.ShowAOIs(false);

        bool correct = scores[idx] >= 0.5f;
        uiManager.ShowFeedback(FormatFeedback(scores[idx]), correct);
        yield return new WaitForSeconds(1.5f);
        uiManager.HideFeedback();
    }

    // ── Implementações obrigatórias de TaskBase ──────────────────────────────

    protected override void SetupTrial()
    {
        if (trials.Length <= trialAtual) return;
        var    locTrial = Loc?.abstractionTrials?.Length > trialAtual ? Loc.abstractionTrials[trialAtual] : null;
        string[] opcoes = locTrial?.opcoes          ?? trials[trialAtual].opcoes;
        string   resp   = locTrial?.respostaCorreta ?? trials[trialAtual].respostaCorreta;
        uiManager.SetupAOIs(opcoes, resp);
    }

    protected override string GetTaskName()    => Loc?.taskAbstraction ?? taskName;
    protected override string GetDescription() => Loc?.descAbstraction ?? taskDescription;
    protected override float  CalculateScore() => scores.Length > trialAtual ? scores[trialAtual] : 0f;
    protected override string GetFeatureName() => "vr_abs";
    protected override string GetInstructionText()
    {
        if (trials.Length <= trialAtual) return "";
        var    locTrial   = Loc?.abstractionTrials?.Length > trialAtual ? Loc.abstractionTrials[trialAtual] : null;
        string par        = locTrial?.par ?? trials[trialAtual].par;
        string complement = Loc?.abstractionComplement ?? complementoInstrucao;
        return $"{par}\n{complement}";
    }
}
