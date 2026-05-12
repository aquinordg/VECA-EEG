using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Classe base abstrata para todas as tarefas cognitivas do VECA.
///
/// CICLO DE VIDA DE UM TRIAL:
///   1. StartTask() → dispara OnTaskStarted
///   2. FasePreparacao() → exibe instrução pelo tempo preparationTime
///   3. SetupTrial()     → subclasse configura as AOIs/estímulos
///   4. FaseExecucao()   → EyeTracker grava por executionTime segundos
///   5. CalculateScore() → subclasse calcula a feature (0-1)
///   6. OnTaskCompleted(featureName, score) disparado
///
/// CONFIGURAÇÃO NO INSPECTOR (componentes necessários na cena):
///   - UIManager  (qualquer objeto na cena)
///   - EyeTracker (qualquer objeto na cena)
/// </summary>
public abstract class TaskBase : MonoBehaviour
{
    [Header("Configuração Geral")]
    public string taskName       = "Tarefa";
    public float preparationTime = 3f;   // tempo da instrução antes da execução
    public float executionTime   = 8f;   // tempo de gravação do olhar

    [Header("Eventos")]
    [Tooltip("Disparado ao iniciar a tarefa")]
    public UnityEvent OnTaskStarted;
    [Tooltip("Disparado ao concluir: string = featureName, float = score (0-1)")]
    public UnityEvent<string, float> OnTaskCompleted;

    protected UIManager  uiManager;
    protected EyeTracker eyeTracker;

    public bool  IsRunning { get; private set; }
    public float LastScore { get; private set; }

    // Quando true, a instrução permanece visível durante a fase de execução
    protected bool mostrarInstrucaoNaExecucao = false;

    protected virtual void Awake()
    {
        uiManager  = FindFirstObjectByType<UIManager>();
        eyeTracker = FindFirstObjectByType<EyeTracker>();
    }

    // ── Ponto de entrada principal ───────────────────────────────────────────

    public void StartTask()
    {
        if (!IsRunning)
            StartCoroutine(SequenciaDaTarefa());
    }

    // ── Sequência interna ────────────────────────────────────────────────────

    private IEnumerator SequenciaDaTarefa()
    {
        IsRunning = true;
        OnTaskStarted?.Invoke();
        uiManager.SetTaskStatus(taskName);

        yield return StartCoroutine(FasePreparacao());

        SetupTrial();

        yield return StartCoroutine(FaseExecucao());

        float score = CalculateScore();
        LastScore   = score;   // salvar antes de IsRunning = false para evitar race condition

        IsRunning = false;
        OnTaskCompleted?.Invoke(GetFeatureName(), score);
    }

    // ── Fases (podem ser sobrescritas por subclasses) ────────────────────────

    protected virtual IEnumerator FasePreparacao()
    {
        uiManager.ShowInstruction(GetInstructionText());

        float decorrido = 0f;
        while (decorrido < preparationTime)
        {
            decorrido += Time.deltaTime;
            uiManager.UpdateTimer(preparationTime - decorrido);
            yield return null;
        }

        uiManager.HideInstruction();
    }

    protected virtual IEnumerator FaseExecucao()
    {
        AOI aoiCorreta = uiManager.GetCorrectAOI();
        if (aoiCorreta != null)
            eyeTracker.SetCurrentCorrectAOI(aoiCorreta);

        if (mostrarInstrucaoNaExecucao)
            uiManager.ShowInstruction(GetInstructionText());

        eyeTracker.StartRecording();

        float decorrido = 0f;
        while (decorrido < executionTime)
        {
            decorrido += Time.deltaTime;
            uiManager.UpdateTimer(executionTime - decorrido);
            yield return null;
        }

        eyeTracker.StopRecording();

        if (mostrarInstrucaoNaExecucao)
            uiManager.HideInstruction();
    }

    // ── Contrato para subclasses ─────────────────────────────────────────────

    /// <summary>Configura AOIs e estímulos para o trial.</summary>
    protected abstract void SetupTrial();

    /// <summary>
    /// Calcula o score do trial. Padrão VECA:
    /// % tempo na AOI correta / tempo total de gravação (0–1).
    /// </summary>
    protected abstract float CalculateScore();

    /// <summary>Nome da feature para o CSV (ex: "vr_mem8").</summary>
    protected abstract string GetFeatureName();

    /// <summary>Texto exibido no InstructionPanel antes do trial.</summary>
    protected abstract string GetInstructionText();
}
