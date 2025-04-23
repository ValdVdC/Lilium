using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TableInteraction : MonoBehaviour, IInteractable
{
    [Header("References")]
    public Transform puzzleCameraPosition;
    public float puzzleCameraSize = 2.0f;
    public PuzzleManager puzzleManager;
    public GameObject puzzleBoard;
    public GameObject placementHighlights;
    public GameObject keyObject;
    public PuzzleAudioManager audioManager;
    public CameraController cameraController;

    [Header("Puzzle Configuration")]
    public PuzzleSlot[,] puzzleSlots = new PuzzleSlot[3,3];
    public GameObject emptySlotVisual;
    
    [HideInInspector]
    public bool interactionEnabled = true;
    private bool isPuzzleOpen = false;
    private Vector2Int emptySlotPosition = new Vector2Int(1, 1); // Centro inicialmente vazio
    private PlayerController playerController;
    
    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        puzzleBoard.SetActive(true);
        placementHighlights.SetActive(false);
        
        if (keyObject != null)
            keyObject.SetActive(false);

        if (audioManager == null)
            audioManager = FindFirstObjectByType<PuzzleAudioManager>();
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();
            
        // Inicializar slots do puzzle
        InitializePuzzleSlots();
    }
    
    private void InitializePuzzleSlots()
    {
        // Criar objetos para os slots do puzzle (isso seria feito no editor)
        // Mas aqui vamos inicializar as referências programaticamente
        
        // Definir posições e tipos dos slots iniciais
        // Supondo que temos os slots já criados no Editor do Unity
        
        // Por exemplo, assim seria a configuração inicial:
        // DaggerNW    ShieldN    DaggerNE
        // ShieldW     Empty      ShieldE
        // DaggerSW    ShieldS    DaggerSE
        
        // Aqui apenas inicializando a estrutura de dados
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                // Buscar o GameObject correspondente baseado nas coordenadas
                string slotName = "PuzzleSlot_" + x + "_" + y;
                Transform slotTransform = puzzleBoard.transform.Find(slotName);
                
                if (slotTransform != null)
                {
                    PuzzleSlot slot = slotTransform.GetComponent<PuzzleSlot>();
                    if (slot == null)
                        slot = slotTransform.gameObject.AddComponent<PuzzleSlot>();
                        
                    slot.position = new Vector2Int(x, y);
                    puzzleSlots[x, y] = slot;
                    
                    // Centro é vazio
                    if (x == 1 && y == 1)
                    {
                        slot.currentType = PuzzleItemType.Empty;
                        emptySlotPosition = new Vector2Int(x, y);
                    }
                }
            }
        }
    }
    
    public void Interact()
    {
        if (!interactionEnabled)
            return;
            
        if (!isPuzzleOpen)
        {
            OpenPuzzleView();
        }
        else
        {
            ClosePuzzleView();
        }
    }
    
    public void OpenPuzzleView()
    {
        isPuzzleOpen = true;
        
        // Desativar jogador e também o InteractorController
        if (playerController != null)
        {
            playerController.enabled = false;
            playerController.spriteRenderer.enabled = false;
            
            // Desativar também o InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = false;

            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(false);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(false);

            ShadowCaster2D shadowCaster = GetComponent<ShadowCaster2D>();
            if (shadowCaster != null) shadowCaster.enabled = false;
        }

        // Ativar câmera da mesa de puzzle
        if (cameraController != null)
        {
            cameraController.ActivateTablePuzzleCamera(puzzleCameraPosition, puzzleCameraSize);
        }
        
        puzzleBoard.SetActive(true);
        
        // Mostrar destaques de posicionamento se estiver na fase de colocação inicial
        if (puzzleManager.currentState == PuzzleState.Collection || 
            puzzleManager.currentState == PuzzleState.InitialSetup)
        {
            ShowPlacementHighlights();
        }
    }
    
    public void ClosePuzzleView()
    {
        isPuzzleOpen = false;
        
        // Reativar movimento do jogador e o InteractorController
        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.spriteRenderer.enabled = true;
            
            // Reativar o InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = true;

            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(true);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(true);

            ShadowCaster2D shadowCaster = GetComponent<ShadowCaster2D>();
            if (shadowCaster != null) shadowCaster.enabled = true;
        }

        // Retornar à câmera principal
        if (cameraController != null)
        {
            cameraController.ReturnToMainCamera();
        }
        
        puzzleBoard.SetActive(true);
        placementHighlights.SetActive(false);
    }
    
    
    private void ShowPlacementHighlights()
    {
        placementHighlights.SetActive(true);
        
        // Lógica para mostrar apenas os highlights das peças disponíveis
        // e nas posições corretas de acordo com o tipo (adaga nas diagonais, escudo no meio)
        foreach (Transform highlight in placementHighlights.transform)
        {
            highlight.gameObject.SetActive(false);
            string name = highlight.name;
            
            // Exemplo: Highlight_Dagger_0_0 seria para adaga na posição [0,0]
            string[] parts = name.Split('_');
            if (parts.Length >= 4)
            {
                string type = parts[1];
                int x = int.Parse(parts[2]);
                int y = int.Parse(parts[3]);
                
                // Verificar se esta posição já está ocupada
                if (puzzleSlots[x, y] != null && puzzleSlots[x, y].currentType != PuzzleItemType.Empty)
                    continue;
                
                // Verificar se temos o item correto no inventário
                if (type == "Dagger" && puzzleManager.HasItemInInventory(PuzzleItemType.Dagger))
                {
                    // Adagas vão nas diagonais
                    if ((x == 0 && y == 0) || (x == 0 && y == 2) || 
                        (x == 2 && y == 0) || (x == 2 && y == 2))
                    {
                        highlight.gameObject.SetActive(true);
                    }
                }
                else if (type == "Shield" && puzzleManager.HasItemInInventory(PuzzleItemType.Shield))
                {
                    // Escudos vão no meio de cada lado
                    if ((x == 0 && y == 1) || (x == 1 && y == 0) || 
                        (x == 1 && y == 2) || (x == 2 && y == 1))
                    {
                        highlight.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
    
    private void Update()
    {
        if (isPuzzleOpen)
        {
            // Verificar se pressionou E para fechar o puzzle
            if (Input.GetKeyDown(KeyCode.E))
            {
                ClosePuzzleView();
                return;
            }
            
            // Lógica para interação com o puzzle
            if (Input.GetMouseButtonDown(0))
            {
                HandlePuzzleClick();
            }
            
            // Verificar teclas de direção para mover peças (na fase de resolução)
            if (puzzleManager.currentState == PuzzleState.Solving)
            {
                HandleKeyboardInput();
            }
        }
    }
    
    private void HandlePuzzleClick()
    {
        // Usar a câmera do puzzle (via cameraController) em vez da mainCamera
        Camera currentCamera = cameraController.tablePuzzleCamera;
        
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        // Restante do código de manipulação de clique fica igual...
        if (hit.collider != null)
        {
            // Estado inicial: colocando peças na mesa
            if (puzzleManager.currentState == PuzzleState.Collection || 
                puzzleManager.currentState == PuzzleState.InitialSetup)
            {
                // Verificar se clicou em um highlight
                Transform parent = hit.collider.transform.parent;
                if (parent != null && parent.gameObject == placementHighlights)
                {
                    PlacePuzzlePiece(hit.collider.gameObject);
                }
            }
            // Estado de resolução: movendo peças
            else if (puzzleManager.currentState == PuzzleState.Solving)
            {
                // Verificar se clicou em uma peça adjacente ao espaço vazio
                PuzzleSlot clickedSlot = hit.collider.GetComponent<PuzzleSlot>();
                if (clickedSlot != null)
                {
                    TryMovePiece(clickedSlot.position);
                }
            }
            // Estado concluído: pegando a chave
            else if (puzzleManager.currentState == PuzzleState.Completed)
            {
                if (hit.collider.gameObject == keyObject)
                {
                    puzzleManager.CollectKey();
                }
            }
        }
    }
    
    private void PlacePuzzlePiece(GameObject highlightObject)
    {
        string[] parts = highlightObject.name.Split('_');
        if (parts.Length >= 4)
        {
            string type = parts[1];
            int x = int.Parse(parts[2]);
            int y = int.Parse(parts[3]);
            
            PuzzleItemType itemType = (type == "Dagger") ? PuzzleItemType.Dagger : PuzzleItemType.Shield;
            
            // Verificar se temos este item no inventário
            if (puzzleManager.HasItemInInventory(itemType))
            {
                // Colocar a peça no slot
                puzzleSlots[x, y].currentType = itemType;
                puzzleSlots[x, y].UpdateVisual();
                
                // Remover do inventário
                puzzleManager.RemoveItemFromInventory(itemType);
                
                // Atualizar highlights
                ShowPlacementHighlights();
                
                // Verificar se todas as peças foram colocadas
                CheckAllPiecesPlaced();

                if (audioManager != null)
                audioManager.PlayPiecePlace();
            }
        }
    }
    
    private void CheckAllPiecesPlaced()
    {
        int daggerCount = 0;
        int shieldCount = 0;
        
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot != null)
            {
                if (slot.currentType == PuzzleItemType.Dagger)
                    daggerCount++;
                else if (slot.currentType == PuzzleItemType.Shield)
                    shieldCount++;
            }
        }
        
        if (daggerCount == puzzleManager.maxDaggers && shieldCount == puzzleManager.maxShields)
        {
            // Todas as peças foram colocadas, iniciar embaralhamento
            StartCoroutine(ShufflePuzzle());
        }
    }
    
    private IEnumerator ShufflePuzzle()
    {
        puzzleManager.currentState = PuzzleState.Shuffling;
        placementHighlights.SetActive(false);
        
        // Guardar configuração inicial (solução)
        SaveSolutionState();
        
        // Animação de embaralhamento
        yield return new WaitForSeconds(0.5f);
        
        // Realizar movimentos aleatórios válidos (30-50 movimentos)
        int shuffleMoves = Random.Range(30, 51);
        for (int i = 0; i < shuffleMoves; i++)
        {
            // Lista de movimentos possíveis
            List<Vector2Int> possibleMoves = GetValidMoves();
            
            if (possibleMoves.Count > 0)
            {
                // Escolher um movimento aleatório
                int moveIndex = Random.Range(0, possibleMoves.Count);
                Vector2Int movePos = possibleMoves[moveIndex];
                
                // Mover a peça
                SwapWithEmptySlot(movePos);
                
                if (audioManager != null)
                audioManager.PlayPuzzleShuffle();

                // Atualizar visuais
                UpdateAllSlotVisuals();
                
                // Pequena pausa para animação
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        // Mudar para estado de resolução
        puzzleManager.currentState = PuzzleState.Solving;
    }
    
    private void SaveSolutionState()
    {
        // Salvar o estado atual como solução
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot != null)
            {
                slot.solutionType = slot.currentType;
            }
        }
    }
    
    private List<Vector2Int> GetValidMoves()
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        
        // Verificar as quatro direções
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // Cima
            new Vector2Int(0, -1), // Baixo
            new Vector2Int(1, 0),  // Direita
            new Vector2Int(-1, 0)  // Esquerda
        };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = emptySlotPosition + dir;
            
            // Verificar se está dentro dos limites
            if (newPos.x >= 0 && newPos.x < 3 && newPos.y >= 0 && newPos.y < 3)
            {
                validMoves.Add(newPos);
            }
        }
        
        return validMoves;
    }
    
    private void HandleKeyboardInput()
    {
        Vector2Int moveDirection = Vector2Int.zero;
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
            moveDirection = new Vector2Int(0, 1);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            moveDirection = new Vector2Int(0, -1);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            moveDirection = new Vector2Int(1, 0);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            moveDirection = new Vector2Int(-1, 0);
            
        if (moveDirection != Vector2Int.zero)
        {
            Vector2Int pieceToMove = emptySlotPosition - moveDirection;
            
            // Verificar se está dentro dos limites
            if (pieceToMove.x >= 0 && pieceToMove.x < 3 && 
                pieceToMove.y >= 0 && pieceToMove.y < 3)
            {
                TryMovePiece(pieceToMove);
            }
        }
    }
    
    private void TryMovePiece(Vector2Int piecePosition)
    {
        // Verificar se a peça está adjacente ao espaço vazio
        if (IsAdjacentToEmpty(piecePosition))
        {
            // Mover a peça para o espaço vazio
            SwapWithEmptySlot(piecePosition);

            if (audioManager != null)
            audioManager.PlayPieceMovement();

            // Atualizar visuais
            UpdateAllSlotVisuals();
            
            // Verificar se o puzzle foi resolvido
            puzzleManager.CheckPuzzleCompletion();
        }
    }
    
    private bool IsAdjacentToEmpty(Vector2Int position)
    {
        // Verificar se a posição está adjacente (não diagonal) ao espaço vazio
        int xDiff = Mathf.Abs(position.x - emptySlotPosition.x);
        int yDiff = Mathf.Abs(position.y - emptySlotPosition.y);
        
        return (xDiff == 1 && yDiff == 0) || (xDiff == 0 && yDiff == 1);
    }
    
    private void SwapWithEmptySlot(Vector2Int piecePosition)
    {
        // Trocar a peça com o espaço vazio
        PuzzleItemType pieceType = puzzleSlots[piecePosition.x, piecePosition.y].currentType;
        
        puzzleSlots[emptySlotPosition.x, emptySlotPosition.y].currentType = pieceType;
        puzzleSlots[piecePosition.x, piecePosition.y].currentType = PuzzleItemType.Empty;
        
        // Atualizar a posição do espaço vazio
        emptySlotPosition = piecePosition;
    }
    
    private void UpdateAllSlotVisuals()
    {
        // Atualizar os visuais de todos os slots
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot != null)
            {
                slot.UpdateVisual();
            }
        }
    }
    
    public bool IsPuzzleSolved()
    {
        // Verificar se todas as peças estão na posição correta
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot != null)
            {
                if (slot.currentType != slot.solutionType)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
}