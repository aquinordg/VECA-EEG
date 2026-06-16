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
    public string instrucao = "\n\nOlhe para o número que vem depois do 7 na sequência: 8 - 7 - ?";

    [Tooltip("4 opções exibidas nos botões")]
    public string[] opcoes = { "6", "4", "7", "5" };

    [Tooltip("Qual das opções é a resposta correta")]
    public string respostaCorreta = "6";

    protected override void Awake()
    {
        base.Awake();
        taskName                       = "EXECUÇÃO";
        executionTime                  = 8f;
        showInstructionDuringExecution = true;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TAREFA:</b> EXECUÇÃO\n\n" +
                "Um comando será exibido na instrução.\n" +
                "Leia com atenção e fixe o olhar na opção correta entre\n" +
                "as 4 apresentadas na tela.\n\n" +
                "<b>Exemplo:</b> \"Olhe para o número que vem depois do 2:\n" +
                "1 - 2 - ?\" → olhe para o número 3.";
    }

    protected override void SetupTrial()
    {
        uiManager.SetupAOIs(Loc?.executionOpcoes ?? opcoes, Loc?.executionRespostaCorreta ?? respostaCorreta);
        uiManager.ShowAOIs(true);
    }

    protected override string GetTaskName()        => Loc?.taskExecution    ?? taskName;
    protected override string GetDescription()     => L(Loc?.descExecution, taskDescription);
    protected override float  CalculateScore()     => eyeTracker.GetCorrectAOIPercentage();
    protected override string GetFeatureName()     => "vr_exec";
    protected override string GetInstructionText() => Loc?.executionInstrucao ?? instrucao;
}
