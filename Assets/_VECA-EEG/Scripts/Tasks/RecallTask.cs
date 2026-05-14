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

    [Header("Textos")]
    public string instrucaoPreparacao = "Você se lembra das imagens do início?\nAgora tente recordá-las.";
    public string instrucaoExecucao   = "Fixe o olhar na imagem que foi mostrada.";

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
                "TAREFA: MEMÓRIA TARDIA\n\n" +
                "Você se lembra das imagens que viu no início do teste?\n\n" +
                "Para cada pergunta, fixe o olhar na imagem que foi apresentada lá atrás, entre as 4 opções na tela.";
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
        uiManager.SetTaskStatus($"Recall ({idx + 1}/3)");

        uiManager.ShowInstruction(instrucaoPreparacao);
        yield return new WaitForSeconds(preparationTime);

        if (!ConfigurarAOIs(idx))
        {
            scores[idx] = 0f;
            yield break;
        }

        string[] ordinals = { "primeira", "segunda", "terceira" };
        string ordinal = idx < ordinals.Length ? ordinals[idx] : $"{idx + 1}ª";

        uiManager.ShowAOIs(true);
        uiManager.ShowInstruction($"\n\nFixe o olhar na imagem que foi mostrada pela <b>{ordinal}</b> vez.");

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
    protected override string GetInstructionText() => instrucaoExecucao;
}
