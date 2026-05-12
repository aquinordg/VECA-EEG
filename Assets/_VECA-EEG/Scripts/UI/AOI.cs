using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AOI : MonoBehaviour
{
    [Header("Identificação")]
    public string aoiID;
    public bool isCorrectAnswer = false;
    
    [Header("Dados de Fixação")]
    public float totalFixationTime = 0f;
    public int fixationCount = 0;
    public float firstFixationTime = -1f;
    public bool wasLookedAt = false;
    
    [Header("Componentes")]
    [Tooltip("Imagem filha para exibir sprites (opcional). Arraste o filho 'AOI_Image' aqui.")]
    public Image aoiImage;
    private Image backgroundImage;
    private Button button;
    private TextMeshProUGUI labelText;

    void Start()
    {
        backgroundImage = GetComponent<Image>();
        button = GetComponent<Button>();
        labelText = GetComponentInChildren<TextMeshProUGUI>();
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
    /// Exibe um sprite na AOI e oculta o texto.
    /// Passe null para voltar ao modo texto.
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        bool usarImagem = sprite != null;

        if (aoiImage != null)
        {
            aoiImage.sprite = sprite;
            aoiImage.gameObject.SetActive(usarImagem);
        }

        if (labelText != null)
            labelText.gameObject.SetActive(!usarImagem);
    }
    
    public void Highlight()
    {
        if (backgroundImage != null)
            backgroundImage.color = new Color(1f, 0.8f, 0f); // Amarelo
    }
    
    public void Unhighlight()
    {
        if (backgroundImage != null)
            backgroundImage.color = new Color(0.88f, 0.88f, 0.88f); // Cinza
    }
    
    public void MarkAsCorrect()
    {
        if (backgroundImage != null)
            backgroundImage.color = new Color(0f, 1f, 0f); // Verde
        isCorrectAnswer = true;
    }
    
    public void MarkAsIncorrect()
    {
        if (backgroundImage != null)
            backgroundImage.color = new Color(1f, 0.2f, 0.2f); // Vermelho
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