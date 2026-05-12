using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Orquestrador da avaliação cognitiva VECA.
///
/// SEQUÊNCIA:
///   1. Memória    → vr_mem8, vr_mem9, vr_mem10
///   2. Atenção    → vr_att
///   3. Abstração  → vr_abs
///   4-6. Cálculo  → vr_calc4, vr_calc5, vr_calc6
///   7. Execução   → vr_exec
///   8. Recall     → vr_recall
///
/// CSV salvo em: Application.persistentDataPath/Results/VECA_<ID>_<timestamp>.csv
/// </summary>
public class TestManager : MonoBehaviour
{
    public static TestManager Instance { get; private set; }

    [Header("Referências de UI")]
    public UIManager  uiManager;
    public EyeTracker eyeTracker;

    [Header("Tarefas")]
    public MemoryTask      memoryTask;
    public AttentionTask   attentionTask;
    public AbstractionTask abstractionTask;
    public CalculationTask calculationTask;
    public ExecutionTask   executionTask;
    public RecallTask      recallTask;

    [Header("Participante")]
    [Tooltip("Código do participante (aparece no nome do CSV)")]
    public string participantID = "P001";

    [Header("Opções")]
    public bool  autoStart         = false;
    public float pausaEntreTarefas = 1.5f;

    private readonly Dictionary<string, float> features = new();

    private static readonly string[] OrdemFeatures =
    {
        "vr_mem8", "vr_mem9", "vr_mem10",
        "vr_att",
        "vr_abs",
        "vr_calc4", "vr_calc5", "vr_calc6",
        "vr_exec",
        "vr_recall"
    };

    public bool TestRunning { get; private set; }

    // ── Unity ────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (autoStart) StartTest();
    }

    // ── API Pública ──────────────────────────────────────────────────────────

    public void StartTest()
    {
        if (!TestRunning) StartCoroutine(SequenciaCompleta());
    }

    // ── Sequência Completa ───────────────────────────────────────────────────

    private IEnumerator SequenciaCompleta()
    {
        TestRunning = true;
        features.Clear();

        uiManager.ShowInstruction("Prepare-se para começar.", 3f);
        yield return new WaitForSeconds(3f);

        // ── 1. MEMÓRIA ────────────────────────────────────────────────────────
        yield return StartCoroutine(ExecutarMemoria());
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 2. ATENÇÃO ────────────────────────────────────────────────────────
        yield return StartCoroutine(ExecutarTarefaSimples(attentionTask, "vr_att"));
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 3. ABSTRAÇÃO ──────────────────────────────────────────────────────
        yield return StartCoroutine(ExecutarAbstracao());
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 4-6. CÁLCULO ──────────────────────────────────────────────────────
        yield return StartCoroutine(ExecutarCalculo());
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 7. EXECUÇÃO ───────────────────────────────────────────────────────
        yield return StartCoroutine(ExecutarTarefaSimples(executionTask, "vr_exec"));
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 8. RECALL TARDIO ──────────────────────────────────────────────────
        yield return StartCoroutine(ExecutarRecall());

        // ── ENCERRAMENTO ──────────────────────────────────────────────────────
        SalvarCSV();
        uiManager.SetTaskStatus("Concluído");
        uiManager.ShowInstruction($"Avaliação concluída!\n\nObrigado, {participantID}.\nResultados salvos.", 0f);
        TestRunning = false;
    }

    // ── Coordenadores por tarefa ─────────────────────────────────────────────

    private IEnumerator ExecutarMemoria()
    {
        if (memoryTask == null) { Debug.LogWarning("[TestManager] MemoryTask não atribuído."); yield break; }

        yield return StartCoroutine(memoryTask.RunAllTrials());

        features["vr_mem8"]  = memoryTask.GetTrialScore(0);
        features["vr_mem9"]  = memoryTask.GetTrialScore(1);
        features["vr_mem10"] = memoryTask.GetTrialScore(2);

        Debug.Log($"[Memória] {features["vr_mem8"]:F3} / {features["vr_mem9"]:F3} / {features["vr_mem10"]:F3}");
    }

    /// <summary>Executa tarefas de um único trial (Atenção, Execução).</summary>
    private IEnumerator ExecutarTarefaSimples(TaskBase tarefa, string nomeFeature)
    {
        if (tarefa == null)
        {
            Debug.LogWarning($"[TestManager] {nomeFeature} não atribuído — pulando.");
            features[nomeFeature] = -1f;
            yield break;
        }

        tarefa.StartTask();
        yield return null;                                        // aguarda IsRunning = true
        yield return new WaitUntil(() => !tarefa.IsRunning);

        features[nomeFeature] = tarefa.LastScore;
        Debug.Log($"[{nomeFeature}] {features[nomeFeature]:F3}");
    }

    private IEnumerator ExecutarAbstracao()
    {
        if (abstractionTask == null) { Debug.LogWarning("[TestManager] AbstractionTask não atribuído."); features["vr_abs"] = -1f; yield break; }

        yield return StartCoroutine(abstractionTask.RunAllTrials());

        features["vr_abs"] = abstractionTask.GetScore();
        Debug.Log($"[Abstração] {features["vr_abs"]:F3}");
    }

    private IEnumerator ExecutarCalculo()
    {
        if (calculationTask == null) { Debug.LogWarning("[TestManager] CalculationTask não atribuído."); yield break; }

        yield return StartCoroutine(calculationTask.RunAllTrials());

        features["vr_calc4"] = calculationTask.GetTrialScore(0);
        features["vr_calc5"] = calculationTask.GetTrialScore(1);
        features["vr_calc6"] = calculationTask.GetTrialScore(2);

        Debug.Log($"[Cálculo] {features["vr_calc4"]:F3} / {features["vr_calc5"]:F3} / {features["vr_calc6"]:F3}");
    }

    private IEnumerator ExecutarRecall()
    {
        if (recallTask == null) { Debug.LogWarning("[TestManager] RecallTask não atribuído."); features["vr_recall"] = -1f; yield break; }

        yield return StartCoroutine(recallTask.RunAllTrials());

        features["vr_recall"] = recallTask.GetScore();
        Debug.Log($"[Recall] {features["vr_recall"]:F3}");
    }

    // ── Exportação CSV ───────────────────────────────────────────────────────

    private void SalvarCSV()
    {
        string pasta    = Path.Combine(Application.persistentDataPath, "Results");
        Directory.CreateDirectory(pasta);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string arquivo   = Path.Combine(pasta, $"VECA_{participantID}_{timestamp}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("participant_id,timestamp,feature,value");

        foreach (string feat in OrdemFeatures)
        {
            float val = features.TryGetValue(feat, out float v) ? v : -1f;
            sb.AppendLine($"{participantID},{timestamp},{feat},{val:F4}");
        }

        File.WriteAllText(arquivo, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[TestManager] CSV salvo em: {arquivo}");
    }
}
