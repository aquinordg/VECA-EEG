using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tarefa de Recall Tardio — feature: vr_recall
///
/// Recall das 3 imagens memoradas no início, após todas as outras tarefas.
/// Reutiliza os estímulos (sprites ou rótulos) da MemoryTask.
/// Score final = média dos 3 trials.
///
/// CONFIGURAÇÃO NO INSPECTOR:
///   MemoryTask → referência ao componente MemoryTask da cena
///                (os mesmos sprites/labels são usados automaticamente)
/// </summary>
public class RecallTask : TaskBase
{
    [Header("Referência")]
    [Tooltip("Componente MemoryTask da cena — reutiliza seus estímulos")]
    public MemoryTask memoryTask;

    private static readonly string[] Ordinais = { "primeira", "segunda", "terceira" };

    [Header("Tempos")]
    public float pausaEntreTrials = 1f;

    private float[] scores   = new float[3];
    private int     trialAtual;

    protected override void Awake()
    {
        base.Awake();
        taskName      = "MEMÓRIA TARDIA";
        executionTime = 8f;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TAREFA:</b> MEMÓRIA TARDIA\n\n" +
                "Você se lembra das imagens que viu no início do teste?\n\n" +
                "Cada rodada apresentará 4 opções — fixe o olhar na imagem\n" +
                "que foi mostrada lá no começo.\n\n" +
                "<b>Exemplo:</b> se você memorizou um Leão, fixe no Leão quando\n" +
                "ele aparecer entre as opções.\n\n" +
                "Esta tarefa tem 3 rodadas e vai mostrar, em sequência,\n" +
                "a primeira, segunda e terceira imagens do início.";
    }

    // ── API para TestManager ─────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
        yield return StartCoroutine(IntroPhase());

        for (int i = 0; i < 3; i++)
        {
            trialAtual = i;
            yield return ExecutarUmTrial(i);
            if (i < 2)
                yield return new WaitForSeconds(pausaEntreTrials);
        }
    }

    /// <summary>Média dos 3 trials de recall (0–1).</summary>
    public float GetScore()
    {
        float soma = 0f;
        foreach (var s in scores) soma += s;
        return soma / scores.Length;
    }

    // ── Trial ────────────────────────────────────────────────────────────────

    private IEnumerator ExecutarUmTrial(int idx)
    {
        uiManager.SetTaskStatus($"MEMÓRIA TARDIA ({idx + 1}/3)");

        string ordinal   = idx < Ordinais.Length ? Ordinais[idx] : $"{idx + 1}ª";
        string instrucao = $"\nVocê se lembra da <b>{ordinal}</b> imagem mostrada no início?\nAgora tente lembrar e olhe para ela.";

        uiManager.ShowInstruction(instrucao);
        yield return new WaitForSeconds(preparationTime);

        if (!ConfigurarAOIs(idx))
        {
            scores[idx] = 0f;
            yield break;
        }

        uiManager.ShowAOIs(true);
        uiManager.ShowInstruction(instrucao);

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
        uiManager.ShowFeedback($"{scores[idx] * 100f:F0}% do tempo na resposta correta", correct);
        yield return new WaitForSeconds(1.5f);
        uiManager.HideFeedback();
    }

    // ── Setup das AOIs (reutiliza lógica da MemoryTask) ──────────────────────

    private bool ConfigurarAOIs(int idx)
    {
        if (memoryTask == null)
        {
            Debug.LogError("[RecallTask] MemoryTask não atribuído — trial abortado.");
            return false;
        }

        Sprite   spriteAlvo  = memoryTask.GetTargetSprite(idx);
        Sprite[] distractors = memoryTask.GetDistractorSprites();
        bool     usarSprites = spriteAlvo != null;

        if (usarSprites)
        {
            Sprite alvo = spriteAlvo;
            var pool    = new List<Sprite>();
            if (distractors != null) pool.AddRange(distractors);
            for (int k = 0; k < 3; k++)
            {
                Sprite s = memoryTask.GetTargetSprite(k);
                if (s != alvo && s != null && !pool.Contains(s)) pool.Add(s);
            }

            var opcoes = new List<Sprite> { alvo };
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int r = Random.Range(0, pool.Count);
                opcoes.Add(pool[r]);
                pool.RemoveAt(r);
            }
            uiManager.SetupAOIs(opcoes.ToArray(), alvo);
        }
        else
        {
            string alvo = memoryTask.GetTargetLabel(idx);
            var pool    = new List<string>(memoryTask.distractorLabels);
            for (int k = 0; k < 3; k++) pool.Remove(memoryTask.GetTargetLabel(k));
            for (int k = 0; k < 3; k++)
            {
                string lbl = memoryTask.GetTargetLabel(k);
                if (lbl != alvo && !pool.Contains(lbl)) pool.Add(lbl);
            }

            var opcoes = new List<string> { alvo };
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int r = Random.Range(0, pool.Count);
                opcoes.Add(pool[r]);
                pool.RemoveAt(r);
            }
            uiManager.SetupAOIs(opcoes.ToArray(), alvo);
        }

        return true;
    }

    // ── Implementações obrigatórias de TaskBase ──────────────────────────────

    protected override void   SetupTrial()       => _ = ConfigurarAOIs(trialAtual);
    protected override float  CalculateScore()   => scores.Length > trialAtual ? scores[trialAtual] : 0f;
    protected override string GetFeatureName()   => "vr_recall";
    protected override string GetInstructionText() =>
        $"Você se lembra da <b>{(trialAtual < Ordinais.Length ? Ordinais[trialAtual] : $"{trialAtual + 1}ª")}</b> imagem mostrada no início?\nAgora tente lembrar e olhe para ela.";
}
