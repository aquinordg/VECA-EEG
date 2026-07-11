using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AOI : MonoBehaviour
{
    [Header("Identification")]
    public string aoiID;
    public bool isCorrectAnswer = false;

    [Header("Fixation Data")]
    public float totalFixationTime = 0f;
    public int fixationCount = 0;
    public float firstFixationTime = -1f;
    public bool wasLookedAt = false;

    [Header("Components")]
    [Tooltip("Child image for displaying sprites (optional). Drag the 'AOI_Image' child here.")]
    public Image aoiImage;
    private Image backgroundImage;
    private Button button;
    private TextMeshProUGUI labelText;

    void Start()
    {
        backgroundImage = GetComponent<Image>();
        button = GetComponent<Button>();
        labelText = GetComponentInChildren<TextMeshProUGUI>();

        if (labelText != null)
        {
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin      = 10f;
            labelText.fontSizeMax      = labelText.fontSize;
            labelText.enableWordWrapping = true;
        }
    }

    public void SetContent(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }

    public string GetContent()
    {
        return labelText != null ? labelText.text : "";
    }

    /// <summary>
    /// Displays a sprite on the AOI and hides the text.
    /// Pass null to return to text mode.
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        bool useImage = sprite != null;

        if (aoiImage != null)
        {
            aoiImage.sprite = sprite;
            aoiImage.gameObject.SetActive(useImage);
        }

        if (labelText != null)
            labelText.gameObject.SetActive(!useImage);
    }

    public void Highlight()
    {
        if (backgroundImage != null)
            backgroundImage.color = new Color(1f, 0.8f, 0f); // Yellow
    }

    public void Unhighlight()
    {
        if (backgroundImage != null)
            backgroundImage.color = new Color(0.88f, 0.88f, 0.88f); // Gray
    }

    public void MarkAsCorrect()
    {
        if (backgroundImage != null)
            backgroundImage.color = new Color(0f, 1f, 0f); // Green
        isCorrectAnswer = true;
    }

    public void MarkAsIncorrect()
    {
        if (backgroundImage != null)
            backgroundImage.color = new Color(1f, 0.2f, 0.2f); // Red
        isCorrectAnswer = false;
    }

    public void ResetData()
    {
        totalFixationTime = 0f;
        fixationCount = 0;
        firstFixationTime = -1f;
        wasLookedAt = false;
        Unhighlight();
    }
}
