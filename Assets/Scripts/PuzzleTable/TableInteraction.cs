using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI; // Adicionado para elementos UI

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
    public bool isPuzzleOpen = false;
    public Vector2Int emptySlotPosition = new Vector2Int(1, 1); // Centro inicialmente vazio
    private PlayerController playerController;

    [Header("Interaction Icon")]
    public GameObject interactionKeyIconPrefab; // Prefab do ícone E
    public float iconYOffset = 1.5f; // Altura acima da mesa
    public float iconXOffset = 0.5f; // Distância para a direita (valor positivo)   
    private GameObject interactionIcon;

    [Header("Keyboard UI")]
    public GameObject keyboardUIPanel;        // Painel contendo todas as teclas
    public GameObject closeButton; // Referência para o botão de fechar
    public GameObject keyW;                   // Tecla W/Up
    public GameObject keyA;                   // Tecla A/Left
    public GameObject keyS;                   // Tecla S/Down
    public GameObject keyD;                   // Tecla D/Right  
    public Color activeKeyColor = Color.white; // Cor quando a tecla está ativa
    public Color inactiveKeyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Cor quando a tecla está inativa

    [Header("Shuffle Configuration")]
    public int minShuffleMoves = 50;
    public int maxShuffleMoves = 80;
    public float shuffleSpeed = 0.1f; 
    
    
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
        
        // Criar o ícone de interação
        CreateInteractionIcon();

        if (keyboardUIPanel != null)
            keyboardUIPanel.SetActive(false);
    }
    
    private void InitializePuzzleSlots()
    {
        // Criar objetos para os slots do puzzle (isso seria feito no editor)
        // Mas aqui vamos inicializar as referências programaticamente
        
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
                    
                    // Determinar a direção inicial com base na posição
                    slot.DetermineInitialDirection();
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
            
        // Ocultar o ícone de interação quando abrir o puzzle
        if (interactionIcon != null)
            interactionIcon.SetActive(false);

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
        
        // IMPORTANTE: Sempre mostrar o closeButton quando o puzzle é aberto
        if (closeButton != null)
            closeButton.SetActive(true);
        
        // Garantir que a UI do teclado esteja inicialmente desativada
        if (keyboardUIPanel != null)
            keyboardUIPanel.SetActive(false);
        
        // Mostrar destaques de posicionamento se estiver na fase de colocação inicial
        if (puzzleManager.currentState == PuzzleState.Collection || 
            puzzleManager.currentState == PuzzleState.InitialSetup)
        {
            ShowPlacementHighlights();
        }
        else if (puzzleManager.currentState == PuzzleState.Solving)
        {
            // Mostrar a UI do teclado apenas quando estiver na fase de resolução
            ShowKeyboardUI();
        }
    }

    private void ShowKeyboardUI()
    {
        if (keyboardUIPanel != null)
        {
            keyboardUIPanel.SetActive(true);
            Debug.Log("Keyboard UI Panel ativado: " + keyboardUIPanel.name);
            UpdateKeyboardUI(); // Atualizar quais teclas estão disponíveis imediatamente
        }
        else
        {
            Debug.LogError("keyboardUIPanel é null! Verifique as referências no Inspector.");
        }
        
        // Verificar também o botão de fechar
        if (closeButton != null)
        {
            closeButton.SetActive(true);
            Debug.Log("Close Button ativado: " + closeButton.name);
        }
        else
        {
            Debug.LogError("closeButton é null! Verifique as referências no Inspector.");
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
        
        // Desativar todos os elementos da UI
        if (keyboardUIPanel != null)
            keyboardUIPanel.SetActive(false);
        if (closeButton != null)
            closeButton.SetActive(false);
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

    // Função para atualizar quais teclas são válidas:
    private void UpdateKeyboardUI()
    {
        if (keyboardUIPanel == null || !isPuzzleOpen || puzzleManager.currentState != PuzzleState.Solving)
            return;
            
        // Obter movimentos válidos com base na posição do espaço vazio
        List<Vector2Int> validMoves = GetValidMoves();
        
        // Por padrão, todas as teclas são inativas
        SetKeyActive(keyW, false);
        SetKeyActive(keyA, false);
        SetKeyActive(keyS, false);
        SetKeyActive(keyD, false);
        
        // Ativar apenas as teclas que correspondem a movimentos válidos
        foreach (Vector2Int move in validMoves)
        {
            Vector2Int direction = move - emptySlotPosition;

            if (direction.x == 0 && direction.y == -1)  // Mover o espaço vazio para cima / peça para baixo
                SetKeyActive(keyS, true);               // Tecla S/Down
            else if (direction.x == 0 && direction.y == 1)  // Mover o espaço vazio para baixo / peça para cima
                SetKeyActive(keyW, true);               // Tecla W/Up
            else if (direction.x == -1 && direction.y == 0)  // Mover o espaço vazio para direita
                SetKeyActive(keyD, true);               // Tecla D/Right
            else if (direction.x == 1 && direction.y == 0)  // Mover o espaço vazio para esquerda
                SetKeyActive(keyA, true);               // Tecla A/Left
        }
    }

    // Método auxiliar para ativar/desativar uma tecla visualmente
    private void SetKeyActive(GameObject keyObject, bool active)
    {
        if (keyObject == null)
            return;
            
        // Obter o componente de imagem para mudar a cor
        Image keyImage = keyObject.GetComponent<Image>();
        if (keyImage != null)
        {
            keyImage.color = active ? activeKeyColor : inactiveKeyColor;
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
            
            // Verificar se o estado mudou para Solving e atualizar a UI
            if (puzzleManager.currentState == PuzzleState.Solving && keyboardUIPanel != null)
            {
                // Se o teclado ainda não estiver ativo, mostrar e atualizar
                if (!keyboardUIPanel.activeSelf)
                {
                    ShowKeyboardUI();
                }
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
                UpdateKeyboardUI();
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

    private void ConfigureInitialSlotType(PuzzleSlot slot, int x, int y)
    {
        // Centro é vazio
        if (x == 1 && y == 1)
        {
            slot.currentType = PuzzleItemType.Empty;
            emptySlotPosition = new Vector2Int(x, y);
        }
        // Posições diagonais são adagas
        else if ((x == 0 && y == 0) || (x == 2 && y == 0) || 
                (x == 0 && y == 2) || (x == 2 && y == 2))
        {
            slot.currentType = PuzzleItemType.Dagger;
        }
        // Posições ortogonais são escudos
        else
        {
            slot.currentType = PuzzleItemType.Shield;
        }
        
        // Salvar o tipo como solução também
        slot.solutionType = slot.currentType;
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
            InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
            bool hasItem = inventory != null && inventory.HasItem(itemType);
            
            if (hasItem)
            {
                // Colocar a peça no slot
                puzzleSlots[x, y].currentType = itemType;
                
                // Configurar a direção correta com base na posição
                SetAppropriateDirection(puzzleSlots[x, y], x, y);
                
                // Salvar direção como solução
                puzzleSlots[x, y].solutionDirection = puzzleSlots[x, y].currentDirection;
                puzzleSlots[x, y].solutionType = itemType;
                
                // Atualizar visual
                puzzleSlots[x, y].UpdateVisual();
                
                // Remover do inventário
                if (inventory != null)
                    inventory.RemoveItem(itemType);
                    
                // Remover do inventário do PuzzleManager para manter compatibilidade
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

    private void SetAppropriateDirection(PuzzleSlot slot, int x, int y)
    {
        // Configurar a direção com base na posição do slot
        // Diagonais (adagas)
        if (x == 0 && y == 0)
            slot.currentDirection = PuzzleDirection.NW;
        else if (x == 2 && y == 0)
            slot.currentDirection = PuzzleDirection.NE;
        else if (x == 0 && y == 2)
            slot.currentDirection = PuzzleDirection.SW;
        else if (x == 2 && y == 2)
            slot.currentDirection = PuzzleDirection.SE;
        
        // Ortogonais (escudos)
        else if (x == 1 && y == 0)
            slot.currentDirection = PuzzleDirection.N;
        else if (x == 2 && y == 1)
            slot.currentDirection = PuzzleDirection.E;
        else if (x == 1 && y == 2)
            slot.currentDirection = PuzzleDirection.S;
        else if (x == 0 && y == 1)
            slot.currentDirection = PuzzleDirection.W;
    }

    private void ConfigureSlotDirection(PuzzleSlot slot, int x, int y)
    {
        // Configurar a direção com base na posição do slot
        // Diagonais (adagas)
        if (x == 0 && y == 0)
            slot.currentDirection = PuzzleDirection.NW;
        else if (x == 2 && y == 0)
            slot.currentDirection = PuzzleDirection.NE;
        else if (x == 0 && y == 2)
            slot.currentDirection = PuzzleDirection.SW;
        else if (x == 2 && y == 2)
            slot.currentDirection = PuzzleDirection.SE;
        
        // Ortogonais (escudos)
        else if (x == 1 && y == 0)
            slot.currentDirection = PuzzleDirection.N;
        else if (x == 2 && y == 1)
            slot.currentDirection = PuzzleDirection.E;
        else if (x == 1 && y == 2)
            slot.currentDirection = PuzzleDirection.S;
        else if (x == 0 && y == 1)
            slot.currentDirection = PuzzleDirection.W;
        
        // Salvar como solução
        slot.solutionDirection = slot.currentDirection;
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
        
        if (keyboardUIPanel != null)
            keyboardUIPanel.SetActive(false);
        
        // Guardar configuração inicial (solução)
        SaveSolutionState();
        
        // Animação de embaralhamento
        yield return new WaitForSeconds(0.5f);
        
        // Melhorias no algoritmo de embaralhamento:
        // 1. Aumentar número de movimentos
        int shuffleMoves = Random.Range(minShuffleMoves, maxShuffleMoves + 1);
        
        // 2. Evitar movimentos redundantes (retornar à posição anterior)
        Vector2Int lastMove = Vector2Int.zero;
        
        // 3. Usar uma estratégia de "memória" para evitar repetições de padrões
        HashSet<string> recentBoardStates = new HashSet<string>();
        
        for (int i = 0; i < shuffleMoves; i++)
        {
            // Lista de movimentos possíveis
            List<Vector2Int> possibleMoves = GetValidMoves();
            
            if (possibleMoves.Count > 0)
            {
                // Filtrar movimentos para evitar voltar para a posição anterior
                List<Vector2Int> preferredMoves = new List<Vector2Int>();
                foreach (Vector2Int move in possibleMoves)
                {
                    // Evitar reverter o último movimento
                    Vector2Int moveDirection = move - emptySlotPosition;
                    Vector2Int reverseMovePos = emptySlotPosition + (lastMove * -1);
                    
                    if (move != reverseMovePos)
                    {
                        preferredMoves.Add(move);
                    }
                }
                
                // Se não tiver movimentos preferidos (só tem a opção de voltar), usar os originais
                if (preferredMoves.Count == 0)
                    preferredMoves = possibleMoves;
                
                // Escolher um movimento aleatório entre os preferidos
                int moveIndex = Random.Range(0, preferredMoves.Count);
                Vector2Int movePos = preferredMoves[moveIndex];
                
                // Registrar a direção deste movimento para evitar reverter no próximo passo
                lastMove = movePos - emptySlotPosition;
                
                // Mover a peça
                SwapWithEmptySlot(movePos);
                
                // Armazenar o estado atual para evitar repetições
                string currentState = GetBoardStateHash();
                recentBoardStates.Add(currentState);
                
                if (audioManager != null && i % 3 == 0)  // Tocar som a cada 3 movimentos para não sobrecarregar
                    audioManager.PlayPuzzleShuffle();

                // Atualizar visuais
                UpdateAllSlotVisuals();
                
                // Pequena pausa para animação (mais rápida)
                yield return new WaitForSeconds(shuffleSpeed);
            }
        }
        
        // Verificar se o estado final não está muito próximo da solução
        if (IsNearSolution())
        {
            // Realizar mais alguns movimentos aleatórios
            for (int i = 0; i < 10; i++)
            {
                List<Vector2Int> possibleMoves = GetValidMoves();
                if (possibleMoves.Count > 0)
                {
                    int moveIndex = Random.Range(0, possibleMoves.Count);
                    SwapWithEmptySlot(possibleMoves[moveIndex]);
                    yield return new WaitForSeconds(shuffleSpeed);
                }
            }
        }
        
        // Mudar para estado de resolução
        puzzleManager.currentState = PuzzleState.Solving;
        Debug.Log("Mudando para estado de resolução (PuzzleState.Solving)");
        
        // Mostrar a UI do teclado agora que estamos em modo de resolução
        ShowKeyboardUI();
    }

    private string GetBoardStateHash()
    {
        System.Text.StringBuilder hash = new System.Text.StringBuilder();
        
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                PuzzleSlot slot = puzzleSlots[x, y];
                if (slot != null)
                {
                    hash.Append(slot.currentType.ToString()[0]);
                    
                    if (slot.currentType != PuzzleItemType.Empty)
                    {
                        hash.Append(((int)slot.currentDirection).ToString());
                    }
                }
            }
        }
        
        return hash.ToString();
    }
    
    // Verifica se o puzzle está próximo da solução
    private bool IsNearSolution()
    {
        int correctPieces = 0;
        
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot != null)
            {
                if (slot.currentType == slot.solutionType)
                {
                    if (slot.currentType == PuzzleItemType.Empty || 
                        slot.currentDirection == slot.solutionDirection)
                    {
                        correctPieces++;
                    }
                }
            }
        }
        
        // Se mais de 6 peças (de 9) estão corretas, está muito próximo
        return correctPieces > 6;
    }
    
    public IEnumerator AutoClosePuzzleView()
    {
        // Sem espera, tornando o fechamento instantâneo
        yield return null;

        // Fechar o puzzle
        ClosePuzzleView();
        
        // Ocultar o ícone de interação permanentemente
        if (interactionIcon != null)
        {
            interactionIcon.SetActive(false);
        }
        
        // Desabilitar a interação com este objeto
        interactionEnabled = false;
    }

    private void SaveSolutionState()
    {
        // Salvar o estado atual como solução
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot != null)
            {
                slot.SaveSolutionState();
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

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            moveDirection = new Vector2Int(0, -1);  // Corrigido: era (0, 1)
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            moveDirection = new Vector2Int(0, 1);   // Corrigido: era (0, -1)
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            moveDirection = new Vector2Int(1, 0);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
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
        PuzzleSlot pieceSlot = puzzleSlots[piecePosition.x, piecePosition.y];
        PuzzleSlot emptySlot = puzzleSlots[emptySlotPosition.x, emptySlotPosition.y];
        
        // Guardar informações da peça a ser movida
        PuzzleItemType pieceType = pieceSlot.currentType;
        PuzzleDirection pieceDirection = pieceSlot.currentDirection;
        
        // Mover a peça para o espaço vazio (mantendo sua direção)
        emptySlot.currentType = pieceType;
        emptySlot.currentDirection = pieceDirection;
        
        // Definir o slot original como vazio
        pieceSlot.currentType = PuzzleItemType.Empty;
        
        // Atualizar posição do espaço vazio
        emptySlotPosition = piecePosition;
        
        // Atualizar visuais
        pieceSlot.UpdateVisual();
        emptySlot.UpdateVisual();
        
        // Atualizar a UI de teclado após o movimento
        UpdateKeyboardUI();
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
        // Verificar se todas as peças estão na posição correta e com a direção correta
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot != null)
            {
                // Se o tipo é diferente da solução, não está resolvido
                if (slot.currentType != slot.solutionType)
                    return false;
                    
                // Se não é vazio e a direção é diferente da solução, não está resolvido
                if (slot.currentType != PuzzleItemType.Empty && 
                    slot.currentDirection != slot.solutionDirection)
                    return false;
            }
        }
        
        return true;
    }

    private void CreateInteractionIcon()
    {
        if (interactionKeyIconPrefab != null)
        {
            // Criar o ícone como instância independente primeiro
            interactionIcon = Instantiate(interactionKeyIconPrefab);
            
            // Guardar a escala original do prefab
            Vector3 originalScale = interactionIcon.transform.localScale;

            // Guardar a posição original do prefab
            Vector3 originalPosition = interactionIcon.transform.localPosition;
            
            // Tornar o ícone filho da mesa para que se mova junto
            interactionIcon.transform.SetParent(transform, false); // false é importante - mantém a escala mundial
            
            // Aplicar offset vertical (Y) e horizontal (X)
            interactionIcon.transform.localPosition = new Vector3(iconXOffset, iconYOffset, 0);
            
            // Garantir que a escala permaneça a mesma do prefab
            interactionIcon.transform.localScale = originalScale;
            
            // Desativar inicialmente
            interactionIcon.SetActive(false);
        }
    }

    public void ShowInteractionIcon()
    {
        // Mostrar o ícone apenas se:
        // 1. O puzzle não está aberto
        // 2. A interação está habilitada
        // 3. O puzzle não foi resolvido
        if (interactionIcon != null && !isPuzzleOpen && interactionEnabled && 
            (puzzleManager == null || puzzleManager.currentState != PuzzleState.Completed))
        {
            interactionIcon.SetActive(true);
        }
        
        // Garantir que a UI do teclado NÃO seja ativada aqui
        if (keyboardUIPanel != null && !isPuzzleOpen)
            keyboardUIPanel.SetActive(false);
    }

    public void HideInteractionIcon()
    {
        if (interactionIcon != null)
            interactionIcon.SetActive(false);
        
        // Garantir que a UI do teclado também seja desativada
        if (keyboardUIPanel != null && !isPuzzleOpen)
            keyboardUIPanel.SetActive(false);
    }
}