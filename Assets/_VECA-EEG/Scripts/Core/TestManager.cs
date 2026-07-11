using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Orchestrator for the VECA cognitive assessment.
///
/// SEQUENCE:
///   1. Memory      → vr_mem8, vr_mem9, vr_mem10
///   2. Attention   → vr_att
///   3. Abstraction → vr_abs
///   4-6. Calculation → vr_calc4, vr_calc5, vr_calc6
///   7. Execution   → vr_exec
///   8. Recall      → vr_recall
///
/// CSV saved to: &lt;project&gt;/Results/VECA_&lt;ID&gt;_&lt;timestamp&gt;.csv
/// </summary>
[DefaultExecutionOrder(-10)]
public class TestManager : MonoBehaviour
{
    public static TestManager Instance { get; private set; }

    [Header("UI References")]
    public UIManager  uiManager;
    public EyeTracker eyeTracker;

    [Header("Tasks")]
    public MemoryTask      memoryTask;
    public AttentionTask   attentionTask;
    public AbstractionTask abstractionTask;
    public CalculationTask calculationTask;
    public ExecutionTask   executionTask;
    public RecallTask      recallTask;

    [Header("Participant")]
    [Tooltip("Auto-filled with a random ID when the test starts")]
    public string participantID = "";

    [Header("Localization")]
    [Tooltip("Asset with all UI texts. Null = PT-BR defaults.")]
    public LocalizationConfig locConfig;

    [Header("Options")]
    public bool  autoStart         = false;
    public float pauseBetweenTasks = 1.5f;

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

        participantID = GenerateParticipantID();
        if (uiManager != null) uiManager.SetParticipantID(participantID);
    }

    void Start()
    {
        if (autoStart) StartTest();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void StartTest()
    {
        if (!TestRunning) StartCoroutine(FullSequence());
    }

    // ── Full sequence ────────────────────────────────────────────────────────

    private IEnumerator FullSequence()
    {
        TestRunning = true;
        records.Clear();

        LSLMarkerStream.Instance?.SendMarker($"session_start,{participantID}");

        uiManager.HideStartScreen();
        uiManager.HideParticipantID();
        uiManager.ShowInstruction(locConfig?.prepareMessage ?? "\n\nGet ready to begin.", 3f);
        yield return new WaitForSeconds(3f);

        // ── 1. MEMORY ─────────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Memory";
        yield return StartCoroutine(RunMemory());
        yield return new WaitForSeconds(pauseBetweenTasks);

        // ── 2. ATTENTION ──────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Attention";
        yield return StartCoroutine(RunSimpleTask(attentionTask, "vr_att"));
        yield return new WaitForSeconds(pauseBetweenTasks);

        // ── 3. ABSTRACTION ────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Abstraction";
        yield return StartCoroutine(RunAbstraction());
        yield return new WaitForSeconds(pauseBetweenTasks);

        // ── 4-6. CALCULATION ──────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Calculation";
        yield return StartCoroutine(RunCalculation());
        yield return new WaitForSeconds(pauseBetweenTasks);

        // ── 7. EXECUTION ──────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Execution";
        yield return StartCoroutine(RunSimpleTask(executionTask, "vr_exec"));
        yield return new WaitForSeconds(pauseBetweenTasks);

        // ── 8. RECALL ─────────────────────────────────────────────────────────
        eyeTracker.CurrentTrialLabel = "Recall";
        yield return StartCoroutine(RunRecall());

        // ── CLOSING ───────────────────────────────────────────────────────────
        LSLMarkerStream.Instance?.SendMarker($"session_end,{participantID}");
        SaveCSV();
        uiManager.SetTaskStatus(locConfig?.statusCompleted ?? "Completed");
        uiManager.SetParticipantID(participantID);
        uiManager.ShowInstruction(
            locConfig?.completionMessage ?? "Assessment complete!\n\nThank you for your participation.\nResults saved.", 0f);
        TestRunning = false;

        yield return StartCoroutine(uiManager.WaitForRestart());

        participantID = GenerateParticipantID();
        uiManager.SetParticipantID(participantID);
        uiManager.HideInstruction();
        uiManager.SetTaskStatus("");
        uiManager.ShowStartScreen();
    }

    // ── Task coordinators ────────────────────────────────────────────────────

    private IEnumerator RunMemory()
    {
        if (memoryTask == null) { Debug.LogWarning("[TestManager] MemoryTask not assigned."); yield break; }

        yield return StartCoroutine(memoryTask.RunAllTrials());

        for (int i = 0; i < 3; i++)
        {
            var (s, e) = memoryTask.GetTrialTimes(i);
            records.Add(new TrialRecord { feature = $"vr_mem{8 + i}", value = memoryTask.GetTrialScore(i), trialStart = s, trialEnd = e });
        }
        Debug.Log($"[Memory] {memoryTask.GetTrialScore(0):F3} / {memoryTask.GetTrialScore(1):F3} / {memoryTask.GetTrialScore(2):F3}");
    }

    /// <summary>Runs single-trial tasks (Attention, Execution).</summary>
    private IEnumerator RunSimpleTask(TaskBase task, string featureName)
    {
        if (task == null)
        {
            Debug.LogWarning($"[TestManager] {featureName} not assigned — skipping.");
            records.Add(new TrialRecord { feature = featureName, value = -1f });
            yield break;
        }

        task.StartTask();
        yield return null;                                       // wait for IsRunning = true
        yield return new WaitUntil(() => !task.IsRunning);

        records.Add(new TrialRecord
        {
            feature    = featureName,
            value      = task.LastScore,
            trialStart = task.TrialStartTime,
            trialEnd   = task.TrialEndTime
        });
        Debug.Log($"[{featureName}] {task.LastScore:F3}");
    }

    private IEnumerator RunAbstraction()
    {
        if (abstractionTask == null)
        {
            Debug.LogWarning("[TestManager] AbstractionTask not assigned.");
            records.Add(new TrialRecord { feature = "vr_abs", value = -1f });
            yield break;
        }

        yield return StartCoroutine(abstractionTask.RunAllTrials());

        float score  = abstractionTask.GetScore();
        var   tStart = abstractionTask.GetTrialTimes(0).start;
        var   tEnd   = abstractionTask.GetTrialTimes(abstractionTask.trials.Length - 1).end;
        records.Add(new TrialRecord { feature = "vr_abs", value = score, trialStart = tStart, trialEnd = tEnd });
        Debug.Log($"[Abstraction] {score:F3}");
    }

    private IEnumerator RunCalculation()
    {
        if (calculationTask == null) { Debug.LogWarning("[TestManager] CalculationTask not assigned."); yield break; }

        yield return StartCoroutine(calculationTask.RunAllTrials());

        for (int i = 0; i < 3; i++)
        {
            var (s, e) = calculationTask.GetTrialTimes(i);
            records.Add(new TrialRecord { feature = $"vr_calc{4 + i}", value = calculationTask.GetTrialScore(i), trialStart = s, trialEnd = e });
        }
        Debug.Log($"[Calculation] {calculationTask.GetTrialScore(0):F3} / {calculationTask.GetTrialScore(1):F3} / {calculationTask.GetTrialScore(2):F3}");
    }

    private IEnumerator RunRecall()
    {
        if (recallTask == null)
        {
            Debug.LogWarning("[TestManager] RecallTask not assigned.");
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

    // ── ID generation ────────────────────────────────────────────────────────

    private static string GenerateParticipantID()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no 0/O/1/I/L
        var sb = new System.Text.StringBuilder(6);
        for (int i = 0; i < 6; i++)
            sb.Append(chars[Random.Range(0, chars.Length)]);
        return sb.ToString();
    }

    // ── CSV export ───────────────────────────────────────────────────────────

    private void SaveCSV()
    {
        string folder = Path.Combine(Application.dataPath, "..", "Results");
        Directory.CreateDirectory(folder);

        string sessionTs = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string file      = Path.Combine(folder, $"VECA_{participantID}_{sessionTs}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("participant_id,trial_start,trial_end,feature,value");

        foreach (var r in records)
        {
            string ts  = r.trialStart == default ? sessionTs : r.trialStart.ToString("yyyyMMdd_HHmmss.fff");
            string te  = r.trialEnd   == default ? sessionTs : r.trialEnd.ToString("yyyyMMdd_HHmmss.fff");
            sb.AppendLine($"{participantID},{ts},{te},{r.feature},{r.value.ToString("F4", CultureInfo.InvariantCulture)}");
        }

        File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[TestManager] CSV saved to: {file}");
    }
}
