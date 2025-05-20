using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum PuzzleItemType
{
    Empty,
    Dagger,
    Shield,
    Key
}

public enum PuzzleState
{
    Collection,      // Coletando peças dos barris
    InitialSetup,    // Colocando peças na mesa
    Shuffling,       // Embaralhando automaticamente
    Solving,         // Jogador está resolvendo o puzzle
    Completed        // Puzzle foi resolvido
}

public class PuzzleManager : MonoBehaviour, ISaveable
{
    [Header("References")]
    public Camera mainCamera;
    public TableInteraction tableInteraction;
    public GameObject keyObject;
    public KeyAnimation keyAnimation;
    public PuzzleAudioManager audioManager;
    public GameObject tilemapToDisable;
    public GameObject torchToDisable;
    public InventoryManager inventoryManager;
    
    [Header("Puzzle Items")]
    public List<PuzzleItemType> inventory = new List<PuzzleItemType>();
    public int maxDaggers = 4;
    public int maxShields = 4;
    
    [Header("Puzzle State")]
    public PuzzleState currentState = PuzzleState.Collection;
    public bool puzzleSolved = false;
    
    private PlayerController playerController;
    private HashSet<string> collectedItems = new HashSet<string>();
    
    [Serializable]
    public class SaveData
    {
        public bool puzzleSolved;
        public List<PuzzleItemType> inventory;
        public PuzzleState currentState;
        public HashSet<string> collectedItems;
    }

    public object GetSaveData()
    {
        SaveData data = new SaveData
        {
            puzzleSolved = this.puzzleSolved,
            inventory = new List<PuzzleItemType>(this.inventory),
            currentState = this.currentState,
            collectedItems = new HashSet<string>(this.collectedItems)
        };
        return data;
    }


    public void LoadFromSaveData(object saveData)
    {
        if (saveData is SaveData data)
        {
            this.puzzleSolved = data.puzzleSolved;
            
            this.inventory = data.inventory != null 
                ? new List<PuzzleItemType>(data.inventory) 
                : new List<PuzzleItemType>();
                
            this.currentState = data.currentState;

            this.collectedItems = data.collectedItems != null 
                ? new HashSet<string>(data.collectedItems) 
                : new HashSet<string>();
            
            // Atualizar o inventário visual
            if (inventoryManager != null)
            {
                foreach (var item in inventory)
                {
                    inventoryManager.AddItem(item);
                }
            }
            
            // Se o puzzle já foi resolvido, desativar a mesa
            if (puzzleSolved && tableInteraction != null)
            {
                tableInteraction.interactionEnabled = false;
                tableInteraction.ClosePuzzleView();
            }
        }
    }

    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        
        if (keyObject != null)
            keyObject.SetActive(false);
    }
    
    public void AddItemToInventory(PuzzleItemType itemType)
    {
        // Primeiro verifica se este item já está no inventário
        if (!inventory.Contains(itemType))
        {
            inventory.Add(itemType);
            Debug.Log("PuzzleManager: Added " + itemType + " to inventory. Count: " + inventory.Count);
            
            // Sincronizar com o inventário visual, só se ainda não foi adicionado
            if (inventoryManager != null)
            {
                // Note que não usamos AddItem aqui para evitar duplicação
                // O método que chamou esta função já deveria ter adicionado o item ao inventoryManager
                Debug.Log("PuzzleManager: Item já adicionado ao inventoryManager anteriormente.");
            }
            else
            {
                Debug.LogWarning("PuzzleManager: inventoryManager é nulo!");
            }
        }
        else
        {
            Debug.Log("PuzzleManager: Item " + itemType + " já estava no inventário.");
        }
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
            
            // Sincronizar com o inventário visual
            if (inventoryManager != null)
            {
                inventoryManager.RemoveItem(itemType);
            }
            
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

        // Tocar som de puzzle completado imediatamente quando resolver
        if (audioManager != null)
        {
            Debug.Log("Tocando som de puzzle resolvido");
            audioManager.PlayPuzzleSolved();
            
            // Opcionalmente mudar a música de fundo
            audioManager.PlaySolvedMusic();
        }

        // Desativar o tilemap especificado
        if (tilemapToDisable != null)
        {
            tilemapToDisable.SetActive(false);
            Debug.Log("Tilemap desativado após completar o puzzle");
        }

        // Ativar a tocha
        if (torchToDisable != null)
        {
            torchToDisable.SetActive(true);
            Debug.Log("Tocha ativada após completar o puzzle");
        }
        
        // Fechar o puzzle automaticamente
        if (tableInteraction != null)
        {
            // Iniciar a coroutine de fechamento automático
            tableInteraction.StartCoroutine(tableInteraction.AutoClosePuzzleView());
            
            // Desativar permanentemente a capacidade de interação com a mesa
            tableInteraction.interactionEnabled = false;
        }

        yield return null;
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

    public void MarkItemCollected(string itemId)
    {
        collectedItems.Add(itemId);
    }

    public bool IsItemCollected(string itemId)
    {
        return collectedItems.Contains(itemId);
    }
}