using UnityEngine;

// Enum para as direções das peças
public enum PuzzleDirection
{
    NW, // Noroeste
    NE, // Nordeste
    SW, // Sudoeste
    SE, // Sudeste
    N,  // Norte
    E,  // Leste
    S,  // Sul
    W   // Oeste
}

public class PuzzleSlot : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer slotRenderer;
    public BoxCollider2D slotCollider;
    
    [Header("Visuals")]
    public Sprite emptySprite;
    public Sprite[] daggerSprites; // NW, NE, SW, SE
    public Sprite[] shieldSprites; // N, E, S, W
    
    [Header("Puzzle Position")]
    public Vector2Int position;
    public PuzzleItemType currentType = PuzzleItemType.Empty;
    public PuzzleDirection currentDirection = PuzzleDirection.NW; // Direção atual da peça
    
    [Header("Solution Info")]
    public PuzzleItemType solutionType = PuzzleItemType.Empty;
    public PuzzleDirection solutionDirection = PuzzleDirection.NW; // Direção correta para solução
    
    private void Start()
    {
        if (slotRenderer == null)
            slotRenderer = GetComponent<SpriteRenderer>();
            
        if (slotCollider == null)
            slotCollider = GetComponent<BoxCollider2D>();
            
        // Determinar a direção inicial com base na posição
        DetermineInitialDirection();
            
        UpdateVisual();
    }
    
    // Determina a direção inicial com base na posição do slot
    public void DetermineInitialDirection()
    {
        // Direções para adagas nas diagonais
        if (position.x == 0 && position.y == 0)
            currentDirection = PuzzleDirection.NW;
        else if (position.x == 2 && position.y == 0)
            currentDirection = PuzzleDirection.NE;
        else if (position.x == 0 && position.y == 2)
            currentDirection = PuzzleDirection.SW;
        else if (position.x == 2 && position.y == 2)
            currentDirection = PuzzleDirection.SE;
        
        // Direções para escudos nas posições N, E, S, W
        else if (position.x == 1 && position.y == 0)
            currentDirection = PuzzleDirection.N;
        else if (position.x == 2 && position.y == 1)
            currentDirection = PuzzleDirection.E;
        else if (position.x == 1 && position.y == 2)
            currentDirection = PuzzleDirection.S;
        else if (position.x == 0 && position.y == 1)
            currentDirection = PuzzleDirection.W;
            
        // Salva a direção inicial como solução (se não for vazio)
        if (currentType != PuzzleItemType.Empty)
            solutionDirection = currentDirection;
    }
    
    public void UpdateVisual()
    {
        if (slotRenderer == null)
            return;
            
        switch (currentType)
        {
            case PuzzleItemType.Empty:
                slotRenderer.sprite = emptySprite;
                break;
                
            case PuzzleItemType.Dagger:
                int daggerIndex = GetDaggerSpriteIndex();
                if (daggerIndex >= 0 && daggerIndex < daggerSprites.Length)
                    slotRenderer.sprite = daggerSprites[daggerIndex];
                break;
                
            case PuzzleItemType.Shield:
                int shieldIndex = GetShieldSpriteIndex();
                if (shieldIndex >= 0 && shieldIndex < shieldSprites.Length)
                    slotRenderer.sprite = shieldSprites[shieldIndex];
                break;
        }
    }
    
    private int GetDaggerSpriteIndex()
    {
        // Determinar qual sprite da adaga usar com base na direção atual da peça
        switch (currentDirection)
        {
            case PuzzleDirection.NW: return 0;
            case PuzzleDirection.NE: return 1;
            case PuzzleDirection.SW: return 2;
            case PuzzleDirection.SE: return 3;
            default: return 0;
        }
    }
    
    private int GetShieldSpriteIndex()
    {
        // Determinar qual sprite do escudo usar com base na direção atual da peça
        switch (currentDirection)
        {
            case PuzzleDirection.N: return 0;
            case PuzzleDirection.E: return 1;
            case PuzzleDirection.S: return 2;
            case PuzzleDirection.W: return 3;
            default: return 0;
        }
    }
    
    public void SaveSolutionState()
    {
        // Salvar o tipo e direção atuais como solução
        solutionType = currentType;
        solutionDirection = currentDirection;
    }
}