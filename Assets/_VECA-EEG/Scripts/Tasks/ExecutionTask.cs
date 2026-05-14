using UnityEngine;

/// <summary>
/// Tarefa de Execução — feature: vr_exec
///
/// O participante recebe um comando e deve fixar na opção correta.
/// Adaptação VR do teste de execução de comandos do MOCA.
///
/// CONFIGURAÇÃO NO INSPECTOR:
///   Instrucao       → comando (ex: "Olhe para o objeto que está acima dos outros")
///   Opcoes          → 4 rótulos nos botões
///   RespostaCorreta → qual das opções é a correta
///   executionTime   → 8 s (padrão artigo)
/// </summary>
public class ExecutionTask : TaskBase
{
    [Header("Conteúdo")]
    [Tooltip("Comando exibido durante o trial")]
    [TextArea(2, 4)]
    public string instrucao = "Olhe para o número que vem depois do 2 na sequência: 1 - 2 - ?";

    [Tooltip("4 opções exibidas nos botões")]
    public string[] opcoes = { "3", "4", "1", "5" };

    [Tooltip("Qual das opções é a resposta correta")]
    public string respostaCorreta = "3";

    protected override void Awake()
    {
        base.Awake();
        taskName                       = "EXECUÇÃO";
        executionTime                  = 8f;
        showInstructionDuringExecution = true;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "TAREFA: EXECUÇÃO\n\n" +
                "Um comando será exibido na instrução.\n\n" +
                "Leia com atenção e fixe o olhar na opção correta entre as 4 apresentadas na tela.";
    }

    protected override void SetupTrial()
    {
        uiManager.SetupAOIs(opcoes, respostaCorreta);
        uiManager.ShowAOIs(true);
    }

    protected override float  CalculateScore()    => eyeTracker.GetCorrectAOIPercentage();
    protected override string GetFeatureName()    => "vr_exec";
    protected override string GetInstructionText() => instrucao;
}
