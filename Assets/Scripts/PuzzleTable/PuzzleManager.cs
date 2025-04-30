using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PuzzleItemType
{
    Empty,
    Dagger,
    Shield
}

public enum PuzzleState
{
    Collection,      // Coletando peças dos barris
    InitialSetup,    // Colocando peças na mesa
    Shuffling,       // Embaralhando automaticamente
    Solving,         // Jogador está resolvendo o puzzle
    Completed        // Puzzle foi resolvido
}

public class PuzzleManager : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public TableInteraction tableInteraction;
    public GameObject keyObject;
    public KeyAnimation keyAnimation;
    
    [Header("Puzzle Items")]
    public List<PuzzleItemType> inventory = new List<PuzzleItemType>();
    public int maxDaggers = 4;
    public int maxShields = 4;
    
    [Header("Puzzle State")]
    public PuzzleState currentState = PuzzleState.Collection;
    public bool puzzleSolved = false;
    
    private PlayerController playerController;
    
    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        
        if (keyObject != null)
            keyObject.SetActive(false);
    }
    
    public void AddItemToInventory(PuzzleItemType itemType)
    {
        inventory.Add(itemType);
        Debug.Log("Added " + itemType + " to inventory. Count: " + inventory.Count);
    }
    
    public bool HasItemInInventory(PuzzleItemType itemType)
    {
        return inventory.Contains(itemType);
    }
    
    public void RemoveItemFromInventory(PuzzleItemType itemType)
    {
        if (inventory.Contains(itemType))
        {
            inventory.Remove(itemType);
            Debug.Log("Removed " + itemType + " from inventory. Count: " + inventory.Count);
        }
    }
    
    public void CheckPuzzleCompletion()
    {
        // Verificar se o puzzle está resolvido
        if (tableInteraction.IsPuzzleSolved() && currentState == PuzzleState.Solving)
        {
            StartCoroutine(CompletePuzzle());
        }
    }

    public void SetupInitialPieces()
    {
        // Configure quais peças já estão na mesa inicialmente
        // Exemplo: 4 peças já colocadas (2 adagas e 2 escudos)
        
        // Isso deve ser chamado no Start ou em um método de inicialização
        // do seu jogo, antes de permitir que o jogador coloque peças
        
        TableInteraction tableInteraction = GetComponent<TableInteraction>();
        if (tableInteraction != null && tableInteraction.puzzleSlots != null)
        {
            // Aqui você pode configurar quais slots já têm peças
            // Por exemplo, vamos supor que queremos adagas nos cantos NW e SE,
            // e escudos nos lados N e W:
            
            // Adaga no canto superior esquerdo (NW)
            if (tableInteraction.puzzleSlots[0, 0] != null)
            {
                PuzzleSlot slot = tableInteraction.puzzleSlots[0, 0];
                slot.currentType = PuzzleItemType.Dagger;
                slot.currentDirection = PuzzleDirection.NW;
                slot.solutionType = PuzzleItemType.Dagger;
                slot.solutionDirection = PuzzleDirection.NW;
                slot.UpdateVisual();
            }
            
            // Adaga no canto inferior direito (SE)
            if (tableInteraction.puzzleSlots[2, 2] != null)
            {
                PuzzleSlot slot = tableInteraction.puzzleSlots[2, 2];
                slot.currentType = PuzzleItemType.Dagger;
                slot.currentDirection = PuzzleDirection.SE;
                slot.solutionType = PuzzleItemType.Dagger;
                slot.solutionDirection = PuzzleDirection.SE;
                slot.UpdateVisual();
            }
            
            // Escudo no lado norte (N)
            if (tableInteraction.puzzleSlots[1, 0] != null)
            {
                PuzzleSlot slot = tableInteraction.puzzleSlots[1, 0];
                slot.currentType = PuzzleItemType.Shield;
                slot.currentDirection = PuzzleDirection.N;
                slot.solutionType = PuzzleItemType.Shield;
                slot.solutionDirection = PuzzleDirection.N;
                slot.UpdateVisual();
            }
            
            // Escudo no lado oeste (W)
            if (tableInteraction.puzzleSlots[0, 1] != null)
            {
                PuzzleSlot slot = tableInteraction.puzzleSlots[0, 1];
                slot.currentType = PuzzleItemType.Shield;
                slot.currentDirection = PuzzleDirection.W;
                slot.solutionType = PuzzleItemType.Shield;
                slot.solutionDirection = PuzzleDirection.W;
                slot.UpdateVisual();
            }
        }
    }
    
    private IEnumerator CompletePuzzle()
    {
        currentState = PuzzleState.Completed;
        puzzleSolved = true;

        // Ativar a chave e iniciar a animação
        if (keyAnimation != null)
        {
            keyAnimation.ResetAnimation();
            keyAnimation.PlayAppearAnimation();
        }

        yield return null; // A animação é controlada pelo KeyAnimation agora
    }
    
    public void CollectKey()
    {
        if (puzzleSolved && keyAnimation != null && keyAnimation.CanBeCollected())
        {
            // Adicionar chave ao inventário do jogador
            Debug.Log("Chave coletada!");

            // Desativar interação com a mesa
            tableInteraction.interactionEnabled = false;

            // Fechar a visualização do puzzle
            tableInteraction.ClosePuzzleView();
            
            // Desativar a chave
            keyObject.SetActive(false);
        }
    }
    private HashSet<string> collectedItems = new HashSet<string>();

    public void MarkItemCollected(string itemId)
    {
        collectedItems.Add(itemId);
    }

    public bool IsItemCollected(string itemId)
    {
        return collectedItems.Contains(itemId);
    }
}