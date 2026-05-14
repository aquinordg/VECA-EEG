using UnityEngine;

/// <summary>
/// Tarefa de Atenção Visual — feature: vr_att
///
/// Mostra 4 opções; o participante deve fixar no estímulo alvo.
/// A instrução permanece visível durante a execução para lembrar o alvo.
///
/// CONFIGURAÇÃO NO INSPECTOR (modo imagem):
///   OpcaoSprites      → 4 sprites (ex: Leão, Vaca, Porco, Galinha)
///   SpriteCorreto     → sprite da resposta correta (ex: Leão)
///   Instrucao         → texto exibido durante o trial
///   executionTime     → 5 s (padrão artigo)
///
/// CONFIGURAÇÃO NO INSPECTOR (modo texto — fallback):
///   Opcoes            → 4 rótulos
///   RespostaCorreta   → um dos rótulos acima
/// </summary>
public class AttentionTask : TaskBase
{
    [Header("Conteúdo")]
    [Tooltip("Instrução exibida durante o trial")]
    [TextArea(2, 4)]
    public string instrucao = "\n\n\nOlhe para o animal selvagem.";

    [Header("Modo Imagem (prioridade)")]
    [Tooltip("4 sprites exibidos nos botões")]
    public Sprite[] opcaoSprites;

    [Tooltip("Sprite da resposta correta (deve ser um dos opcaoSprites)")]
    public Sprite spriteCorreto;

    [Header("Modo Texto (fallback)")]
    [Tooltip("4 rótulos exibidos nos botões")]
    public string[] opcoes = { "Leão", "Vaca", "Porco", "Galinha" };

    [Tooltip("Qual das opções é a resposta correta")]
    public string respostaCorreta = "Leão";

    protected override void Awake()
    {
        base.Awake();
        taskName                        = "ATENÇÃO";
        executionTime                   = 5f;
        showInstructionDuringExecution  = true;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "TAREFA: ATENÇÃO\n\n" +
                "Uma instrução será exibida.\n\n" +
                "Entre as 4 opções na tela, fixe o olhar no item correto.";
    }

    protected override void SetupTrial()
    {
        bool usarSprites = opcaoSprites != null && opcaoSprites.Length >= 2 && spriteCorreto != null;

        if (usarSprites)
            uiManager.SetupAOIs(opcaoSprites, spriteCorreto);
        else
            uiManager.SetupAOIs(opcoes, respostaCorreta);

        uiManager.ShowAOIs(true);
    }

    protected override float  CalculateScore()    => eyeTracker.GetCorrectAOIPercentage();
    protected override string GetFeatureName()    => "vr_att";
    protected override string GetInstructionText() => instrucao;
}
