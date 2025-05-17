using UnityEngine;
using UnityEngine.UI;

public class BarrelUIController : MonoBehaviour
{
    [Header("References")]
    public RectTransform itemHighlight; // Retângulo que envolve o item interagível
    public GameObject clickIndicator;   // Ícone do cursor que aparece ao passar o mouse sobre o item
    public Button closeButton;          // Botão para fechar a UI do barril
    
    [Header("Highlight Settings")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.3f);    // Cor normal do highlight
    public Color hoverColor = new Color(1f, 1f, 0.5f, 0.5f);   // Cor do highlight quando o mouse está sobre o item
    
    private Image highlightImage;
    private BarrelInteraction barrelInteraction;
    
    private void Awake()
    {
        // Pegar a referência ao componente de interação do barril
        barrelInteraction = GetComponentInParent<BarrelInteraction>();
        
        // Inicializar componentes
        if (itemHighlight != null)
        {
            highlightImage = itemHighlight.GetComponent<Image>();
            if (highlightImage == null)
            {
                // Adicionar um componente Image se não existir
                highlightImage = itemHighlight.gameObject.AddComponent<Image>();
                
                // Configura o Image Type como Sliced para bordas arredondadas
                // Mesmo sem uma imagem específica, podemos usar o sprite padrão da Unity
                highlightImage.type = Image.Type.Sliced;
                
                // Pegar o sprite padrão da Unity (branco)
                Sprite defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                highlightImage.sprite = defaultSprite;
                
                highlightImage.color = normalColor;
                highlightImage.raycastTarget = false; // Não bloqueará raycasts
            }
            else
            {
                // Se já existe um componente Image, configurar o tipo
                highlightImage.type = Image.Type.Sliced;
                
                // Se não houver sprite, usar o padrão
                if (highlightImage.sprite == null)
                {
                    Sprite defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                    highlightImage.sprite = defaultSprite;
                }
            }
        }
        
        // Configurar o botão de fechar
        if (closeButton != null && barrelInteraction != null)
        {
            closeButton.onClick.AddListener(barrelInteraction.CloseBarrel);
        }
        
        // Inicializar estados
        if (clickIndicator != null)
            clickIndicator.SetActive(false);
            
        if (itemHighlight != null)
            itemHighlight.gameObject.SetActive(false);
    }
    
    // Este método pode ser chamado pelo BarrelInteraction quando o mouse passar sobre o item
    public void SetHighlightHoverState(bool isHovering)
    {
        if (highlightImage != null)
        {
            highlightImage.color = isHovering ? hoverColor : normalColor;
        }
    }
    
    // Este método pode ser chamado pelo BarrelInteraction para mostrar/esconder o indicador de clique
    public void ShowClickIndicator(bool show, Vector2 position)
    {
        if (clickIndicator != null)
        {
            clickIndicator.SetActive(show);
            
            if (show)
            {
                RectTransform rectTransform = clickIndicator.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = position;
                }
            }
        }
    }
}