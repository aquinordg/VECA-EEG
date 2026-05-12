using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia todos os elementos visuais da interface durante os testes cognitivos.
///
/// CONFIGURAÇÃO NO INSPECTOR:
///   - TaskStatusText  → TMP no HeaderPanel (ex: "Memória – Trial 1")
///   - TimerText       → TMP no HeaderPanel (ex: "00:08")
///   - InstructionText → TMP dentro do InstructionPanel
///   - FeedbackText    → TMP dentro do FeedbackPanel
///   - InstructionPanel → GameObject com o painel de instruções
///   - FeedbackPanel    → GameObject com o painel de feedback
///   - AoiGrid          → Transform pai das 4 AOIs no WorldCanvas
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Textos")]
    public TextMeshProUGUI taskStatusText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI feedbackText;

    [Header("Painéis")]
    public GameObject instructionPanel;
    public GameObject feedbackPanel;

    [Header("Grid de AOIs")]
    [Tooltip("Transform pai que contém as 4 AOIs")]
    public Transform aoiGrid;

    [Header("Cores de Feedback")]
    public Color corCorreta  = new Color(0.10f, 0.80f, 0.20f);
    public Color corErrada   = new Color(0.90f, 0.15f, 0.15f);
    public Color corNeutral  = new Color(0.20f, 0.60f, 1.00f);

    // Cache das AOIs filhas do grid
    private AOI[] aois;
    private Coroutine corotinaInstrucao;

    void Awake()
    {
        if (feedbackPanel)    feedbackPanel.SetActive(false);
        if (instructionPanel) instructionPanel.SetActive(false);

        // Pré-carregar AOIs enquanto o contexto de GUI ainda não existe
        if (aoiGrid != null)
            aois = aoiGrid.GetComponentsInChildren<AOI>(true); // true = inclui inativos
    }

    void Start()
    {
        // SetActive separado do Awake para evitar redraw do Inspector antes do contexto de GUI
        if (aoiGrid != null) aoiGrid.gameObject.SetActive(false);
    }

    // ── Status e Timer ───────────────────────────────────────────────────────

    public void SetTaskStatus(string texto)
    {
        if (taskStatusText) taskStatusText.text = texto;
    }

    /// <param name="segundos">Segundos restantes; formata como MM:SS</param>
    public void UpdateTimer(float segundos)
    {
        if (!timerText) return;
        segundos = Mathf.Max(0f, segundos); // evita exibir tempo negativo no último frame
        int min = Mathf.FloorToInt(segundos / 60f);
        int seg = Mathf.FloorToInt(segundos % 60f);
        timerText.text = $"{min:00}:{seg:00}";
    }

    // ── Instruções ───────────────────────────────────────────────────────────

    /// <param name="texto">Mensagem exibida no InstructionPanel</param>
    /// <param name="duracao">0 = permanece até HideInstruction() ser chamado</param>
    public void ShowInstruction(string texto, float duracao = 0f)
    {
        if (corotinaInstrucao != null) StopCoroutine(corotinaInstrucao);

        if (instructionPanel) instructionPanel.SetActive(true);
        if (instructionText)  instructionText.text = texto;

        if (duracao > 0f)
            corotinaInstrucao = StartCoroutine(OcultarInstrucaoApos(duracao));
    }

    public void HideInstruction()
    {
        if (corotinaInstrucao != null)
        {
            StopCoroutine(corotinaInstrucao);
            corotinaInstrucao = null;
        }
        if (instructionPanel) instructionPanel.SetActive(false);
    }

    private IEnumerator OcultarInstrucaoApos(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInstruction();
    }

    // ── Feedback ─────────────────────────────────────────────────────────────

    /// <param name="mensagem">Texto exibido no FeedbackPanel</param>
    /// <param name="correto">true = verde, false = vermelho, null = azul neutro</param>
    public void ShowFeedback(string mensagem, bool? correto = null)
    {
        if (feedbackPanel) feedbackPanel.SetActive(true);
        if (feedbackText)
        {
            feedbackText.text = mensagem;
            feedbackText.color = correto == null  ? corNeutral
                               : correto.Value    ? corCorreta
                                                  : corErrada;
        }
    }

    public void HideFeedback()
    {
        if (feedbackPanel) feedbackPanel.SetActive(false);
    }

    // ── AOIs ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Modo texto: distribui rótulos pelas AOIs embaralhando a posição.
    /// </summary>
    public void SetupAOIs(string[] opcoes, string respostaCorreta)
    {
        if (aois == null || aois.Length == 0) return;

        string[] embaralhadas = (string[])opcoes.Clone();
        EmbaralharArray(embaralhadas);

        for (int i = 0; i < aois.Length; i++)
        {
            if (i < embaralhadas.Length)
            {
                aois[i].gameObject.SetActive(true);
                aois[i].SetSprite(null);               // garante modo texto
                aois[i].SetContent(embaralhadas[i]);
                aois[i].aoiID           = embaralhadas[i];
                aois[i].isCorrectAnswer = (embaralhadas[i] == respostaCorreta);
                aois[i].ResetData();
            }
            else
            {
                aois[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Modo imagem: distribui sprites pelas AOIs embaralhando a posição.
    /// Usa o nome do sprite como aoiID.
    /// </summary>
    public void SetupAOIs(Sprite[] sprites, Sprite spriteCorreto)
    {
        if (aois == null || aois.Length == 0) return;

        Sprite[] embaralhados = (Sprite[])sprites.Clone();
        EmbaralharArray(embaralhados);

        for (int i = 0; i < aois.Length; i++)
        {
            if (i < embaralhados.Length)
            {
                aois[i].gameObject.SetActive(true);
                aois[i].SetSprite(embaralhados[i]);
                aois[i].aoiID           = embaralhados[i].name;
                aois[i].isCorrectAnswer = (embaralhados[i] == spriteCorreto);
                aois[i].ResetData();
            }
            else
            {
                aois[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Ativa ou desativa o AOIGrid inteiro.
    /// Ao ativar, os filhos já estão configurados por SetupAOIs() e aparecem no estado correto.
    /// </summary>
    public void ShowAOIs(bool visivel)
    {
        if (aoiGrid != null) aoiGrid.gameObject.SetActive(visivel);
    }

    /// <summary>Retorna a AOI marcada como resposta correta, ou null.</summary>
    public AOI GetCorrectAOI()
    {
        if (aois == null) return null;
        foreach (var aoi in aois)
            if (aoi.isCorrectAnswer) return aoi;
        return null;
    }

    public AOI[] GetAllAOIs() => aois;

    // ── Utilitários ──────────────────────────────────────────────────────────

    private void EmbaralharArray<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
