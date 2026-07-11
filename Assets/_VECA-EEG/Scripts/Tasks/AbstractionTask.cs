using System.Collections;
using UnityEngine;

/// <summary>
/// Abstraction Task — feature: vr_abs
///
/// Two trials: in each one, the participant sees a concept pair in the instruction
/// and must fixate on the category linking them among 4 options.
/// Final score = average of the 2 trials.
///
/// INSPECTOR SETUP:
///   Trials[0].Pair          → "Train and Bicycle"
///   Trials[0].Options       → ["Transportation","Animal","Color","Food"]
///   Trials[0].CorrectAnswer → "Transportation"
///   Trials[1].Pair          → "Clock and Ruler"
///   Trials[1].Options       → ["Measurement","Tool","Toy","Clothing"]
///   Trials[1].CorrectAnswer → "Measurement"
/// </summary>
public class AbstractionTask : TaskBase
{
    [System.Serializable]
    public struct AbstractionTrial
    {
        [Tooltip("Concept pair shown in the instruction")]
        public string pair;
        [Tooltip("4 categories shown on the buttons")]
        public string[] options;
        [Tooltip("Correct category")]
        public string correctAnswer;
    }

    [Header("Trials")]
    public AbstractionTrial[] trials = new AbstractionTrial[]
    {
        new AbstractionTrial
        {
            pair          = "Train and Bicycle",
            options       = new[] { "Transportation", "Animal", "Color", "Food" },
            correctAnswer = "Transportation"
        },
        new AbstractionTrial
        {
            pair          = "Clock and Ruler",
            options       = new[] { "Measurement", "Tool", "Toy", "Clothing" },
            correctAnswer = "Measurement"
        }
    };

    [Header("Texts")]
    [TextArea(1, 3)]
    public string instructionComplement = "What do they have in common?";

    [Header("Timing")]
    public float pauseBetweenTrials = 1f;

    private float[]           scores;
    private System.DateTime[] _trialStartTimes;
    private System.DateTime[] _trialEndTimes;
    private int               currentTrial;

    protected override void Awake()
    {
        base.Awake();
        taskName         = "ABSTRACTION";
        executionTime    = 8f;
        scores           = new float[trials.Length];
        _trialStartTimes = new System.DateTime[trials.Length];
        _trialEndTimes   = new System.DateTime[trials.Length];
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TASK:</b> ABSTRACTION\n\n" +
                "Two concepts will appear in the instruction.\n" +
                "Among the 4 options, fix your gaze on the category that best\n" +
                "describes what they have in common.\n\n" +
                "<b>Example:</b> \"House and Bridge\" → look at \"Structures\".\n\n" +
                "This task has 2 rounds.";
    }

    // ── API for TestManager ──────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
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

    /// <summary>Average score across trials (0–1).</summary>
    public float GetScore()
    {
        if (scores.Length == 0) return 0f;
        float sum = 0f;
        foreach (var s in scores) sum += s;
        return sum / scores.Length;
    }

    // ── Trial ────────────────────────────────────────────────────────────────

    private IEnumerator RunSingleTrial(int idx)
    {
        var      trial      = trials[idx];
        var      locTrial   = Loc?.abstractionTrials?.Length > idx ? Loc.abstractionTrials[idx] : null;
        string   pair       = locTrial?.par             ?? trial.pair;
        string[] options    = locTrial?.opcoes          ?? trial.options;
        string   answer     = locTrial?.respostaCorreta ?? trial.correctAnswer;
        string   complement = Loc?.abstractionComplement ?? instructionComplement;

        uiManager.SetTaskStatus($"{Loc?.taskAbstraction ?? taskName} ({idx + 1}/{trials.Length})");

        string instructionText = $"<b>{pair}</b>\n{complement}";
        uiManager.ShowInstruction(instructionText);
        yield return new WaitForSeconds(preparationTime);

        uiManager.SetupAOIs(options, answer);
        uiManager.ShowAOIs(true);

        uiManager.ShowInstruction(instructionText);

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

    // ── Required TaskBase implementations ────────────────────────────────────

    protected override void SetupTrial()
    {
        if (trials.Length <= currentTrial) return;
        var      locTrial = Loc?.abstractionTrials?.Length > currentTrial ? Loc.abstractionTrials[currentTrial] : null;
        string[] opts     = locTrial?.opcoes          ?? trials[currentTrial].options;
        string   resp     = locTrial?.respostaCorreta ?? trials[currentTrial].correctAnswer;
        uiManager.SetupAOIs(opts, resp);
    }

    protected override string GetTaskName()    => Loc?.taskAbstraction ?? taskName;
    protected override string GetDescription() => L(Loc?.descAbstraction, taskDescription);
    protected override float  CalculateScore() => scores.Length > currentTrial ? scores[currentTrial] : 0f;
    protected override string GetFeatureName() => "vr_abs";
    protected override string GetInstructionText()
    {
        if (trials.Length <= currentTrial) return "";
        var    locTrial   = Loc?.abstractionTrials?.Length > currentTrial ? Loc.abstractionTrials[currentTrial] : null;
        string pair       = locTrial?.par ?? trials[currentTrial].pair;
        string complement = Loc?.abstractionComplement ?? instructionComplement;
        return $"{pair}\n{complement}";
    }
}
