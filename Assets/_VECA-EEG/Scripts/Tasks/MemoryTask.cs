using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarefa de Memória — replica o protocolo do artigo VECA.
///
/// PROTOCOLO POR TRIAL:
///   Encoding  (encodingTime s) → mostra a imagem/rótulo alvo isolado
///   Storage   (storageDelay s) → tela em branco (intervalo de retenção)
///   Recall    (executionTime s)→ 4 AOIs na tela; participante fixa na correta
///
/// FEATURES GERADAS:
///   Trial 0 → vr_mem8  |  Trial 1 → vr_mem9  |  Trial 2 → vr_mem10
///
/// MODO SPRITE (recomendado):
///   Sprites → lista única com todos os sprites disponíveis (mínimo 4).
///   A cada sessão, 3 são sorteados como alvos e o restante vira distratores.
///
/// MODO TEXTO (fallback — quando Sprites está vazio):
///   TargetLabels     → 3 rótulos dos alvos
///   DistractorLabels → pool de rótulos distratores
/// </summary>
public class MemoryTask : TaskBase
{
    [Header("Estímulos — Modo Sprite")]
    [Tooltip("Pool único de sprites. Mínimo 4: 3 serão sorteados como alvos, o resto como distratores.")]
    public Sprite[] sprites;

    [Header("Estímulos — Modo Texto (fallback)")]
    [Tooltip("Rótulos dos 3 alvos (usado quando Sprites está vazio)")]
    public string[] targetLabels = { "Leão", "Rinoceronte", "Camelo" };

    [Tooltip("Pool de rótulos distratores")]
    public string[] distractorLabels = { "Elefante", "Zebra", "Girafa", "Tigre", "Lobo" };

    [Header("Ordem")]
    [Tooltip("Sorteia os alvos e embaralha a ordem de apresentação a cada sessão")]
    public bool aleatorio = true;

    [Header("Display de Encoding")]
    [Tooltip("GameObject exibido apenas durante a fase de encoding")]
    public GameObject encodingDisplay;

    [Tooltip("Image dentro do encodingDisplay")]
    public Image encodingImage;

    [Header("Tempos")]
    [Tooltip("Duração da exibição do alvo na fase de encoding (s)")]
    public float encodingTime = 4f;
    [Tooltip("Intervalo entre encoding e recall — tela em branco (s)")]
    public float storageDelay = 3f;
    [Tooltip("Pausa entre trials (s)")]
    public float pausaEntreTrials = 0.8f;

    // ── Estado interno ───────────────────────────────────────────────────────

    private int trialAtual;
    private readonly float[]          scores          = new float[3];
    private readonly System.DateTime[] _trialStartTimes = new System.DateTime[3];
    private readonly System.DateTime[] _trialEndTimes   = new System.DateTime[3];

    // Arrays de runtime calculados em InicializarOrdem()
    private string[] _labelsAtivos;       // 3 rótulos na ordem desta sessão
    private Sprite[] _spritesAtivos;      // 3 sprites sorteados como alvos
    private Sprite[] _distractoresAtivos; // sprites restantes (distratores)

    private static readonly string[] nomesFeature = { "vr_mem8", "vr_mem9", "vr_mem10" };

    protected override void Awake()
    {
        base.Awake();
        taskName      = "MEMÓRIA";
        executionTime = 8f;
        if (string.IsNullOrWhiteSpace(taskDescription))
            taskDescription =
                "<b>TAREFA:</b> MEMÓRIA\n\n" +
                "Você verá 3 imagens, uma de cada vez, por alguns segundos.\n" +
                "Após um breve intervalo, a mesma imagem aparecerá misturada\n" +
                "com outras 3 opções — fixe o olhar nela.\n\n" +
                "<b>Exemplo:</b> se você viu um Leão, olhe para o Leão quando ele\n" +
                "aparecer entre as 4 opções.\n\n" +
                "Esta tarefa tem 3 rodadas.";
        InicializarOrdem(embaralhar: false);
    }

    // ── Getters públicos (usados pelo RecallTask) ─────────────────────────────

    public string   GetTargetLabel(int idx)    => _labelsAtivos    != null && idx < _labelsAtivos.Length    ? _labelsAtivos[idx]    : null;
    public Sprite   GetTargetSprite(int idx)   => _spritesAtivos   != null && idx < _spritesAtivos.Length   ? _spritesAtivos[idx]   : null;
    public Sprite[] GetDistractorSprites()     => _distractoresAtivos;
    public (System.DateTime start, System.DateTime end) GetTrialTimes(int idx) =>
        (_trialStartTimes[idx], _trialEndTimes[idx]);

    // ── API Pública ──────────────────────────────────────────────────────────

    public IEnumerator RunAllTrials()
    {
        if (Loc?.memoryTargetLabels?.Length >= 3)
            targetLabels = Loc.memoryTargetLabels;
        if (Loc?.memoryDistractorLabels?.Length > 0)
            distractorLabels = Loc.memoryDistractorLabels;

        yield return StartCoroutine(IntroPhase());

        InicializarOrdem(embaralhar: aleatorio);

        for (int i = 0; i < 3; i++)
        {
            trialAtual = i;
            yield return StartCoroutine(ExecutarUmTrial(i));
            yield return new WaitForSeconds(pausaEntreTrials);
        }
    }

    public float GetTrialScore(int index) =>
        index >= 0 && index < scores.Length ? scores[index] : 0f;

    // ── Inicialização da ordem ────────────────────────────────────────────────

    private void InicializarOrdem(bool embaralhar)
    {
        // Modo texto
        int[] idxTexto = { 0, 1, 2 };
        if (embaralhar) Embaralhar(idxTexto);
        _labelsAtivos = new string[3];
        for (int i = 0; i < 3; i++)
            _labelsAtivos[i] = targetLabels.Length > idxTexto[i] ? targetLabels[idxTexto[i]] : "";

        // Modo sprite
        if (sprites == null || sprites.Length < 4)
        {
            _spritesAtivos      = null;
            _distractoresAtivos = null;
            return;
        }

        // Montar pool de índices disponíveis
        var pool = new List<int>();
        for (int i = 0; i < sprites.Length; i++) pool.Add(i);

        // Sortear 3 alvos
        int[] alvos = new int[3];
        for (int i = 0; i < 3; i++)
        {
            int pick = embaralhar ? Random.Range(0, pool.Count) : i;
            alvos[i] = pool[pick];
            pool.RemoveAt(pick);
        }

        // Embaralhar a ordem de apresentação dos alvos entre si
        if (embaralhar) Embaralhar(alvos);

        _spritesAtivos = new Sprite[3];
        for (int i = 0; i < 3; i++) _spritesAtivos[i] = sprites[alvos[i]];

        _distractoresAtivos = new Sprite[pool.Count];
        for (int i = 0; i < pool.Count; i++) _distractoresAtivos[i] = sprites[pool[i]];
    }

    private static void Embaralhar(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    // ── Trial Completo ───────────────────────────────────────────────────────

    private IEnumerator ExecutarUmTrial(int idx)
    {
        uiManager.SetTaskStatus($"{Loc?.taskMemory ?? taskName} ({idx + 1}/3)");

        // ── FASE 1: ENCODING ─────────────────────────────────────────────────
        MostrarEncodingDisplay(idx, true);
        uiManager.ShowAOIs(false);
        uiManager.ShowInstruction(Loc?.memoryEncodePrompt ?? "Memorize esta imagem", encodingTime);

        yield return new WaitForSeconds(encodingTime);

        uiManager.HideInstruction();
        MostrarEncodingDisplay(idx, false);

        // ── FASE 2: STORAGE ──────────────────────────────────────────────────
        uiManager.ShowInstruction("\n\n...", storageDelay);
        yield return new WaitForSeconds(storageDelay);
        uiManager.HideInstruction();

        // ── FASE 3: RECALL ───────────────────────────────────────────────────
        ConfigurarAOIsDeRecall(idx);
        uiManager.ShowInstruction(Loc?.memoryRecallPrompt ?? "\n\nFixe o olhar na resposta correta.");

        AOI aoiCorreta = uiManager.GetCorrectAOI();
        eyeTracker.SetCurrentCorrectAOI(aoiCorreta);
        eyeTracker.StartRecording();

        float decorrido = 0f;
        while (decorrido < executionTime)
        {
            decorrido += Time.deltaTime;
            uiManager.UpdateTimer(executionTime - decorrido);
            yield return null;
        }

        eyeTracker.StopRecording();
        _trialStartTimes[idx] = eyeTracker.RecordingStartTime;
        _trialEndTimes[idx]   = eyeTracker.RecordingEndTime;
        uiManager.HideInstruction();

        scores[idx] = eyeTracker.GetCorrectAOIPercentage();

        uiManager.ShowAOIs(false);

        bool acertou = scores[idx] >= 0.5f;
        uiManager.ShowFeedback(FormatFeedback(scores[idx]), acertou);

        yield return new WaitForSeconds(1.5f);
        uiManager.HideFeedback();
    }

    // ── Setup das AOIs de Recall ─────────────────────────────────────────────

    private void ConfigurarAOIsDeRecall(int idx)
    {
        if (_spritesAtivos != null)
            ConfigurarRecallComSprites(idx);
        else
            ConfigurarRecallComTexto(_labelsAtivos[idx]);

        uiManager.ShowAOIs(true);
    }

    private void ConfigurarRecallComSprites(int idx)
    {
        Sprite alvo = _spritesAtivos[idx];

        // Pool: distratores dedicados + outros alvos
        var pool = new List<Sprite>();
        if (_distractoresAtivos != null) pool.AddRange(_distractoresAtivos);
        foreach (var s in _spritesAtivos)
            if (s != alvo && !pool.Contains(s)) pool.Add(s);

        var opcoes = new List<Sprite> { alvo };
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int r = Random.Range(0, pool.Count);
            opcoes.Add(pool[r]);
            pool.RemoveAt(r);
        }

        uiManager.SetupAOIs(opcoes.ToArray(), alvo);
    }

    private void ConfigurarRecallComTexto(string alvo)
    {
        var pool = new List<string>(distractorLabels);
        foreach (var lbl in _labelsAtivos) pool.Remove(lbl);
        foreach (var lbl in _labelsAtivos)
            if (lbl != alvo && !pool.Contains(lbl)) pool.Add(lbl);

        var opcoes = new List<string> { alvo };
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int r = Random.Range(0, pool.Count);
            opcoes.Add(pool[r]);
            pool.RemoveAt(r);
        }

        uiManager.SetupAOIs(opcoes.ToArray(), alvo);
    }

    // ── Display de Encoding ──────────────────────────────────────────────────

    private void MostrarEncodingDisplay(int idx, bool ativo)
    {
        if (encodingDisplay == null) return;
        encodingDisplay.SetActive(ativo);

        if (!ativo || encodingImage == null) return;
        if (_spritesAtivos != null && idx < _spritesAtivos.Length)
            encodingImage.sprite = _spritesAtivos[idx];
    }

    // ── Implementação de TaskBase ────────────────────────────────────────────

    protected override void   SetupTrial()      => ConfigurarAOIsDeRecall(trialAtual);
    protected override float  CalculateScore()  => eyeTracker.GetCorrectAOIPercentage();
    protected override string GetTaskName()    => Loc?.taskMemory ?? taskName;
    protected override string GetDescription() => L(Loc?.descMemory, taskDescription);
    protected override string GetFeatureName() => trialAtual < nomesFeature.Length ? nomesFeature[trialAtual] : "vr_mem";
    protected override string GetInstructionText() => $"Onde estava: <b>{_labelsAtivos[trialAtual]}</b>?";
}
