using UnityEngine;

public class BookItem : MonoBehaviour
{
    [Header("Configurações Visuais")]
    public SpriteRenderer bookSprite;           // Renderizador do sprite do livro
    public Color normalColor = Color.white;     // Cor normal do livro
    public Color highlightColor = new Color(1f, 1f, 0.7f, 1f);  // Cor quando destacado pelo cursor
    public Color selectedColor = new Color(0.7f, 1f, 0.7f, 1f); // Cor quando selecionado
    public float normalScale = 1.0f;            // Escala normal
    public float highlightedScale = 1.1f;       // Escala quando destacado pelo cursor
    public float selectedScale = 1.2f;          // Escala quando selecionado
    
    [Header("Configurações de Posição")]
    public Vector3 originalPosition;            // Posição original do livro na estante
    private Vector2Int _lastKnownGridPosition = new Vector2Int(-1, -1);
    
    [Header("Referências")]
    public BookSlot _currentSlot;
    private BookSlot _lastKnownSlot;  
    
    protected BookshelfPuzzleManager puzzleManager;  // Referência ao gerenciador do puzzle
    protected bool isHighlighted = false;       // Indica se está destacado pelo cursor
    protected bool isSelected = false;          // Indica se está selecionado pelo jogador
    
    protected virtual void Awake()
    {
        // Verificar se temos o SpriteRenderer
        if (bookSprite == null)
            bookSprite = GetComponent<SpriteRenderer>();
            
        // Armazenar posição original
        originalPosition = transform.position;
        
        // Inicializar aparência
        UpdateAppearance();
    }
    
    public virtual void SetPuzzleManager(BookshelfPuzzleManager manager)
    {
        puzzleManager = manager;
    }
    
    public virtual void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;
        UpdateAppearance();
    }
    
    public virtual void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateAppearance();
    }
    
    // Atualiza a aparência visual baseada no estado
    protected virtual void UpdateAppearance()
    {
        if (bookSprite == null) return;
        
        // Atualizar cor
        if (isSelected)
            bookSprite.color = selectedColor;
        else if (isHighlighted)
            bookSprite.color = highlightColor;
        else
            bookSprite.color = normalColor;
            
        // Atualizar escala
        Vector3 targetScale = Vector3.one;
        if (isSelected)
            targetScale = Vector3.one * selectedScale;
        else if (isHighlighted)
            targetScale = Vector3.one * highlightedScale;
        else
            targetScale = Vector3.one * normalScale;
            
        transform.localScale = targetScale;
    }
    
    // Retorna o livro à sua posição original
    public virtual void ResetPosition()
    {
        transform.position = originalPosition;
    }
    
    // Detecta cliques (implementação alternativa caso precise)
    protected virtual void OnMouseDown()
    {
        if (puzzleManager == null || !puzzleManager.puzzleActive) return;
        
        // Podemos implementar lógica de clique diretamente aqui como alternativa
    }
    public BookSlot currentSlot
    {
        get { return _currentSlot; }
        set 
        { 
            _currentSlot = value;
            if (value != null)
            {
                _lastKnownSlot = value; // Atualiza o último slot conhecido
                Debug.Log($"[BOOK ITEM] {name}: Atualizando lastKnownSlot para {value.name}");
            }
        }
    }
    
    public BookSlot lastKnownSlot
    {
        get { return _lastKnownSlot; }
    }
    public Vector2Int lastKnownGridPosition
    {
        get { return _lastKnownGridPosition; }
        set 
        { 
            _lastKnownGridPosition = value;
            Debug.Log($"[BOOK ITEM] {name}: Atualizando lastKnownGridPosition para [{value.y}, {value.x}]");
        }
    }
}