using System.Collections;
using UnityEngine;

/// <summary>
/// Visual Attention Task — feature: vr_att
///
/// Shows 4 options; the participant must fixate on the target stimulus.
/// The instruction remains visible during execution to remind the participant of the target.
///
/// INSPECTOR SETUP (image mode):
///   OpcaoSprites  → 4 sprites (e.g., Lion, Cow, Pig, Chicken)
///   CorrectSprite → sprite of the correct answer (e.g., Lion)
///   Instruction   → text displayed during the trial
///   executionTime → 8 s
///
/// INSPECTOR SETUP (text mode — fallback):
///   Options       → 4 labels
///   CorrectAnswer → one of the labels above
/// </summary>
public class AttentionTask : TaskBase
{
    [Header("Content")]
    [Tooltip("Instruction shown during the trial")]
    [TextArea(2, 4)]
    public string instruction = "\n\n\nLook at the WILD ANIMAL.";

    [Header("Image Mode (priority)")]
    [Tooltip("4 sprites shown on the buttons")]
    public Sprite[] opcaoSprites;

    [Tooltip("Sprite of the correct answer (must be one of opcaoSprites)")]
    public Sprite correctSprite;

    [Header("Text Mode (fallback)")]
    [Tooltip("4 labels shown on the buttons")]
    public string[] options = { "Lion", "Cow", "Pig", "Chicken" };

    [Tooltip("Which option is the correct answer")]
    public string correctAnswer = "Lion";

    protected override void Awake()
    {
        base.Awake();
        taskName                       = "ATTENTION";
        executionTime                  = 8f;
        showInstructionDuringExecution = true;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TASK:</b> ATTENTION\n\n" +
                "An instruction will appear on screen.\n" +
                "Read it carefully and fix your gaze on the correct item\n" +
                "among the 4 options presented.\n\n" +
                "<b>Example:</b> if the instruction says \"Look at the toy\",\n" +
                "look at the image of the toy.\n\n" +
                "This task has 1 round.";
    }

    protected override IEnumerator PreparationPhase()
    {
        uiManager.ShowInstruction(GetInstructionText());
        float elapsed = 0f;
        while (elapsed < preparationTime)
        {
            elapsed += Time.deltaTime;
            uiManager.UpdateTimer(preparationTime - elapsed);
            yield return null;
        }
        // Instruction remains visible when AOIs appear
    }

    protected override void SetupTrial()
    {
        bool useSprites = opcaoSprites != null && opcaoSprites.Length >= 2 && correctSprite != null;

        if (useSprites)
            uiManager.SetupAOIs(opcaoSprites, correctSprite);
        else
            uiManager.SetupAOIs(Loc?.attentionOpcoes ?? options, Loc?.attentionRespostaCorreta ?? correctAnswer);

        uiManager.ShowAOIs(true);
    }

    protected override string GetTaskName()        => Loc?.taskAttention   ?? taskName;
    protected override string GetDescription()     => L(Loc?.descAttention, taskDescription);
    protected override float  CalculateScore()     => eyeTracker.GetCorrectAOIPercentage();
    protected override string GetFeatureName()     => "vr_att";
    protected override string GetInstructionText() => Loc?.attentionInstrucao ?? instruction;
}
