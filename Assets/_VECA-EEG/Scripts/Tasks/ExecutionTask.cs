using UnityEngine;

/// <summary>
/// Execution Task — feature: vr_exec
///
/// The participant receives a command and must fixate on the correct option.
/// VR adaptation of the MOCA command execution test.
///
/// INSPECTOR SETUP:
///   Instruction   → command (e.g., "Look at the object that is above the others")
///   Options       → 4 labels on the buttons
///   CorrectAnswer → which option is correct
///   executionTime → 8 s (article default)
/// </summary>
public class ExecutionTask : TaskBase
{
    [Header("Content")]
    [Tooltip("Command shown during the trial")]
    [TextArea(2, 4)]
    public string instruction = "\n\nLook at the number that comes after 7 in the sequence: 8 - 7 - ?";

    [Tooltip("4 options shown on the buttons")]
    public string[] options = { "6", "4", "7", "5" };

    [Tooltip("Which option is the correct answer")]
    public string correctAnswer = "6";

    protected override void Awake()
    {
        base.Awake();
        taskName                       = "EXECUTION";
        executionTime                  = 8f;
        showInstructionDuringExecution = true;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TASK:</b> EXECUTION\n\n" +
                "A command will appear in the instruction.\n" +
                "Read it carefully and fix your gaze on the correct option\n" +
                "among the 4 shown on screen.\n\n" +
                "<b>Example:</b> \"Look at the number that comes after 2:\n" +
                "1 - 2 - ?\" → look at number 3.";
    }

    protected override void SetupTrial()
    {
        uiManager.SetupAOIs(Loc?.executionOpcoes ?? options, Loc?.executionRespostaCorreta ?? correctAnswer);
        uiManager.ShowAOIs(true);
    }

    protected override string GetTaskName()        => Loc?.taskExecution    ?? taskName;
    protected override string GetDescription()     => L(Loc?.descExecution, taskDescription);
    protected override float  CalculateScore()     => eyeTracker.GetCorrectAOIPercentage();
    protected override string GetFeatureName()     => "vr_exec";
    protected override string GetInstructionText() => Loc?.executionInstrucao ?? instruction;
}
