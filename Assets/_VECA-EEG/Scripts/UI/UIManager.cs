using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Status & Timer")]
    public TextMeshProUGUI taskStatusText;
    public TextMeshProUGUI timerText;

    [Header("Instruction Panel")]
    public GameObject      instructionPanel;
    public TextMeshProUGUI instructionText;
    [Tooltip("Button inside InstructionPanel — wire OnClick → UIManager.ConfirmUnderstood")]
    public Button          gotItButton;

    [Header("Feedback Panel")]
    public GameObject      feedbackPanel;
    public TextMeshProUGUI feedbackText;

    [Header("AOI Grid")]
    [Tooltip("Parent transform containing the 4 AOIs")]
    public Transform aoiGrid;

    [Header("Start Screen")]
    [Tooltip("Parent panel containing the intro text and Start button — hidden when the test begins")]
    public GameObject startScreenPanel;

    [Tooltip("TMP element on the start screen that displays the participant ID — ex: 'ID: K7RN4A'")]
    public TextMeshProUGUI participantIDText;

    [Tooltip("Text appended to every task description shown with the Got It button")]
    public string gotItHint = "Para começar, olhe para o botão ENTENDIDO\nno canto da tela.";

    [Tooltip("TMP element that shows the intro text above the Start button")]
    public TextMeshProUGUI introText;

    [Tooltip("Button that starts the test — shown/hidden together with the start screen")]
    public Button startButton;

    [Tooltip("Button shown at conclusion to restart — wire OnClick → UIManager.ConfirmRestart")]
    public Button restartButton;

    [TextArea(4, 10)]
    [Tooltip("Introductory text shown on the start screen")]
    public string introMessage =
        "Bem-vindo à Avaliação Cognitiva VECA-EEG.\n\n" +
        "Este teste avalia funções cognitivas como memória, atenção, abstração, cálculo e execução de comandos.\n\n" +
        "Em cada etapa, leia a instrução com atenção e fixe o olhar na resposta correta entre as opções apresentadas na tela.\n\n" +
        "Quando estiver pronto para começar, pressione <b>INICIAR TESTE</b>.";

    [Header("Feedback Colors")]
    public Color correctColor  = new Color(0.10f, 0.80f, 0.20f);
    public Color incorrectColor = new Color(0.90f, 0.15f, 0.15f);
    public Color neutralColor  = new Color(0.20f, 0.60f, 1.00f);

    // ── Internal state ───────────────────────────────────────────────────────

    private AOI[]     aois;
    private Coroutine instructionCoroutine;
    private bool      _waitingForConfirmation;
    private bool      _waitingForRestart;

    // ── Unity ────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (feedbackPanel)    feedbackPanel.SetActive(false);
        if (instructionPanel) instructionPanel.SetActive(false);
        if (gotItButton)      gotItButton.gameObject.SetActive(false);
        if (restartButton)    restartButton.gameObject.SetActive(false);

        if (aoiGrid != null)
            aois = aoiGrid.GetComponentsInChildren<AOI>(true);
    }

    void Start()
    {
        if (aoiGrid != null)          aoiGrid.gameObject.SetActive(false);
        if (introText != null)        introText.text = introMessage;
        if (participantIDText != null) participantIDText.gameObject.SetActive(true);
    }

    // ── Start Screen ─────────────────────────────────────────────────────────

    public void SetParticipantID(string id)
    {
        if (participantIDText != null)
        {
            participantIDText.text = $"ID: {id}";
            participantIDText.gameObject.SetActive(true);
        }
    }

    public void HideParticipantID()
    {
        if (participantIDText != null)
            participantIDText.gameObject.SetActive(false);
    }

    public void ShowStartScreen()
    {
        if (startScreenPanel != null) startScreenPanel.SetActive(true);
        else if (introText != null)   introText.gameObject.SetActive(true);
        if (startButton != null)      startButton.gameObject.SetActive(true);
    }

    public void HideStartScreen()
    {
        if (startScreenPanel != null) startScreenPanel.SetActive(false);
        else if (introText != null)   introText.gameObject.SetActive(false);
        if (startButton != null)      startButton.gameObject.SetActive(false);
    }

    // ── Status & Timer ───────────────────────────────────────────────────────

    public void SetTaskStatus(string text)
    {
        if (taskStatusText) taskStatusText.text = text;
    }

    public void UpdateTimer(float seconds)
    {
        if (!timerText) return;
        seconds = Mathf.Max(0f, seconds);
        int min = Mathf.FloorToInt(seconds / 60f);
        int seg = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{min:00}:{seg:00}";
    }

    // ── Instructions ─────────────────────────────────────────────────────────

    public void ShowInstruction(string text, float duration = 0f)
    {
        if (instructionCoroutine != null) StopCoroutine(instructionCoroutine);

        if (instructionPanel) instructionPanel.SetActive(true);
        if (instructionText)  instructionText.text = text;

        if (duration > 0f)
            instructionCoroutine = StartCoroutine(HideInstructionAfter(duration));
    }

    public void HideInstruction()
    {
        if (instructionCoroutine != null)
        {
            StopCoroutine(instructionCoroutine);
            instructionCoroutine = null;
        }
        if (instructionPanel) instructionPanel.SetActive(false);
    }

    private IEnumerator HideInstructionAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInstruction();
    }

    // ── Got It Button ─────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the instruction panel with the given text and waits for the Got It button click.
    /// Wire the button's OnClick to UIManager.ConfirmUnderstood in the Inspector.
    /// </summary>
    public IEnumerator WaitForConfirmation(string text)
    {
        string full = string.IsNullOrWhiteSpace(gotItHint)
            ? text
            : $"{text}\n\n<b>{gotItHint}</b>";
        ShowInstruction(full);
        _waitingForConfirmation = true;

        if (gotItButton != null)
            gotItButton.gameObject.SetActive(true);

        yield return new WaitUntil(() => !_waitingForConfirmation);

        if (gotItButton != null)
            gotItButton.gameObject.SetActive(false);

        HideInstruction();
    }

    /// <summary>Called by the Got It button's OnClick event in the scene.</summary>
    public void ConfirmUnderstood()
    {
        _waitingForConfirmation = false;
    }

    /// <summary>Shows the restart button and waits for the player to click it.</summary>
    public IEnumerator WaitForRestart()
    {
        _waitingForRestart = true;
        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        yield return new WaitUntil(() => !_waitingForRestart);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
    }

    /// <summary>Called by the restart button's OnClick event in the scene.</summary>
    public void ConfirmRestart()
    {
        _waitingForRestart = false;
    }

    // ── Feedback ─────────────────────────────────────────────────────────────

    public void ShowFeedback(string message, bool? correct = null)
    {
        if (feedbackPanel) feedbackPanel.SetActive(true);
        if (feedbackText)
        {
            feedbackText.text  = message;
            feedbackText.color = correct == null ? neutralColor
                               : correct.Value   ? correctColor
                                                 : incorrectColor;
        }
    }

    public void HideFeedback()
    {
        if (feedbackPanel) feedbackPanel.SetActive(false);
    }

    // ── AOIs ─────────────────────────────────────────────────────────────────

    public void SetupAOIs(string[] options, string correctAnswer)
    {
        if (aois == null || aois.Length == 0) return;

        string[] shuffled = (string[])options.Clone();
        ShuffleArray(shuffled);

        for (int i = 0; i < aois.Length; i++)
        {
            if (i < shuffled.Length)
            {
                aois[i].gameObject.SetActive(true);
                aois[i].SetSprite(null);
                aois[i].SetContent(shuffled[i]);
                aois[i].aoiID           = shuffled[i];
                aois[i].isCorrectAnswer = (shuffled[i] == correctAnswer);
                aois[i].ResetData();
            }
            else
            {
                aois[i].gameObject.SetActive(false);
            }
        }
    }

    public void SetupAOIs(Sprite[] sprites, Sprite correctSprite)
    {
        if (aois == null || aois.Length == 0) return;

        Sprite[] shuffled = (Sprite[])sprites.Clone();
        ShuffleArray(shuffled);

        for (int i = 0; i < aois.Length; i++)
        {
            if (i < shuffled.Length)
            {
                aois[i].gameObject.SetActive(true);
                aois[i].SetSprite(shuffled[i]);
                aois[i].aoiID           = shuffled[i].name;
                aois[i].isCorrectAnswer = (shuffled[i] == correctSprite);
                aois[i].ResetData();
            }
            else
            {
                aois[i].gameObject.SetActive(false);
            }
        }
    }

    public void ShowAOIs(bool visible)
    {
        if (aoiGrid != null) aoiGrid.gameObject.SetActive(visible);
    }

    public AOI GetCorrectAOI()
    {
        if (aois == null) return null;
        foreach (var aoi in aois)
            if (aoi.isCorrectAnswer) return aoi;
        return null;
    }

    public AOI[] GetAllAOIs() => aois;

    // ── Utilities ────────────────────────────────────────────────────────────

    private void ShuffleArray<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
