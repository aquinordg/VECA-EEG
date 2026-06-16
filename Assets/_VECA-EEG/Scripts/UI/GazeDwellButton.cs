using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ativa um Button quando o gaze permanece sobre ele por <see cref="dwellTime"/> segundos.
/// Adicione este componente a qualquer Button de navegação (Iniciar, Entendido, Nova Avaliação).
/// </summary>
public class GazeDwellButton : MonoBehaviour
{
    public EyeTracker eyeTracker;

    [Tooltip("Segundos de gaze contínuo para ativar o botão")]
    public float dwellTime = 1.5f;

    [Tooltip("Imagem de progresso (Image com Image Type = Filled). Opcional.")]
    public Image fillImage;

    [Header("Gaze Highlight")]
    [Tooltip("Cor do botão em repouso")]
    public Color normalColor = Color.white;
    [Tooltip("Cor do botão quando o gaze está sobre ele")]
    public Color gazeColor   = new Color(1f, 0.92f, 0.016f);

    private Button        button;
    private RectTransform rectTransform;
    private Image         buttonImage;
    private float         gazeTimer;
    private bool          activated;

    void Awake()
    {
        button        = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        buttonImage   = GetComponent<Image>();
    }

    void OnEnable()
    {
        gazeTimer = 0f;
        activated = false;
        SetFill(0f);
        SetHighlight(false);
    }

    void Update()
    {
        if (activated || button == null || !button.interactable) return;

        Vector2 gazePos = eyeTracker.ObterPosicaoGaze();
        bool gazeOverButton = RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform, gazePos, eyeTracker.vrCamera != null ? eyeTracker.vrCamera : Camera.main);

        if (gazeOverButton)
        {
            SetHighlight(true);
            gazeTimer += Time.deltaTime;
            SetFill(gazeTimer / dwellTime);

            if (gazeTimer >= dwellTime)
            {
                activated = true;
                SetFill(0f);
                SetHighlight(false);
                button.onClick.Invoke();
            }
        }
        else
        {
            SetHighlight(false);
            gazeTimer = 0f;
            SetFill(0f);
        }
    }

    private void SetFill(float value)
    {
        if (fillImage != null)
            fillImage.fillAmount = value;
    }

    private void SetHighlight(bool on)
    {
        if (buttonImage != null)
            buttonImage.color = on ? gazeColor : normalColor;
    }
}
