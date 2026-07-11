using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calculation Task — features: vr_calc4, vr_calc5, vr_calc6
///
/// Three serial subtractions (100−7). The participant sees the problem in the instruction
/// and fixates on the correct answer among 4 numerical options.
///
/// INSPECTOR SETUP:
///   Trials[0] → problem "100 − 7 =", options ["93","92","94","97"], correct "93"
///   Trials[1] → problem "93 − 7 =",  options ["86","85","87","84"], correct "86"
///   Trials[2] → problem "86 − 7 =",  options ["79","78","80","77"], correct "79"
/// </summary>
public class CalculationTask : TaskBase
{
    [System.Serializable]
    public class CalculationTrial
    {
        [Tooltip("Problem shown in the instruction (e.g., '100 − 7 =')")]
        public string problem;
        [Tooltip("4 numerical options on the buttons")]
        public string[] options;
        [Tooltip("Correct answer (must match one of the options)")]
        public string correctAnswer;
        [Tooltip("Feature name in the CSV")]
        public string featureName;
    }

    [Header("Trials")]
    public CalculationTrial[] trials = new CalculationTrial[]
    {
        new CalculationTrial { problem = "100 − 7 =", options = new[]{"93","92","94","97"}, correctAnswer = "93", featureName = "vr_calc4" },
        new CalculationTrial { problem = "93 − 7 =",  options = new[]{"86","85","87","84"}, correctAnswer = "86", featureName = "vr_calc5" },
        new CalculationTrial { problem = "86 − 7 =",  options = new[]{"79","78","80","77"}, correctAnswer = "79", featureName = "vr_calc6" },
    };

    [Header("Randomization")]
    [Tooltip("Generates random problems each session (overrides the values above)")]
    public bool randomize = true;

    [Header("Timing")]
    public float pauseBetweenTrials = 1f;

    private float[]           scores;
    private System.DateTime[] _trialStartTimes;
    private System.DateTime[] _trialEndTimes;
    private int               currentTrial;

    private void Reset()
    {
        trials = new CalculationTrial[]
        {
            new CalculationTrial { problem = "100 − 7 =", options = new[]{"93","92","94","97"}, correctAnswer = "93", featureName = "vr_calc4" },
            new CalculationTrial { problem = "93 − 7 =",  options = new[]{"86","85","87","84"}, correctAnswer = "86", featureName = "vr_calc5" },
            new CalculationTrial { problem = "86 − 7 =",  options = new[]{"79","78","80","77"}, correctAnswer = "79", featureName = "vr_calc6" },
        };
    }

    protected override void Awake()
    {
        base.Awake();
        taskName         = "CALCULATION";
        executionTime    = 8f;
        scores           = new float[trials.Length];
        _trialStartTimes = new System.DateTime[trials.Length];
        _trialEndTimes   = new System.DateTime[trials.Length];
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TASK:</b> CALCULATION\n\n" +
                "A subtraction problem will appear in the instruction.\n" +
                "Among the 4 options on screen, fix your gaze on the correct answer.\n\n" +
                "<b>Example:</b> \"10 − 8 =\" → look at the number 2.\n\n" +
                "This task has 3 rounds.";
    }

    // ── API for TestManager ──────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
        if (randomize) GenerateRandomTrials();

        yield return StartCoroutine(IntroPhase());

        for (int i = 0; i < trials.Length; i++)
        {
            currentTrial = i;
            yield return RunSingleTrial(i);
            if (i < trials.Length - 1)
                yield return new WaitForSeconds(pauseBetweenTrials);
        }
    }

    public (System.DateTime start, System.DateTime end) GetTrialTimes(int idx) =>
        (_trialStartTimes[idx], _trialEndTimes[idx]);

    /// <summary>Score for trial idx (0–2) after completion (0–1).</summary>
    public float GetTrialScore(int idx) =>
        idx >= 0 && idx < scores.Length ? scores[idx] : 0f;

    // ── Trial ────────────────────────────────────────────────────────────────

    private IEnumerator RunSingleTrial(int idx)
    {
        var trial = trials[idx];
        uiManager.SetTaskStatus($"{Loc?.taskCalculation ?? taskName} ({idx + 1}/{trials.Length})");

        string prompt = $"{Loc?.calculationPrompt ?? "Look at the correct answer."}\n\n<b>{trial.problem}</b>";
        uiManager.ShowInstruction(prompt);
        yield return new WaitForSeconds(preparationTime);

        uiManager.SetupAOIs(trial.options, trial.correctAnswer);
        uiManager.ShowAOIs(true);

        uiManager.ShowInstruction(prompt);

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

    // ── Random generation ────────────────────────────────────────────────────

    private void GenerateRandomTrials()
    {
        for (int i = 0; i < trials.Length; i++)
        {
            int a, b;
            do {
                a = Random.Range(1, 11); // 1–10
                b = Random.Range(1, 6);  // 1–5
            } while (a <= b);            // ensure a > b

            bool isAddition = Random.value >= 0.5f;
            int  result     = isAddition ? a + b : a - b;
            string op       = isAddition ? "+" : "−";

            var distractors = GenerateDistractors(result, 3);

            string fname = trials[i].featureName;
            trials[i] = new CalculationTrial
            {
                problem       = $"{a} {op} {b} =",
                options       = new[] { result.ToString(), distractors[0].ToString(),
                                        distractors[1].ToString(), distractors[2].ToString() },
                correctAnswer = result.ToString(),
                featureName   = fname
            };
        }
    }

    private static List<int> GenerateDistractors(int correct, int count)
    {
        var pool = new List<int>();
        for (int d = 1; d <= 5; d++)
        {
            if (correct + d > 0) pool.Add(correct + d);
            if (correct - d > 0) pool.Add(correct - d);
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool.GetRange(0, Mathf.Min(count, pool.Count));
    }

    // ── Required TaskBase implementations ────────────────────────────────────

    protected override void SetupTrial()
    {
        if (trials.Length > currentTrial)
            uiManager.SetupAOIs(trials[currentTrial].options, trials[currentTrial].correctAnswer);
    }

    protected override string GetTaskName()    => Loc?.taskCalculation ?? taskName;
    protected override string GetDescription() => L(Loc?.descCalculation, taskDescription);
    protected override float  CalculateScore() => scores.Length > currentTrial ? scores[currentTrial] : 0f;
    protected override string GetFeatureName() => currentTrial < trials.Length ? trials[currentTrial].featureName : "vr_calc";
    protected override string GetInstructionText() => trials.Length > currentTrial ? trials[currentTrial].problem : "";
}
