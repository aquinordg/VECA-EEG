using UnityEngine;

[CreateAssetMenu(menuName = "VECA-EEG/Localization Config", fileName = "LocalizationConfig")]
public class LocalizationConfig : ScriptableObject
{
    [Header("UI — Button Labels")]
    public string buttonStart   = "INICIAR TESTE";
    public string buttonGotIt   = "ENTENDIDO";
    public string buttonRestart = "NOVA AVALIAÇÃO";

    [Header("UI — Navigation")]
    public string gotItHint = "Para começar, olhe para o botão ENTENDIDO\nno canto da tela.";

    [Header("UI — Start Screen")]
    [TextArea(4, 10)]
    public string introMessage =
        "Bem-vindo à Avaliação Cognitiva VECA-EEG.\n\n" +
        "Este teste avalia funções cognitivas como memória, atenção, abstração, cálculo e execução de comandos.\n\n" +
        "Em cada etapa, leia a instrução com atenção e fixe o olhar na resposta correta entre as opções apresentadas na tela.\n\n" +
        "Quando estiver pronto para começar, pressione <b>INICIAR TESTE</b>.";

    [Header("Session")]
    public string statusCompleted = "Concluído";
    [TextArea(1, 2)]
    public string prepareMessage = "\n\nPrepare-se para começar.";
    [TextArea(2, 4)]
    public string completionMessage =
        "Avaliação concluída!\n\nObrigado pela participação.\nResultados salvos.";

    [Header("Feedback")]
    [Tooltip("Use {0} where the percentage value should appear")]
    public string feedbackTemplate = "{0}% do tempo na resposta correta";

    [Header("Task Names")]
    public string taskMemory      = "MEMÓRIA";
    public string taskAttention   = "ATENÇÃO";
    public string taskAbstraction = "ABSTRAÇÃO";
    public string taskCalculation = "CÁLCULO";
    public string taskExecution   = "EXECUÇÃO";
    public string taskRecall      = "MEMÓRIA TARDIA";

    [Header("Task Descriptions")]
    [TextArea(4, 10)]
    public string descMemory =
        "<b>TAREFA:</b> MEMÓRIA\n\n" +
        "Você verá 3 imagens, uma de cada vez, por alguns segundos.\n" +
        "Após um breve intervalo, a mesma imagem aparecerá misturada\n" +
        "com outras 3 opções — fixe o olhar nela.\n\n" +
        "<b>Exemplo:</b> se você viu um Leão, olhe para o Leão quando ele\n" +
        "aparecer entre as 4 opções.\n\n" +
        "Esta tarefa tem 3 rodadas.";

    [TextArea(4, 10)]
    public string descAttention =
        "<b>TAREFA:</b> ATENÇÃO\n\n" +
        "Uma instrução será exibida na tela.\n" +
        "Leia com atenção e fixe o olhar no item correto entre\n" +
        "as 4 opções apresentadas.\n\n" +
        "<b>Exemplo:</b> se a instrução diz \"Olhe para o brinquedo\",\n" +
        "olhe para a imagem do brinquedo.\n\n" +
        "Esta tarefa tem 1 rodada.";

    [TextArea(4, 10)]
    public string descAbstraction =
        "<b>TAREFA:</b> ABSTRAÇÃO\n\n" +
        "Dois conceitos serão exibidos na instrução.\n" +
        "Entre as 4 opções, fixe o olhar na categoria que melhor\n" +
        "descreve o que eles têm em comum.\n\n" +
        "<b>Exemplo:</b> \"Casa e Ponte\" → olhe para \"Construções\".\n\n" +
        "Esta tarefa tem 2 rodadas.";

    [TextArea(4, 10)]
    public string descCalculation =
        "<b>TAREFA:</b> CÁLCULO\n\n" +
        "Uma conta de subtração será exibida na instrução.\n" +
        "Entre as 4 opções na tela, fixe o olhar na resposta correta.\n\n" +
        "<b>Exemplo:</b> \"10 − 8 =\" → olhe para o número 2.\n\n" +
        "Esta tarefa tem 3 rodadas.";

    [TextArea(4, 10)]
    public string descExecution =
        "<b>TAREFA:</b> EXECUÇÃO\n\n" +
        "Um comando será exibido na instrução.\n" +
        "Leia com atenção e fixe o olhar na opção correta entre\n" +
        "as 4 apresentadas na tela.\n\n" +
        "<b>Exemplo:</b> \"Olhe para o número que vem depois do 2:\n" +
        "1 - 2 - ?\" → olhe para o número 3.";

    [TextArea(4, 10)]
    public string descRecall =
        "<b>TAREFA:</b> MEMÓRIA TARDIA\n\n" +
        "Você se lembra das imagens que viu no início do teste?\n\n" +
        "Cada rodada apresentará 4 opções — fixe o olhar na imagem\n" +
        "que foi mostrada lá no começo.\n\n" +
        "<b>Exemplo:</b> se você memorizou um Leão, fixe no Leão quando\n" +
        "ele aparecer entre as opções.\n\n" +
        "Esta tarefa tem 3 rodadas e vai mostrar, em sequência,\n" +
        "a primeira, segunda e terceira imagens do início.";

    [Header("Memory Task — Prompts")]
    public string memoryEncodePrompt = "Memorize esta imagem";
    public string memoryRecallPrompt = "\n\nFixe o olhar na resposta correta.";

    [Header("Memory Task — Text Mode Labels")]
    public string[] memoryTargetLabels     = { "Leão", "Rinoceronte", "Camelo" };
    public string[] memoryDistractorLabels = { "Elefante", "Zebra", "Girafa", "Tigre", "Lobo" };

    [Header("Attention Task")]
    [TextArea(1, 3)]
    public string   attentionInstrucao       = "\n\n\nOlhe para o ANIMAL SELVAGEM.";
    public string[] attentionOpcoes          = { "Leão", "Vaca", "Porco", "Galinha" };
    public string   attentionRespostaCorreta = "Leão";

    [Header("Abstraction Task")]
    public string abstractionComplement = "O que têm em comum?";
    public AbstractionTrialConfig[] abstractionTrials =
    {
        new AbstractionTrialConfig
        {
            par             = "Trem e Bicicleta",
            opcoes          = new[] { "Transporte", "Animal", "Cor", "Alimento" },
            respostaCorreta = "Transporte"
        },
        new AbstractionTrialConfig
        {
            par             = "Relógio e Régua",
            opcoes          = new[] { "Medição", "Ferramenta", "Brinquedo", "Roupa" },
            respostaCorreta = "Medição"
        }
    };

    [Header("Calculation Task")]
    public string calculationPrompt = "Olhe para a resposta correta.";

    [Header("Execution Task")]
    [TextArea(1, 3)]
    public string   executionInstrucao       = "\n\nOlhe para o número que vem depois do 7 na sequência: 8 - 7 - ?";
    public string[] executionOpcoes          = { "6", "4", "7", "5" };
    public string   executionRespostaCorreta = "6";

    [Header("Recall Task")]
    public string[] recallOrdinals       = { "primeira", "segunda", "terceira" };
    [TextArea(1, 3)]
    public string recallPromptTemplate   =
        "\nVocê se lembra da <b>{0}</b> imagem mostrada no início?\nAgora tente lembrar e olhe para ela.";
}

[System.Serializable]
public class AbstractionTrialConfig
{
    public string   par;
    public string[] opcoes;
    public string   respostaCorreta;
}
