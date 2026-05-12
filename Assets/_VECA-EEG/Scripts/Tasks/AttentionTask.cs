using UnityEngine;

/// <summary>
/// Tarefa de Atenção Visual — feature: vr_att
///
/// Mostra 4 opções; o participante deve fixar no estímulo alvo.
/// A instrução permanece visível durante a execução para lembrar o alvo.
///
/// CONFIGURAÇÃO NO INSPECTOR:
///   Instrucao       → texto exibido (ex: "Olhe para o animal selvagem")
///   Opcoes          → 4 rótulos (ex: "Leão", "Vaca", "Porco", "Galinha")
///   RespostaCorreta → um dos rótulos acima (ex: "Leão")
///   executionTime   → 5 s (padrão artigo)
/// </summary>
public class AttentionTask : TaskBase
{
    [Header("Conteúdo")]
    [Tooltip("Instrução exibida durante o trial")]
    public string instrucao = "Olhe para o animal selvagem.";

    [Tooltip("4 opções exibidas nos botões")]
    public string[] opcoes = { "Leão", "Vaca", "Porco", "Galinha" };

    [Tooltip("Qual das opções é a resposta correta")]
    public string respostaCorreta = "Leão";

    protected override void Awake()
    {
        base.Awake();
        taskName                   = "Atenção";
        executionTime              = 5f;
        mostrarInstrucaoNaExecucao = true;
    }

    protected override void SetupTrial()
    {
        uiManager.SetupAOIs(opcoes, respostaCorreta);
        uiManager.ShowAOIs(true);
    }

    protected override float  CalculateScore()    => eyeTracker.GetCorrectAOIPercentage();
    protected override string GetFeatureName()    => "vr_att";
    protected override string GetInstructionText() => instrucao;
}
