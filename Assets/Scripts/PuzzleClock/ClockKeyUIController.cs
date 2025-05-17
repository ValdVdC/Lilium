using UnityEngine;
using UnityEngine.UI;

public class ClockKeyUIController : MonoBehaviour
{
    [Header("Referencias UI")]
    public RectTransform keyHighlight;        // Retângulo que envolve a chave
    public GameObject clickIndicator;         // Ícone do cursor quando passa sobre a chave
    
    [Header("Configurações do Highlight")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.3f);    // Cor normal
    public Color hoverColor = new Color(1f, 1f, 0.5f, 0.5f);   // Cor quando hover
    
    [Header("Debug")]
    public bool showDebugLogs = true;         // Controla se os logs de debug serão mostrados
    
    private Image highlightImage;
    private ClockPuzzleManager clockPuzzleManager;
    
    private void Awake()
    {
        // Obter referência ao gerenciador do puzzle
        clockPuzzleManager = GetComponentInParent<ClockPuzzleManager>();
        
        // Inicializar o highlight
        if (keyHighlight != null)
        {
            highlightImage = keyHighlight.GetComponent<Image>();
            if (highlightImage == null)
            {
                // Adicionar o componente se não existir
                highlightImage = keyHighlight.gameObject.AddComponent<Image>();
                
                // Configurar o tipo como Sliced para bordas arredondadas
                highlightImage.type = Image.Type.Sliced;
                
                // Usar o sprite padrão da Unity
                Sprite defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                highlightImage.sprite = defaultSprite;
                
                highlightImage.color = normalColor;
                highlightImage.raycastTarget = false; // Não bloqueia raycasts
                
                DebugLog("Highlight Image criado e configurado");
            }
        }
        else
        {
            Debug.LogError("[CLOCK KEY UI] keyHighlight não está atribuído no Inspector!");
        }
        
        // Configurar o indicador de clique
        if (clickIndicator != null)
        {
            // Garantir que o indicador de clique esteja configurado corretamente
            Image clickImage = clickIndicator.GetComponent<Image>();
            if (clickImage == null)
            {
                clickImage = clickIndicator.AddComponent<Image>();
                // Usar um sprite de cursor padrão
                clickImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
                clickImage.raycastTarget = false;
                DebugLog("Indicador de clique configurado");
            }
        }
        else
        {
            Debug.LogError("[CLOCK KEY UI] clickIndicator não está atribuído no Inspector!");
        }
        
        // Esconder elementos inicialmente
        if (clickIndicator != null)
            clickIndicator.SetActive(false);
            
        if (keyHighlight != null)
            keyHighlight.gameObject.SetActive(false);
            
        DebugLog("ClockKeyUIController inicializado");
    }
    
    private void OnEnable()
    {
        DebugLog("ClockKeyUIController ativado");
    }
    
    // Atualiza o estado visual do highlight baseado no hover
    public void SetHighlightHoverState(bool isHovering)
    {
        if (highlightImage != null)
        {
            highlightImage.color = isHovering ? hoverColor : normalColor;
            DebugLog("Highlight estado atualizado: " + (isHovering ? "Hover" : "Normal"));
        }
        else
        {
            Debug.LogError("[CLOCK KEY UI] highlightImage é nulo em SetHighlightHoverState");
        }
    }
    
    // Mostra/esconde o indicador de clique na posição especificada
    public void ShowClickIndicator(bool show, Vector2 position)
    {
        if (clickIndicator != null)
        {
            if (show != clickIndicator.activeSelf)
            {
                clickIndicator.SetActive(show);
                DebugLog("Indicador de clique: " + (show ? "Mostrado" : "Escondido"));
            }
            
            if (show)
            {
                RectTransform rectTransform = clickIndicator.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = position;
                    DebugLog("Indicador de clique posicionado em: " + position);
                }
                else
                {
                    Debug.LogError("[CLOCK KEY UI] RectTransform do clickIndicator é nulo!");
                }
            }
        }
        else
        {
            Debug.LogError("[CLOCK KEY UI] clickIndicator é nulo em ShowClickIndicator");
        }
    }
    
    // Ativa a UI de interação da chave
    public void ShowKeyInteractionUI()
    {
        if (keyHighlight != null)
        {
            keyHighlight.gameObject.SetActive(true);
            DebugLog("UI de interação da chave ativada");
        }
        else
        {
            Debug.LogError("[CLOCK KEY UI] keyHighlight é nulo em ShowKeyInteractionUI");
        }
    }
    
    // Desativa a UI de interação da chave
    public void HideKeyInteractionUI()
    {
        if (keyHighlight != null)
        {
            keyHighlight.gameObject.SetActive(false);
            DebugLog("UI de interação da chave desativada");
        }
        
        if (clickIndicator != null)
        {
            clickIndicator.SetActive(false);
            DebugLog("Indicador de clique desativado");
        }
    }
    
    // Método para logs de debug condicional
    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("[CLOCK KEY UI] " + message);
        }
    }
}