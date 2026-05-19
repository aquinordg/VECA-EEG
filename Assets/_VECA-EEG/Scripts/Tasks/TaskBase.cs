using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public abstract class TaskBase : MonoBehaviour
{
    [Header("General Settings")]
    public string taskName       = "Task";
    public float  preparationTime = 3f;
    public float  executionTime   = 8f;

    [Header("Task Introduction")]
    [Tooltip("Text shown before the task starts, with a Got It button. Leave empty to skip.")]
    [TextArea(2, 5)]
    public string taskDescription = "";

    [Header("Events")]
    [Tooltip("Fired when the task starts")]
    public UnityEvent OnTaskStarted;
    [Tooltip("Fired when the task completes: string = featureName, float = score (0-1)")]
    public UnityEvent<string, float> OnTaskCompleted;

    protected UIManager  uiManager;
    protected EyeTracker eyeTracker;

    public bool  IsRunning { get; private set; }
    public float LastScore { get; private set; }

    public System.DateTime TrialStartTime { get; protected set; }
    public System.DateTime TrialEndTime   { get; protected set; }

    protected bool showInstructionDuringExecution = false;

    protected virtual void Awake()
    {
        uiManager  = FindFirstObjectByType<UIManager>();
        eyeTracker = FindFirstObjectByType<EyeTracker>();
    }

    // ── Entry point ──────────────────────────────────────────────────────────

    public void StartTask()
    {
        if (!IsRunning)
            StartCoroutine(TaskSequence());
    }

    // ── Internal sequence ────────────────────────────────────────────────────

    private IEnumerator TaskSequence()
    {
        IsRunning = true;
        OnTaskStarted?.Invoke();
        uiManager.SetTaskStatus(taskName);

        yield return StartCoroutine(IntroPhase());
        yield return StartCoroutine(PreparationPhase());

        SetupTrial();

        yield return StartCoroutine(ExecutionPhase());

        float score = CalculateScore();
        LastScore   = score;

        IsRunning = false;
        OnTaskCompleted?.Invoke(GetFeatureName(), score);
    }

    // ── Phases ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows taskDescription with the Got It button and waits for confirmation.
    /// Call this at the start of RunAllTrials() in multi-trial tasks.
    /// </summary>
    protected IEnumerator IntroPhase()
    {
        if (string.IsNullOrWhiteSpace(taskDescription)) yield break;
        uiManager.SetTaskStatus(taskName);
        yield return StartCoroutine(uiManager.WaitForConfirmation(taskDescription));
    }

    protected virtual IEnumerator PreparationPhase()
    {
        uiManager.ShowInstruction(GetInstructionText());

        float elapsed = 0f;
        while (elapsed < preparationTime)
        {
            elapsed += Time.deltaTime;
            uiManager.UpdateTimer(preparationTime - elapsed);
            yield return null;
        }

        uiManager.HideInstruction();
    }

    protected virtual IEnumerator ExecutionPhase()
    {
        AOI correctAOI = uiManager.GetCorrectAOI();
        if (correctAOI != null)
            eyeTracker.SetCurrentCorrectAOI(correctAOI);

        if (showInstructionDuringExecution)
            uiManager.ShowInstruction(GetInstructionText());

        eyeTracker.StartRecording();

        float elapsed = 0f;
        while (elapsed < executionTime)
        {
            elapsed += Time.deltaTime;
            uiManager.UpdateTimer(executionTime - elapsed);
            yield return null;
        }

        eyeTracker.StopRecording();
        TrialStartTime = eyeTracker.RecordingStartTime;
        TrialEndTime   = eyeTracker.RecordingEndTime;

        if (showInstructionDuringExecution)
            uiManager.HideInstruction();

        uiManager.ShowAOIs(false);

        float pct    = eyeTracker.GetCorrectAOIPercentage();
        bool correct = pct >= 0.5f;
        uiManager.ShowFeedback($"{pct * 100f:F0}% do tempo na resposta correta", correct);
        yield return new WaitForSeconds(1.5f);
        uiManager.HideFeedback();
    }

    // ── Subclass contract ────────────────────────────────────────────────────

    protected abstract void   SetupTrial();
    protected abstract float  CalculateScore();
    protected abstract string GetFeatureName();
    protected abstract string GetInstructionText();
}
