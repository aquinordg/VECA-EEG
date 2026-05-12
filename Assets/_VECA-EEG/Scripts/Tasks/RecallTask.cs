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
        taskName      = "Recall";
        executionTime = 8f;
    }

    // ── API para TestManager ─────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
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

        ConfigurarAOIs(idx);
        uiManager.ShowAOIs(true);
        uiManager.ShowInstruction(instrucaoExecucao);

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
    }

    // ── Setup das AOIs (reutiliza lógica da MemoryTask) ──────────────────────

    private void ConfigurarAOIs(int idx)
    {
        if (memoryTask == null) { Debug.LogWarning("[RecallTask] MemoryTask não atribuído."); return; }

        bool usarSprites = memoryTask.targetSprites  != null && idx < memoryTask.targetSprites.Length
                        && memoryTask.distractorSprites != null && memoryTask.distractorSprites.Length >= 3;

        if (usarSprites)
        {
            Sprite alvo = memoryTask.targetSprites[idx];
            var pool    = new List<Sprite>(memoryTask.distractorSprites);
            foreach (var s in memoryTask.targetSprites)
                if (s != alvo && !pool.Contains(s)) pool.Add(s);

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
            string alvo = memoryTask.targetLabels[idx];
            var pool    = new List<string>(memoryTask.distractorLabels);
            foreach (var lbl in memoryTask.targetLabels) pool.Remove(lbl);
            foreach (var lbl in memoryTask.targetLabels)
                if (lbl != alvo && !pool.Contains(lbl)) pool.Add(lbl);

            var opcoes = new List<string> { alvo };
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int r = Random.Range(0, pool.Count);
                opcoes.Add(pool[r]);
                pool.RemoveAt(r);
            }
            uiManager.SetupAOIs(opcoes.ToArray(), alvo);
        }
    }

    // ── Implementações obrigatórias de TaskBase ──────────────────────────────

    protected override void   SetupTrial()       => ConfigurarAOIs(trialAtual);
    protected override float  CalculateScore()   => scores.Length > trialAtual ? scores[trialAtual] : 0f;
    protected override string GetFeatureName()   => "vr_recall";
    protected override string GetInstructionText() => instrucaoExecucao;
}
