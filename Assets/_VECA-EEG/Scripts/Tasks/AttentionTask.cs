using System.Collections;
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
///   executionTime     → 8 s
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
    public string instrucao = "\n\n\nOlhe para o ANIMAL SELVAGEM.";

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
        executionTime                   = 8f;
        showInstructionDuringExecution  = true;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TAREFA:</b> ATENÇÃO\n\n" +
                "Uma instrução será exibida na tela.\n" +
                "Leia com atenção e fixe o olhar no item correto entre\n" +
                "as 4 opções apresentadas.\n\n" +
                "<b>Exemplo:</b> se a instrução diz \"Olhe para o brinquedo\",\n" +
                "olhe para a imagem do brinquedo.\n\n" +
                "Esta tarefa tem 1 rodada.";
    }

    protected override IEnumerator PreparationPhase()
    {
        uiManager.ShowInstruction(GetInstructionText());
        float elapsed = 0f;
        while (elapsed < preparationTime)
        {
            elapsed += Time.deltaTime;
            uiManager.UpdateTimer(preparationTime - elapsed);
            yield return null;
        }
        // Instrução permanece visível quando as AOIs aparecem
    }

    protected override void SetupTrial()
    {
        bool usarSprites = opcaoSprites != null && opcaoSprites.Length >= 2 && spriteCorreto != null;

        if (usarSprites)
            uiManager.SetupAOIs(opcaoSprites, spriteCorreto);
        else
            uiManager.SetupAOIs(Loc?.attentionOpcoes ?? opcoes, Loc?.attentionRespostaCorreta ?? respostaCorreta);

        uiManager.ShowAOIs(true);
    }

    protected override string GetTaskName()        => Loc?.taskAttention   ?? taskName;
    protected override string GetDescription()     => L(Loc?.descAttention, taskDescription);
    protected override float  CalculateScore()     => eyeTracker.GetCorrectAOIPercentage();
    protected override string GetFeatureName()     => "vr_att";
    protected override string GetInstructionText() => Loc?.attentionInstrucao ?? instrucao;
}
