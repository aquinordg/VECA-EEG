using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Activates a Button when gaze dwells on it for <see cref="dwellTime"/> seconds.
/// Add this component to any navigation button (Start, Got It, New Assessment).
/// </summary>
public class GazeDwellButton : MonoBehaviour
{
    public EyeTracker eyeTracker;

    [Tooltip("Seconds of continuous gaze required to activate the button")]
    public float dwellTime = 1.5f;

    [Tooltip("Progress image (Image with Image Type = Filled). Optional.")]
    public Image fillImage;

    [Header("Gaze Highlight")]
    [Tooltip("Button color at rest")]
    public Color normalColor = Color.white;
    [Tooltip("Button color when gaze is over it")]
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

        Vector2 gazePos = eyeTracker.GetGazePosition();
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
