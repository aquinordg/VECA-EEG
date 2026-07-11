using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recall Task — feature: vr_recall
///
/// Recall of the 3 images memorized at the start, after all other tasks.
/// Reuses stimuli (sprites or labels) from MemoryTask.
/// Final score = average of the 3 trials.
///
/// INSPECTOR SETUP:
///   MemoryTask → reference to the MemoryTask component in the scene
///                (the same sprites/labels are used automatically)
/// </summary>
public class RecallTask : TaskBase
{
    [Header("Reference")]
    [Tooltip("MemoryTask component in the scene — reuses its stimuli")]
    public MemoryTask memoryTask;

    private static readonly string[] _ordinalsFallback = { "first", "second", "third" };

    private string GetOrdinal(int idx)
    {
        var ords = Loc?.recallOrdinals;
        if (ords != null && idx < ords.Length) return ords[idx];
        return idx < _ordinalsFallback.Length ? _ordinalsFallback[idx] : (idx + 1).ToString();
    }

    private string GetRecallPrompt(int idx)
    {
        string tmpl = Loc?.recallPromptTemplate ??
            "\nDo you remember the <b>{0}</b> image shown at the beginning?\nNow try to recall and look at it.";
        return string.Format(tmpl, GetOrdinal(idx));
    }

    [Header("Timing")]
    public float pauseBetweenTrials = 1f;

    private float[]           scores           = new float[3];
    private System.DateTime[] _trialStartTimes = new System.DateTime[3];
    private System.DateTime[] _trialEndTimes   = new System.DateTime[3];
    private int               currentTrial;

    protected override void Awake()
    {
        base.Awake();
        taskName      = "RECALL";
        executionTime = 8f;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TASK:</b> RECALL\n\n" +
                "Do you remember the images you saw at the beginning?\n\n" +
                "Each round will show 4 options — fix your gaze on the image\n" +
                "that was shown at the start.\n\n" +
                "<b>Example:</b> if you memorized a Lion, look at the Lion when\n" +
                "it appears among the options.\n\n" +
                "This task has 3 rounds and will show, in order,\n" +
                "the first, second, and third images from the beginning.";
    }

    // ── API for TestManager ──────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
        yield return StartCoroutine(IntroPhase());

        for (int i = 0; i < 3; i++)
        {
            currentTrial = i;
            yield return RunSingleTrial(i);
            if (i < 2)
                yield return new WaitForSeconds(pauseBetweenTrials);
        }
    }

    public (System.DateTime start, System.DateTime end) GetTrialTimes(int idx) =>
        (_trialStartTimes[idx], _trialEndTimes[idx]);

    /// <summary>Average of the 3 recall trials (0–1).</summary>
    public float GetScore()
    {
        float sum = 0f;
        foreach (var s in scores) sum += s;
        return sum / scores.Length;
    }

    // ── Trial ────────────────────────────────────────────────────────────────

    private IEnumerator RunSingleTrial(int idx)
    {
        uiManager.SetTaskStatus($"{Loc?.taskRecall ?? taskName} ({idx + 1}/3)");

        string instruction = GetRecallPrompt(idx);

        uiManager.ShowInstruction(instruction);
        yield return new WaitForSeconds(preparationTime);

        if (!SetupAOIs(idx))
        {
            scores[idx] = 0f;
            yield break;
        }

        uiManager.ShowAOIs(true);
        uiManager.ShowInstruction(instruction);

        AOI aoiCorrect = uiManager.GetCorrectAOI();
        eyeTracker.SetCurrentCorrectAOI(aoiCorrect);
        eyeTracker.StartRecording();

        float elapsed = 0f;
        while (elapsed < executionTime)
        {
            elapsed += Time.deltaTime;
            uiManager.UpdateTimer(executionTime - elapsed);
            yield return null;
        }

        eyeTracker.StopRecording();
        _trialStartTimes[idx] = eyeTracker.RecordingStartTime;
        _trialEndTimes[idx]   = eyeTracker.RecordingEndTime;
        uiManager.HideInstruction();
        scores[idx] = eyeTracker.GetCorrectAOIPercentage();

        uiManager.ShowAOIs(false);

        bool correct = scores[idx] >= 0.5f;
        uiManager.ShowFeedback(FormatFeedback(scores[idx]), correct);
        yield return new WaitForSeconds(1.5f);
        uiManager.HideFeedback();
    }

    // ── AOI setup (reuses MemoryTask logic) ──────────────────────────────────

    private bool SetupAOIs(int idx)
    {
        if (memoryTask == null)
        {
            Debug.LogError("[RecallTask] MemoryTask not assigned — trial aborted.");
            return false;
        }

        Sprite   targetSprite = memoryTask.GetTargetSprite(idx);
        Sprite[] distractors  = memoryTask.GetDistractorSprites();
        bool     useSprites   = targetSprite != null;

        if (useSprites)
        {
            Sprite target = targetSprite;
            var pool      = new List<Sprite>();
            if (distractors != null) pool.AddRange(distractors);
            for (int k = 0; k < 3; k++)
            {
                Sprite s = memoryTask.GetTargetSprite(k);
                if (s != target && s != null && !pool.Contains(s)) pool.Add(s);
            }

            var options = new List<Sprite> { target };
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int r = Random.Range(0, pool.Count);
                options.Add(pool[r]);
                pool.RemoveAt(r);
            }
            uiManager.SetupAOIs(options.ToArray(), target);
        }
        else
        {
            string target = memoryTask.GetTargetLabel(idx);
            var pool      = new List<string>(memoryTask.distractorLabels);
            for (int k = 0; k < 3; k++) pool.Remove(memoryTask.GetTargetLabel(k));
            for (int k = 0; k < 3; k++)
            {
                string lbl = memoryTask.GetTargetLabel(k);
                if (lbl != target && !pool.Contains(lbl)) pool.Add(lbl);
            }

            var options = new List<string> { target };
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int r = Random.Range(0, pool.Count);
                options.Add(pool[r]);
                pool.RemoveAt(r);
            }
            uiManager.SetupAOIs(options.ToArray(), target);
        }

        return true;
    }

    // ── Required TaskBase implementations ────────────────────────────────────

    protected override void   SetupTrial()        => _ = SetupAOIs(currentTrial);
    protected override string GetTaskName()       => Loc?.taskRecall  ?? taskName;
    protected override string GetDescription()    => L(Loc?.descRecall, taskDescription);
    protected override float  CalculateScore()    => scores.Length > currentTrial ? scores[currentTrial] : 0f;
    protected override string GetFeatureName()    => "vr_recall";
    protected override string GetInstructionText() => GetRecallPrompt(currentTrial);
}
