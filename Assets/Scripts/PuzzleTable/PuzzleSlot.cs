using UnityEngine;

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
    public PuzzleItemType solutionType = PuzzleItemType.Empty;
    
    private void Start()
    {
        if (slotRenderer == null)
            slotRenderer = GetComponent<SpriteRenderer>();
            
        if (slotCollider == null)
            slotCollider = GetComponent<BoxCollider2D>();
            
        UpdateVisual();
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
                // Determinar qual sprite de adaga usar com base na posição
                int daggerIndex = GetDaggerSpriteIndex();
                if (daggerIndex >= 0 && daggerIndex < daggerSprites.Length)
                    slotRenderer.sprite = daggerSprites[daggerIndex];
                break;
                
            case PuzzleItemType.Shield:
                // Determinar qual sprite de escudo usar com base na posição
                int shieldIndex = GetShieldSpriteIndex();
                if (shieldIndex >= 0 && shieldIndex < shieldSprites.Length)
                    slotRenderer.sprite = shieldSprites[shieldIndex];
                break;
        }
    }
    
    private int GetDaggerSpriteIndex()
    {
        // Determinar qual variante da adaga baseado na posição
        // Isso depende de como você quer que as adagas apontem para o centro
        
        // Assumindo grade 3x3 onde o centro é [1,1]
        if (position.x == 0 && position.y == 0)
            return 0; // Noroeste (NW) -> Sudeste
        else if (position.x == 2 && position.y == 0)
            return 1; // Nordeste (NE) -> Sudoeste
        else if (position.x == 0 && position.y == 2)
            return 2; // Sudoeste (SW) -> Nordeste
        else if (position.x == 2 && position.y == 2)
            return 3; // Sudeste (SE) -> Noroeste
            
        return 0; // Fallback
    }
    
    private int GetShieldSpriteIndex()
    {
        // Determinar qual variante do escudo baseado na posição
        
        // Assumindo grade 3x3 onde o centro é [1,1]
        if (position.x == 1 && position.y == 0)
            return 0; // Norte (N) -> Sul
        else if (position.x == 2 && position.y == 1)
            return 1; // Leste (E) -> Oeste
        else if (position.x == 1 && position.y == 2)
            return 2; // Sul (S) -> Norte
        else if (position.x == 0 && position.y == 1)
            return 3; // Oeste (W) -> Leste
            
        return 0; // Fallback
    }
}