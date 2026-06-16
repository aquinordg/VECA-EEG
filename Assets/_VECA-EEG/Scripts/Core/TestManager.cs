using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
/// CSV salvo em: &lt;projeto&gt;/Results/VECA_&lt;ID&gt;_&lt;timestamp&gt;.csv
/// </summary>
[DefaultExecutionOrder(-10)]
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
    [Tooltip("Preenchido automaticamente com ID aleatório ao iniciar o teste")]
    public string participantID = "";

    [Header("Localização")]
    [Tooltip("Asset com todos os textos de UI. Null = PT-BR padrão.")]
    public LocalizationConfig locConfig;

    [Header("Opções")]
    public bool  autoStart         = false;
    public float pausaEntreTarefas = 1.5f;

    private struct TrialRecord
    {
        public string          feature;
        public float           value;
        public System.DateTime trialStart;
        public System.DateTime trialEnd;
    }
    private readonly List<TrialRecord> records = new();

    public bool TestRunning { get; private set; }

    // ── Unity ────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        participantID = GerarParticipantID();
        if (uiManager != null) uiManager.SetParticipantID(participantID);
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
        records.Clear();

        LSLMarkerStream.Instance?.SendMarker($"session_start,{participantID}");

        uiManager.HideStartScreen();
        uiManager.HideParticipantID();
        uiManager.ShowInstruction(locConfig?.prepareMessage ?? "\n\nPrepare-se para começar.", 3f);
        yield return new WaitForSeconds(3f);

        // ── 1. MEMÓRIA ────────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Memoria";
        yield return StartCoroutine(ExecutarMemoria());
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 2. ATENÇÃO ────────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Atencao";
        yield return StartCoroutine(ExecutarTarefaSimples(attentionTask, "vr_att"));
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 3. ABSTRAÇÃO ──────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Abstracao";
        yield return StartCoroutine(ExecutarAbstracao());
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 4-6. CÁLCULO ──────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Calculo";
        yield return StartCoroutine(ExecutarCalculo());
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 7. EXECUÇÃO ───────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Execucao";
        yield return StartCoroutine(ExecutarTarefaSimples(executionTask, "vr_exec"));
        yield return new WaitForSeconds(pausaEntreTarefas);

        // ── 8. RECALL TARDIO ──────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Recall";
        yield return StartCoroutine(ExecutarRecall());

        // ── ENCERRAMENTO ──────────────────────────────────────────────────────
        LSLMarkerStream.Instance?.SendMarker($"session_end,{participantID}");
        SalvarCSV();
        uiManager.SetTaskStatus(locConfig?.statusCompleted ?? "Concluído");
        uiManager.SetParticipantID(participantID);
        uiManager.ShowInstruction(
            locConfig?.completionMessage ?? "Avaliação concluída!\n\nObrigado pela participação.\nResultados salvos.", 0f);
        TestRunning = false;

        yield return StartCoroutine(uiManager.WaitForRestart());

        participantID = GerarParticipantID();
        uiManager.SetParticipantID(participantID);
        uiManager.HideInstruction();
        uiManager.SetTaskStatus("");
        uiManager.ShowStartScreen();
    }

    // ── Coordenadores por tarefa ─────────────────────────────────────────────

    private IEnumerator ExecutarMemoria()
    {
        if (memoryTask == null) { Debug.LogWarning("[TestManager] MemoryTask não atribuído."); yield break; }

        yield return StartCoroutine(memoryTask.RunAllTrials());

        for (int i = 0; i < 3; i++)
        {
            var (s, e) = memoryTask.GetTrialTimes(i);
            records.Add(new TrialRecord { feature = $"vr_mem{8 + i}", value = memoryTask.GetTrialScore(i), trialStart = s, trialEnd = e });
        }
        Debug.Log($"[Memória] {memoryTask.GetTrialScore(0):F3} / {memoryTask.GetTrialScore(1):F3} / {memoryTask.GetTrialScore(2):F3}");
    }

    /// <summary>Executa tarefas de um único trial (Atenção, Execução).</summary>
    private IEnumerator ExecutarTarefaSimples(TaskBase tarefa, string nomeFeature)
    {
        if (tarefa == null)
        {
            Debug.LogWarning($"[TestManager] {nomeFeature} não atribuído — pulando.");
            records.Add(new TrialRecord { feature = nomeFeature, value = -1f });
            yield break;
        }

        tarefa.StartTask();
        yield return null;                                        // aguarda IsRunning = true
        yield return new WaitUntil(() => !tarefa.IsRunning);

        records.Add(new TrialRecord
        {
            feature    = nomeFeature,
            value      = tarefa.LastScore,
            trialStart = tarefa.TrialStartTime,
            trialEnd   = tarefa.TrialEndTime
        });
        Debug.Log($"[{nomeFeature}] {tarefa.LastScore:F3}");
    }

    private IEnumerator ExecutarAbstracao()
    {
        if (abstractionTask == null)
        {
            Debug.LogWarning("[TestManager] AbstractionTask não atribuído.");
            records.Add(new TrialRecord { feature = "vr_abs", value = -1f });
            yield break;
        }

        yield return StartCoroutine(abstractionTask.RunAllTrials());

        float score  = abstractionTask.GetScore();
        var   tStart = abstractionTask.GetTrialTimes(0).start;
        var   tEnd   = abstractionTask.GetTrialTimes(abstractionTask.trials.Length - 1).end;
        records.Add(new TrialRecord { feature = "vr_abs", value = score, trialStart = tStart, trialEnd = tEnd });
        Debug.Log($"[Abstração] {score:F3}");
    }

    private IEnumerator ExecutarCalculo()
    {
        if (calculationTask == null) { Debug.LogWarning("[TestManager] CalculationTask não atribuído."); yield break; }

        yield return StartCoroutine(calculationTask.RunAllTrials());

        for (int i = 0; i < 3; i++)
        {
            var (s, e) = calculationTask.GetTrialTimes(i);
            records.Add(new TrialRecord { feature = $"vr_calc{4 + i}", value = calculationTask.GetTrialScore(i), trialStart = s, trialEnd = e });
        }
        Debug.Log($"[Cálculo] {calculationTask.GetTrialScore(0):F3} / {calculationTask.GetTrialScore(1):F3} / {calculationTask.GetTrialScore(2):F3}");
    }

    private IEnumerator ExecutarRecall()
    {
        if (recallTask == null)
        {
            Debug.LogWarning("[TestManager] RecallTask não atribuído.");
            records.Add(new TrialRecord { feature = "vr_recall", value = -1f });
            yield break;
        }

        yield return StartCoroutine(recallTask.RunAllTrials());

        float score  = recallTask.GetScore();
        var   tStart = recallTask.GetTrialTimes(0).start;
        var   tEnd   = recallTask.GetTrialTimes(2).end;
        records.Add(new TrialRecord { feature = "vr_recall", value = score, trialStart = tStart, trialEnd = tEnd });
        Debug.Log($"[Recall] {score:F3}");
    }

    // ── Geração de ID ────────────────────────────────────────────────────────

    private static string GerarParticipantID()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // sem 0/O/1/I/L
        var sb = new System.Text.StringBuilder(6);
        for (int i = 0; i < 6; i++)
            sb.Append(chars[Random.Range(0, chars.Length)]);
        return sb.ToString();
    }

    // ── Exportação CSV ───────────────────────────────────────────────────────

    private void SalvarCSV()
    {
        string pasta = Path.Combine(Application.dataPath, "..", "Results");
        Directory.CreateDirectory(pasta);

        string sessionTs = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string arquivo   = Path.Combine(pasta, $"VECA_{participantID}_{sessionTs}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("participant_id,trial_start,trial_end,feature,value");

        foreach (var r in records)
        {
            string ts  = r.trialStart == default ? sessionTs : r.trialStart.ToString("yyyyMMdd_HHmmss.fff");
            string te  = r.trialEnd   == default ? sessionTs : r.trialEnd.ToString("yyyyMMdd_HHmmss.fff");
            sb.AppendLine($"{participantID},{ts},{te},{r.feature},{r.value.ToString("F4", CultureInfo.InvariantCulture)}");
        }

        File.WriteAllText(arquivo, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[TestManager] CSV salvo em: {arquivo}");
    }
}
