using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Memory Task — replicates the VECA article protocol.
///
/// PROTOCOL PER TRIAL:
///   Encoding  (encodingTime s)  → shows the target image/label in isolation
///   Storage   (storageDelay s)  → blank screen (retention interval)
///   Recall    (executionTime s) → 4 AOIs on screen; participant fixates the correct one
///
/// FEATURES GENERATED:
///   Trial 0 → vr_mem8  |  Trial 1 → vr_mem9  |  Trial 2 → vr_mem10
///
/// SPRITE MODE (recommended):
///   Sprites → single pool with all available sprites (minimum 4).
///   Per session, 3 are randomly selected as targets; the rest become distractors.
///
/// TEXT MODE (fallback — when Sprites is empty):
///   TargetLabels     → 3 target labels
///   DistractorLabels → pool of distractor labels
/// </summary>
public class MemoryTask : TaskBase
{
    [Header("Stimuli — Sprite Mode")]
    [Tooltip("Single sprite pool. Minimum 4: 3 will be drawn as targets, the rest as distractors.")]
    public Sprite[] sprites;

    [Header("Stimuli — Text Mode (fallback)")]
    [Tooltip("Labels for the 3 targets (used when Sprites is empty)")]
    public string[] targetLabels = { "Lion", "Rhinoceros", "Camel" };

    [Tooltip("Pool of distractor labels")]
    public string[] distractorLabels = { "Elephant", "Zebra", "Giraffe", "Tiger", "Wolf" };

    [Header("Order")]
    [Tooltip("Randomly shuffles targets and presentation order each session")]
    public bool randomize = true;

    [Header("Encoding Display")]
    [Tooltip("GameObject shown only during the encoding phase")]
    public GameObject encodingDisplay;

    [Tooltip("Image inside the encodingDisplay")]
    public Image encodingImage;

    [Header("Timing")]
    [Tooltip("Duration of target display during encoding (s)")]
    public float encodingTime = 4f;
    [Tooltip("Interval between encoding and recall — blank screen (s)")]
    public float storageDelay = 3f;
    [Tooltip("Pause between trials (s)")]
    public float pauseBetweenTrials = 0.8f;

    // ── Internal state ───────────────────────────────────────────────────────

    private int currentTrial;
    private readonly float[]           scores           = new float[3];
    private readonly System.DateTime[] _trialStartTimes = new System.DateTime[3];
    private readonly System.DateTime[] _trialEndTimes   = new System.DateTime[3];

    // Runtime arrays calculated in InitializeOrder()
    private string[] _activeLabels;      // 3 labels in this session's order
    private Sprite[] _activeSprites;     // 3 sprites drawn as targets
    private Sprite[] _activeDistractors; // remaining sprites (distractors)

    private static readonly string[] featureNames = { "vr_mem8", "vr_mem9", "vr_mem10" };

    protected override void Awake()
    {
        base.Awake();
        taskName      = "MEMORY";
        executionTime = 8f;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TASK:</b> MEMORY\n\n" +
                "You will see 3 images, one at a time, for a few seconds.\n" +
                "After a brief interval, the same image will reappear mixed\n" +
                "with 3 other options — fix your gaze on it.\n\n" +
                "<b>Example:</b> if you saw a Lion, look at the Lion when it\n" +
                "appears among the 4 options.\n\n" +
                "This task has 3 rounds.";
        InitializeOrder(shuffle: false);
    }

    // ── Public getters (used by RecallTask) ──────────────────────────────────

    public string   GetTargetLabel(int idx)    => _activeLabels    != null && idx < _activeLabels.Length    ? _activeLabels[idx]    : null;
    public Sprite   GetTargetSprite(int idx)   => _activeSprites   != null && idx < _activeSprites.Length   ? _activeSprites[idx]   : null;
    public Sprite[] GetDistractorSprites()     => _activeDistractors;
    public (System.DateTime start, System.DateTime end) GetTrialTimes(int idx) =>
        (_trialStartTimes[idx], _trialEndTimes[idx]);

    // ── Public API ───────────────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
        if (Loc?.memoryTargetLabels?.Length >= 3)
            targetLabels = Loc.memoryTargetLabels;
        if (Loc?.memoryDistractorLabels?.Length > 0)
            distractorLabels = Loc.memoryDistractorLabels;

        yield return StartCoroutine(IntroPhase());

        InitializeOrder(shuffle: randomize);

        for (int i = 0; i < 3; i++)
        {
            currentTrial = i;
            yield return StartCoroutine(RunSingleTrial(i));
            yield return new WaitForSeconds(pauseBetweenTrials);
        }
    }

    public float GetTrialScore(int index) =>
        index >= 0 && index < scores.Length ? scores[index] : 0f;

    // ── Order initialization ──────────────────────────────────────────────────

    private void InitializeOrder(bool shuffle)
    {
        // Text mode
        int[] textIdx = { 0, 1, 2 };
        if (shuffle) Shuffle(textIdx);
        _activeLabels = new string[3];
        for (int i = 0; i < 3; i++)
            _activeLabels[i] = targetLabels.Length > textIdx[i] ? targetLabels[textIdx[i]] : "";

        // Sprite mode
        if (sprites == null || sprites.Length < 4)
        {
            _activeSprites      = null;
            _activeDistractors  = null;
            return;
        }

        // Build pool of available indices
        var pool = new List<int>();
        for (int i = 0; i < sprites.Length; i++) pool.Add(i);

        // Draw 3 targets
        int[] targets = new int[3];
        for (int i = 0; i < 3; i++)
        {
            int pick = shuffle ? Random.Range(0, pool.Count) : i;
            targets[i] = pool[pick];
            pool.RemoveAt(pick);
        }

        // Shuffle the presentation order of targets
        if (shuffle) Shuffle(targets);

        _activeSprites = new Sprite[3];
        for (int i = 0; i < 3; i++) _activeSprites[i] = sprites[targets[i]];

        _activeDistractors = new Sprite[pool.Count];
        for (int i = 0; i < pool.Count; i++) _activeDistractors[i] = sprites[pool[i]];
    }

    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    // ── Full trial ───────────────────────────────────────────────────────────

    private IEnumerator RunSingleTrial(int idx)
    {
        uiManager.SetTaskStatus($"{Loc?.taskMemory ?? taskName} ({idx + 1}/3)");

        // ── PHASE 1: ENCODING ────────────────────────────────────────────────
        ShowEncodingDisplay(idx, true);
        uiManager.ShowAOIs(false);
        uiManager.ShowInstruction(Loc?.memoryEncodePrompt ?? "Memorize this image", encodingTime);

        yield return new WaitForSeconds(encodingTime);

        uiManager.HideInstruction();
        ShowEncodingDisplay(idx, false);

        // ── PHASE 2: STORAGE ─────────────────────────────────────────────────
        uiManager.ShowInstruction("\n\n...", storageDelay);
        yield return new WaitForSeconds(storageDelay);
        uiManager.HideInstruction();

        // ── PHASE 3: RECALL ──────────────────────────────────────────────────
        SetupRecallAOIs(idx);
        uiManager.ShowInstruction(Loc?.memoryRecallPrompt ?? "\n\nFix your gaze on the correct answer.");

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

    // ── Recall AOI setup ─────────────────────────────────────────────────────

    private void SetupRecallAOIs(int idx)
    {
        if (_activeSprites != null)
            SetupRecallWithSprites(idx);
        else
            SetupRecallWithText(_activeLabels[idx]);

        uiManager.ShowAOIs(true);
    }

    private void SetupRecallWithSprites(int idx)
    {
        Sprite target = _activeSprites[idx];

        var pool = new List<Sprite>();
        if (_activeDistractors != null) pool.AddRange(_activeDistractors);
        foreach (var s in _activeSprites)
            if (s != target && !pool.Contains(s)) pool.Add(s);

        var options = new List<Sprite> { target };
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int r = Random.Range(0, pool.Count);
            options.Add(pool[r]);
            pool.RemoveAt(r);
        }

        uiManager.SetupAOIs(options.ToArray(), target);
    }

    private void SetupRecallWithText(string target)
    {
        var pool = new List<string>(distractorLabels);
        foreach (var lbl in _activeLabels) pool.Remove(lbl);
        foreach (var lbl in _activeLabels)
            if (lbl != target && !pool.Contains(lbl)) pool.Add(lbl);

        var options = new List<string> { target };
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int r = Random.Range(0, pool.Count);
            options.Add(pool[r]);
            pool.RemoveAt(r);
        }

        uiManager.SetupAOIs(options.ToArray(), target);
    }

    // ── Encoding display ─────────────────────────────────────────────────────

    private void ShowEncodingDisplay(int idx, bool active)
    {
        if (encodingDisplay == null) return;
        encodingDisplay.SetActive(active);

        if (!active || encodingImage == null) return;
        if (_activeSprites != null && idx < _activeSprites.Length)
            encodingImage.sprite = _activeSprites[idx];
    }

    // ── TaskBase implementation ──────────────────────────────────────────────

    protected override void   SetupTrial()      => SetupRecallAOIs(currentTrial);
    protected override float  CalculateScore()  => eyeTracker.GetCorrectAOIPercentage();
    protected override string GetTaskName()     => Loc?.taskMemory ?? taskName;
    protected override string GetDescription()  => L(Loc?.descMemory, taskDescription);
    protected override string GetFeatureName()  => currentTrial < featureNames.Length ? featureNames[currentTrial] : "vr_mem";
    protected override string GetInstructionText() => $"Where was: <b>{_activeLabels[currentTrial]}</b>?";
}
