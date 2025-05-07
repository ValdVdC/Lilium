using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class BookshelfPuzzleManager : MonoBehaviour
{
    [Header("Estado do Puzzle")]
    public bool puzzleActive = false;       // Indica se o puzzle está ativo
    public bool puzzleSolved = false;       // Indica se o puzzle foi resolvido
    
    [Header("Referências dos Livros")]
    public BookItem blueHorizontalBook;     // Livro azul deitado (terceiro da prateleira de cima)
    public BookItem redYellowBook;          // Livro vermelho com detalhes amarelos (segundo da prateleira de cima)
    public BookItem greenHorizontalBook;    // Livro verde deitado (último da prateleira de baixo)
    public BookItem purpleSpottedBook;      // Livro roxo com detalhes amarelos pontilhados (terceiro da prateleira de baixo)
    
    [Header("Slots para Solução")]
    public BookSlot spadesSlot;             // Slot do naipe de espadas (1º slot)
    public BookSlot clubsSlot;              // Slot do naipe de paus (2º slot)
    public BookSlot diamondsSlot;           // Slot do naipe de ouros (3º slot)
    public BookSlot heartsSlot;             // Slot do naipe de copas (4º slot)
    
    [Header("Interface e Feedback")]
    public GameObject puzzleUI;             // UI com instruções
    public GameObject interactionKeyIcon;   // Ícone de tecla E para interagir
    public GameObject successFeedback;      // Feedback visual de sucesso
    public GameObject failureFeedback;      // Feedback visual de falha
    
    [Header("Câmera")]
    public CameraController cameraController;  // Controlador da câmera
    public Transform bookshelfPosition;        // Posição da estante para a câmera focar
    public float puzzleCameraSize = 3.0f;      // Tamanho da câmera durante o puzzle
    
    // [Header("Áudio")]
    // public PuzzleAudioManager audioManager;    // Gerenciador de áudio
    
    [Header("Visual")]
    public float selectionHighlightScale = 1.2f;    // Escala para destacar o item selecionado
    public Color highlightColor = new Color(1f, 1f, 0.7f, 1f);  // Cor para destacar o item sob o cursor

    [Header("Posições dos Itens")]
    public BookItem[,] bookGrid;           // Matriz para armazenar os livros por posição lógica
    public int rowCount = 3;               // Número de prateleiras
    public int colCount = 4;               // Número de livros/slots por prateleira

    [Header("Visualização Prévia")]
    public float previewAlpha = 0.5f;           // Transparência para a visualização prévia
    private GameObject previewObject;           // Objeto de visualização prévia
    private SpriteRenderer previewRenderer;     // Renderer do objeto de visualização

    // Listas para controle
    private List<BookItem> allBooks = new List<BookItem>();  // Todos os livros disponíveis
    private List<BookSlot> allSlots = new List<BookSlot>();  // Todos os slots para a solução
    
    private BookItem currentHighlightedBook;    // Livro atualmente destacado pelo cursor
    private BookItem selectedBook;              // Livro atualmente selecionado
    private Vector2Int cursorPosition;          // Posição atual do cursor virtual (agora usando Vector2Int)
    private bool playerInRange = false;         // Jogador está no alcance de interação
    
    // Solução correta do puzzle
    private Dictionary<BookSlot, BookItem> correctSolution;
    
    private void Awake()
    {
        // Inicializar listas
        allBooks = new List<BookItem>();
        allSlots = new List<BookSlot>();
        
        // Inicializar solução correta
        correctSolution = new Dictionary<BookSlot, BookItem>()
        {
            { spadesSlot, blueHorizontalBook },
            { clubsSlot, redYellowBook },
            { diamondsSlot, greenHorizontalBook },
            { heartsSlot, purpleSpottedBook }
        };
        
        // Encontrar todos os livros na cena
        BookItem[] booksInScene = FindObjectsByType<BookItem>(FindObjectsSortMode.None);
        foreach (BookItem book in booksInScene)
        {
            if (!(book is BookSlot))  // Não adicionar slots à lista de livros
            {
                allBooks.Add(book);
                book.SetPuzzleManager(this);  // Vincular ao gerenciador
            }
        }
        
        // Encontrar todos os slots na cena
        BookSlot[] slotsInScene = FindObjectsByType<BookSlot>(FindObjectsSortMode.None);
        foreach (BookSlot slot in slotsInScene)
        {
            allSlots.Add(slot);
            slot.SetPuzzleManager(this);  // Vincular ao gerenciador
        }
        
        // Desativar componentes de UI no início
        if (puzzleUI != null)
            puzzleUI.SetActive(false);
            
        if (interactionKeyIcon != null)
            interactionKeyIcon.SetActive(false);
            
        if (successFeedback != null)
            successFeedback.SetActive(false);
            
        if (failureFeedback != null)
            failureFeedback.SetActive(false);
            
        Debug.Log("[BOOKSHELF PUZZLE] Inicializado com " + allBooks.Count + " livros e " + allSlots.Count + " slots");
        InitializeBookGrid();
    }

    private void InitializeBookGrid()
    {
        // Inicializar a matriz de livros
        bookGrid = new BookItem[rowCount, colCount];
        
        // Mapear cada livro para sua posição inicial na grade
        MapGridPositions();
        
        // Validar se todos os slots foram preenchidos corretamente
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {
                if (bookGrid[row, col] == null)
                {
                    Debug.LogWarning($"[BOOKSHELF PUZZLE] Posição vazia na matriz: [{row}, {col}]");
                }
                else 
                {
                    Debug.Log($"[BOOKSHELF PUZZLE] Posição [{row}, {col}]: {bookGrid[row, col].name}");
                }
            }
        }
        
        Debug.Log("[BOOKSHELF PUZZLE] Matriz de livros inicializada");
    }
    
    // Mapeia cada livro para sua posição na grade com base em suas posições físicas
    private void MapGridPositions()
    {
        // Posição aproximada de cada prateleira (ajuste conforme necessário)
        float[] shelfYPositions = new float[rowCount];
        float[] shelfXPositions = new float[colCount];
        
        // MODIFICAÇÃO 1: Vamos usar nomes mais específicos para encontrar os livros
        
        // Primeira prateleira (de cima) - row 0
        bookGrid[0, 0] = FindBookByName("RedStarBook"); // Primeiro livro vermelho
        bookGrid[0, 1] = redYellowBook;                 // Livro vermelho com detalhes amarelos
        bookGrid[0, 2] = blueHorizontalBook;            // Livro azul deitado
        bookGrid[0, 3] = FindBookByExactName("GreenBook");   // Livro verde
        
        // Segunda prateleira (do meio) - row 1
        // MODIFICAÇÃO 2: Usar nome mais específico para o livro azul vertical
        bookGrid[1, 0] = FindBookByExactName("BlueBook"); // Livro azul normal (vertical)
        bookGrid[1, 1] = FindBookByName("HorizontalRedBook"); // Livro vermelho deitado
        bookGrid[1, 2] = purpleSpottedBook;             // Livro roxo com detalhes amarelos pontilhados
        bookGrid[1, 3] = greenHorizontalBook;           // Livro verde deitado
        
        // Terceira prateleira (naipes) - row 2
        bookGrid[2, 0] = spadesSlot;                    // Espadas
        bookGrid[2, 1] = clubsSlot;                     // Paus
        bookGrid[2, 2] = diamondsSlot;                  // Ouros
        bookGrid[2, 3] = heartsSlot;                    // Copas
        
        // Imprimir debug para verificar o mapeamento
        for (int row = 0; row < rowCount; row++) {
            for (int col = 0; col < colCount; col++) {
                if (bookGrid[row, col] != null) {
                    Debug.Log($"Grid[{row},{col}] = {bookGrid[row, col].name}");
                } else {
                    Debug.Log($"Grid[{row},{col}] = NULL");
                }
            }
        }
    }
    // Método para encontrar livro pelo nome exato
    private BookItem FindBookByExactName(string bookName)
    {
        foreach (BookItem book in allBooks)
        {
            if (book.name == bookName)
                return book;
        }
        
        // Se não encontrar pelo nome exato, tentar com Contains como fallback
        foreach (BookItem book in allBooks)
        {
            if (book.name.Contains(bookName) && 
                !book.name.Contains("Horizontal") && 
                !book.name.Contains("Deitado"))
                return book;
        }
        
        Debug.LogWarning($"[BOOKSHELF PUZZLE] Livro não encontrado: {bookName}");
        return null;
    }

    // Encontra um livro pelo nome
    private BookItem FindBookByName(string bookName)
    {
        // Lista para armazenar todos os livros encontrados
        List<BookItem> foundBooks = new List<BookItem>();
        
        foreach (BookItem book in allBooks)
        {
            if (book.name.Contains(bookName))
                foundBooks.Add(book);
        }
        
        // Se encontrou exatamente um livro, retornar
        if (foundBooks.Count == 1)
            return foundBooks[0];
        
        // Se encontrou mais de um livro, tentar refinar a busca
        if (foundBooks.Count > 1)
        {
            Debug.LogWarning($"[BOOKSHELF PUZZLE] Múltiplos livros encontrados com nome '{bookName}': {foundBooks.Count}");
            
            // Listar todos os livros encontrados
            foreach (BookItem book in foundBooks)
            {
                Debug.Log($"- Encontrado: {book.name}");
            }
            
            // Tentar encontrar o mais específico (que não seja uma variante)
            foreach (BookItem book in foundBooks)
            {
                // Se o nome for exato, retornar este
                if (book.name == bookName)
                    return book;
            }
            
            // Se chegou aqui, não encontrou nome exato, retornar o primeiro (com aviso)
            Debug.LogWarning($"[BOOKSHELF PUZZLE] Usando o primeiro livro encontrado: {foundBooks[0].name}");
            return foundBooks[0];
        }
        
        // Se não encontrou nenhum, retornar null
        Debug.LogWarning($"[BOOKSHELF PUZZLE] Livro não encontrado: {bookName}");
        return null;
    }

    private void Update()
    {
        // Verificar interação quando o puzzle está ativo
        if (puzzleActive)
        {
            // Tecla E para sair da interação
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[BOOKSHELF PUZZLE] Saindo da interação com a estante");
                StopPuzzleInteraction();
                return;
            }
            
            // Navegação com WASD
            if (Input.GetKeyDown(KeyCode.W)) MoveCursor(Vector2Int.up);
            if (Input.GetKeyDown(KeyCode.A)) MoveCursor(Vector2Int.left);
            if (Input.GetKeyDown(KeyCode.S)) MoveCursor(Vector2Int.down);
            if (Input.GetKeyDown(KeyCode.D)) MoveCursor(Vector2Int.right);
            
            // Enter para selecionar/trocar
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SelectOrSwapBook();
            }
            
            // Atualizar visualização prévia
            UpdatePreview();
        }
        // Verificar interação do jogador quando não estiver no puzzle
        else if (playerInRange && !puzzleSolved && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[BOOKSHELF PUZZLE] Iniciando interação com a estante");
            StartPuzzleInteraction();
        }
    }
    
    // Movimento do cursor virtual - versão corrigida
    private void MoveCursor(Vector2Int direction)
    {
        // Armazenar posição atual antes de mover
        Vector2Int startPosition = cursorPosition;
        
        // Calcular nova posição (com Y invertido para navegação intuitiva)
        int newRow = cursorPosition.y - direction.y;
        int newCol = cursorPosition.x + direction.x;
        
        // Limitar dentro dos limites da estante
        newRow = Mathf.Clamp(newRow, 0, rowCount - 1);
        newCol = Mathf.Clamp(newCol, 0, colCount - 1);
        
        // CORREÇÃO PRINCIPAL: Verificação especial para a fileira dos naipes (row 2)
        // Se estamos tentando mover PARA a fileira de naipes
        if (newRow == 2 && cursorPosition.y != 2)
        {
            // Se não tem livro selecionado, procurar livros na fileira de naipes
            if (selectedBook == null)
            {
                bool hasAnyBook = false;
                int targetCol = -1;
                float minDistance = float.MaxValue;

                Debug.Log($"[BOOKSHELF PUZZLE] ==== Verificando fileira de naipes ==== (posição atual: {cursorPosition.x}, {cursorPosition.y})");
                
                // Verificar cada slot da fileira de naipes
                BookSlot[] slots = { spadesSlot, clubsSlot, diamondsSlot, heartsSlot };
                for (int col = 0; col < slots.Length; col++)
                {
                    BookSlot slot = slots[col];
                    if (slot != null)
                    {
                        Debug.Log($"[BOOKSHELF PUZZLE] Verificando {slot.name} na coluna {col}");
                        if (slot.CurrentBook != null)
                        {
                            hasAnyBook = true;
                            float distance = Mathf.Abs(col - cursorPosition.x);
                            Debug.Log($"[BOOKSHELF PUZZLE] Encontrado livro {slot.CurrentBook.name} - distância: {distance}");
                            
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                targetCol = col;
                                Debug.Log($"[BOOKSHELF PUZZLE] Nova menor distância: {minDistance} na coluna {targetCol}");
                            }
                        }
                    }
                }

                if (hasAnyBook)
                {
                    // Mover para o livro mais próximo
                    newCol = targetCol;
                    Debug.Log($"[BOOKSHELF PUZZLE] Movendo para o livro na coluna {targetCol}");
                }
                else
                {
                    Debug.Log("[BOOKSHELF PUZZLE] Não há livros na fileira de naipes para navegar");
                    return; // Manter posição atual
                }
            }
            else
            {
                // Se tem livro selecionado, permitir acesso (continua o código)
                Debug.Log("[BOOKSHELF PUZZLE] Permitindo acesso à fileira de naipes - livro selecionado");
            }
        }
        
        // Verificar se a nova posição está vazia
        bool positionIsEmpty = IsPositionEmpty(newRow, newCol);
        
        // CORREÇÃO: Caso especial para slots de naipes - eles não devem ser considerados vazios
        // quando um livro está selecionado (mesmo se não tiverem livro dentro deles)
        if (newRow == 2 && selectedBook != null)
        {
            BookItem item = bookGrid[newRow, newCol];
            if (item is BookSlot)
            {
                // Se estamos com livro selecionado e indo para um slot de naipe,
                // consideramos como posição não-vazia (para evitar a busca de outros itens)
                positionIsEmpty = false;
            }
        }
        
        if (positionIsEmpty)
        {
            // NOVA LÓGICA APRIMORADA: Encontrar o item não vazio mais próximo
            bool foundItem = false;
            
            if (direction.y != 0) // Navegação vertical (cima/baixo)
            {
                // NOVA SOLUÇÃO: Verificar se toda a fileira está vazia
                bool entireRowEmpty = true;
                for (int col = 0; col < colCount; col++)
                {
                    if (!IsPositionEmpty(newRow, col))
                    {
                        entireRowEmpty = false;
                        break;
                    }
                }
                
                // Se toda a fileira estiver vazia, tentar pular para a próxima fileira não vazia
                if (entireRowEmpty)
                {
                    Debug.Log($"[BOOKSHELF PUZZLE] Fileira {newRow} inteiramente vazia, tentando pular...");
                    
                    // Continuar na mesma direção até encontrar uma fileira não vazia
                    int searchRow = newRow;
                    while (true)
                    {
                        searchRow -= direction.y; // Continuar na mesma direção (-y para cima, +y para baixo)
                        
                        // Verificar se está nos limites
                        if (searchRow < 0 || searchRow >= rowCount)
                            break;
                        
                        // Verificar se esta fileira tem pelo menos um item
                        bool hasItem = false;
                        int bestCol = -1;
                        
                        // Tentar encontrar um item na mesma coluna primeiro
                        if (!IsPositionEmpty(searchRow, newCol))
                        {
                            hasItem = true;
                            bestCol = newCol;
                        }
                        else
                        {
                            // Procurar em toda a fileira, priorizando posições mais próximas da coluna atual
                            for (int offset = 1; offset < colCount; offset++)
                            {
                                // Verificar à esquerda e à direita com offsets crescentes
                                if (newCol - offset >= 0 && !IsPositionEmpty(searchRow, newCol - offset))
                                {
                                    hasItem = true;
                                    bestCol = newCol - offset;
                                    break;
                                }
                                
                                if (newCol + offset < colCount && !IsPositionEmpty(searchRow, newCol + offset))
                                {
                                    hasItem = true;
                                    bestCol = newCol + offset;
                                    break;
                                }
                            }
                        }
                        
                        if (hasItem)
                        {
                            newRow = searchRow;
                            newCol = bestCol;
                            foundItem = true;
                            Debug.Log($"[BOOKSHELF PUZZLE] Pulou para fileira {newRow}, coluna {newCol}");
                            break;
                        }
                    }
                    
                    // Se não encontrou nenhuma fileira com itens
                    if (!foundItem)
                    {
                        Debug.Log("[BOOKSHELF PUZZLE] Não encontrou nenhuma fileira com itens, mantendo posição original");
                        newRow = cursorPosition.y;
                        newCol = cursorPosition.x;
                        return;
                    }
                }
                else 
                {
                    // Buscar na mesma linha, priorizando esquerda
                    for (int offset = 1; offset < colCount; offset++)
                    {
                        // Verificar à esquerda com offsets crescentes
                        if (newCol - offset >= 0)
                        {
                            // CORREÇÃO: Tratamento especial para navegação para fileira de naipes
                            if (newRow == 2 && selectedBook == null)
                            {
                                // Se tentando ir para naipes sem livro selecionado, não considerar slot vazio
                                continue;
                            }
                            
                            if (!IsPositionEmpty(newRow, newCol - offset))
                            {
                                newCol = newCol - offset;
                                foundItem = true;
                                Debug.Log($"[BOOKSHELF PUZZLE] Encontrado item à esquerda em [{newRow}, {newCol}]");
                                break;
                            }
                        }
                        
                        // Verificar à direita com offsets crescentes
                        if (newCol + offset < colCount)
                        {
                            // CORREÇÃO: Tratamento especial para navegação para fileira de naipes
                            if (newRow == 2 && selectedBook == null)
                            {
                                // Se tentando ir para naipes sem livro selecionado, não considerar slot vazio
                                continue;
                            }
                            
                            if (!IsPositionEmpty(newRow, newCol + offset))
                            {
                                newCol = newCol + offset;
                                foundItem = true;
                                Debug.Log($"[BOOKSHELF PUZZLE] Encontrado item à direita em [{newRow}, {newCol}]");
                                break;
                            }
                        }
                    }
                    
                    // Se não encontrou nada na mesma linha, voltar para linha original
                    if (!foundItem)
                    {
                        Debug.Log("[BOOKSHELF PUZZLE] Nenhum item encontrado na linha de destino, mantendo linha original");
                        newRow = cursorPosition.y;
                        newCol = cursorPosition.x;
                        return;
                    }
                }
            }
            else if (direction.x != 0) // Navegação horizontal (esquerda/direita)
            {
                // Continuar na mesma direção até encontrar item não vazio
                int searchCol = newCol;
                while (true)
                {
                    searchCol += direction.x;
                    
                    // Verificar limites
                    if (searchCol < 0 || searchCol >= colCount)
                        break;
                    
                    // CORREÇÃO: Tratamento especial para navegação na fileira de naipes
                    if (newRow == 2 && selectedBook == null)
                    {
                        // Se tentando navegar horizontalmente na fileira de naipes sem livro selecionado
                        break;
                    }
                    
                    if (!IsPositionEmpty(newRow, searchCol))
                    {
                        newCol = searchCol;
                        foundItem = true;
                        Debug.Log($"[BOOKSHELF PUZZLE] Encontrado item na direção horizontal em [{newRow}, {newCol}]");
                        break;
                    }
                }
                
                // Se não encontrou nada, manter posição original
                if (!foundItem)
                {
                    Debug.Log("[BOOKSHELF PUZZLE] Nenhum item encontrado na direção horizontal, mantendo posição original");
                    newRow = cursorPosition.y;
                    newCol = cursorPosition.x;
                    return;
                }
            }
        }
        
        // Verificação adicional: Se a posição final for na fileira de naipes e não tem livro selecionado
        if (newRow == 2 && selectedBook == null)
        {
            // Verificar se o slot de destino tem um livro
            BookItem finalItem = bookGrid[newRow, newCol];
            if (finalItem is BookSlot finalSlot && finalSlot.CurrentBook == null)
            {
                Debug.Log("[BOOKSHELF PUZZLE] Posição final na fileira de naipes sem livro selecionado e slot vazio, movimento bloqueado");
                return;
            }
            // Se tem livro no slot, permitir acesso
            Debug.Log("[BOOKSHELF PUZZLE] Permitindo acesso ao slot de naipe - slot já contém um livro");
        }
        
        // Verificação adicional: Se a posição final ainda estiver vazia, não mover
        if (IsPositionEmpty(newRow, newCol))
        {
            Debug.Log("[BOOKSHELF PUZZLE] Posição final ainda está vazia, mantendo posição original");
            return;
        }
        
        // Atualizar posição do cursor
        cursorPosition = new Vector2Int(newCol, newRow);
        
        Debug.Log($"[BOOKSHELF PUZZLE] Cursor movido de ({startPosition.x}, {startPosition.y}) para: ({cursorPosition.x}, {cursorPosition.y}) - Fileira: {newRow+1}");
        
        // Atualizar o destaque visual
        UpdateHighlightedItem();
    }

    private bool IsPositionEmpty(int row, int col)
    {
        // Verificar se a posição é válida
        if (row >= 0 && row < rowCount && col >= 0 && col < colCount)
        {
            // Uma posição está vazia se não tiver nenhum item
            BookItem item = bookGrid[row, col];
            
            if (item == null)
                return true;
                    
            // Se for um slot vazio (sem livro), é considerado vazio
            if (item is BookSlot slot)
            {
                // NOVA LÓGICA: Se o slot tem um livro dentro, nunca é considerado vazio
                if (slot.CurrentBook != null)
                    return false;
                
                // CORREÇÃO IMPORTANTE: Slots na fileira de naipes são considerados 
                // NÃO vazios quando um livro está selecionado
                if (row == 2 && selectedBook != null)
                {
                    Debug.Log("[BOOKSHELF PUZZLE] Slot de naipe tratado como NÃO vazio - livro selecionado disponível");
                    return false; // Não é vazio para fins de navegação
                }
                
                return true;
            }
            
            // CORREÇÃO: Se for um slot na fileira dos naipes (row 2) 
            // e NÃO temos livro selecionado, agora verificamos se tem livro no slot
            if (row == 2 && item is BookSlot slot2 && selectedBook == null)
            {
                // Se tem livro, não é vazio; se não tem, é vazio
                bool isEmpty = slot2.CurrentBook == null;
                Debug.Log($"[BOOKSHELF PUZZLE] Slot de naipe {slot2.name} tratado como {(isEmpty ? "vazio" : "NÃO vazio")} - tem livro: {slot2.CurrentBook != null}");
                return isEmpty;
            }
        }
        
        return false;
    }
    
    // Atualiza o item que está destacado pelo cursor
    private void UpdateHighlightedItem()
    {
        // Remover destaque anterior
        if (currentHighlightedBook != null)
        {
            currentHighlightedBook.SetHighlighted(false);
        }
        
        // Encontrar novo livro/slot sob o cursor
        currentHighlightedBook = GetItemAtCursorPosition();
        
        // Aplicar destaque ao novo item
        if (currentHighlightedBook != null)
        {
            currentHighlightedBook.SetHighlighted(true);
            Debug.Log($"[BOOKSHELF PUZZLE] Destacando: {currentHighlightedBook.name}");
        }
        else
        {
            Debug.Log("[BOOKSHELF PUZZLE] Nenhum item para destacar na posição do cursor");
        }
        
        // Atualizar visualização prévia
        UpdatePreview();
    }

    private void CreatePreviewObject()
    {
        // Criar um novo objeto para a visualização prévia
        previewObject = new GameObject("BookPreview");
        previewObject.transform.SetParent(transform);
        
        // Adicionar SpriteRenderer
        previewRenderer = previewObject.AddComponent<SpriteRenderer>();
        
        // Definir sorting order alto para garantir que apareça na frente de tudo
        previewRenderer.sortingOrder = 20;  // Aumentei para garantir que esteja na frente
        
        // Inicialmente desativado
        previewObject.SetActive(false);
        
        Debug.Log("[BOOKSHELF PUZZLE] Objeto de visualização prévia criado");
    }

    private void UpdatePreview()
    {
        // Se não temos objeto de visualização prévia ainda, criar um
        if (previewObject == null)
            CreatePreviewObject();
        
        // Se não há livro selecionado, não mostrar visualização
        if (selectedBook == null)
        {
            previewObject.SetActive(false);
            return;
        }
        
        // Verificar se o cursor está sobre um espaço vazio
        int row = cursorPosition.y;
        int col = cursorPosition.x;
        
        // MODIFICAÇÃO AQUI: Caso especial para slots de naipes (fileira 2)
        bool shouldShowPreview = false;
        
        if (row == 2 && bookGrid[row, col] is BookSlot naipeSlot)
        {
            // Para slots de naipes, mostrar o preview se o slot estiver vazio
            shouldShowPreview = naipeSlot.CurrentBook == null;
            Debug.Log($"[BOOKSHELF PUZZLE] Slot de naipe: {naipeSlot.name}, tem livro: {naipeSlot.CurrentBook != null}, mostrar preview: {shouldShowPreview}");
        }
        else
        {
            // Para outras posições, usar lógica normal
            shouldShowPreview = IsPositionEmpty(row, col);
        }
        
        // Mostrar visualização apenas em posições válidas
        if (shouldShowPreview)
        {
            // Obter a posição do espaço vazio
            Vector3 previewPosition;
            float scaleMultiplier = 0.8f;  // Escala padrão
            
            // Se for um slot de naipe vazio (última fileira), aplicar escala menor
            if (bookGrid[row, col] is BookSlot currentSlot)
            {
                previewPosition = currentSlot.transform.position;
                // Usar uma escala ainda menor para a fileira dos naipes (última fileira)
                if (row == 2)  // Considerando que row 2 é a fileira dos naipes (última fileira)
                {
                    scaleMultiplier = 0.6f;  // Valor menor para a última fileira
                }
            }
            // Caso contrário, usar a posição original do grid
            else
            {
                previewPosition = CalculateGridPosition(row, col);
            }
            
            // Configurar a visualização prévia
            previewObject.SetActive(true);
            previewObject.transform.position = previewPosition;
            
            // Copiar o sprite do livro selecionado
            previewRenderer.sprite = selectedBook.bookSprite.sprite;
            
            // Aplicar transparência
            Color previewColor = selectedBook.bookSprite.color;
            previewColor.a = previewAlpha;
            previewRenderer.color = previewColor;
            
            // Usar escala com base na posição da fileira
            previewObject.transform.localScale = Vector3.one * scaleMultiplier;
        }
        else
        {
            // Se não está em um espaço vazio, esconder visualização
            previewObject.SetActive(false);
        }
    }

    // Método auxiliar para calcular a posição de um item no grid se você não tiver uma referência
    private Vector3 CalculateGridPosition(int row, int col)
    {
        // Este é um exemplo básico. Você precisará adaptar ao seu layout específico.
        // Uma opção é buscar posições de livros adjacentes e calcular a média.
        
        // Verifica livros adjacentes para estimar a posição
        Vector3 position = Vector3.zero;
        int adjacentCount = 0;
        
        // Verificar posições adjacentes (cima, baixo, esquerda, direita)
        int[,] adjacent = new int[,] { {-1, 0}, {1, 0}, {0, -1}, {0, 1} };
        
        for (int i = 0; i < 4; i++)
        {
            int checkRow = row + adjacent[i, 0];
            int checkCol = col + adjacent[i, 1];
            
            if (checkRow >= 0 && checkRow < rowCount && checkCol >= 0 && checkCol < colCount)
            {
                BookItem item = bookGrid[checkRow, checkCol];
                if (item != null)
                {
                    position += item.transform.position;
                    adjacentCount++;
                }
            }
        }
        
        // Se encontrou alguma posição adjacente, calcular a média
        if (adjacentCount > 0)
        {
            position /= adjacentCount;
        }
        else
        {
            // Fallback: usar alguma referência geral da estante
            // Você pode implementar um cálculo baseado na posição da estante
            position = transform.position + new Vector3(col * 1.0f, -row * 1.0f, 0);
        }
        
        return position;
    }

    // Encontra o livro ou slot na posição do cursor
    private BookItem GetItemAtCursorPosition()
    {
        int row = cursorPosition.y;
        int col = cursorPosition.x;
        
        // Verificar se a posição é válida
        if (row >= 0 && row < rowCount && col >= 0 && col < colCount)
        {
            return bookGrid[row, col];
        }
        
        return null;
    }
    
    // Seleciona ou troca o livro atual
    private void SelectOrSwapBook()
    {
        if (currentHighlightedBook == null) return;
        
        // Armazenar a posição atual do cursor antes de qualquer operação
        Vector2Int originalCursorPosition = cursorPosition;
        
        if (selectedBook == null)
        {
            // Tentando selecionar um item
            if (currentHighlightedBook is BookSlot slot)
            {
                // Se for um slot com livro, seleciona o livro
                if (slot.CurrentBook != null)
                {
                    selectedBook = slot.CurrentBook;
                    selectedBook.SetSelected(true);
                    // Remover o livro do slot
                    slot.RemoveBook();
                    Debug.Log($"[BOOKSHELF PUZZLE] Livro selecionado do slot: {selectedBook.name}");
                }
                else
                {
                    Debug.Log("[BOOKSHELF PUZZLE] Slot vazio, nada para selecionar");
                }
            }
            else 
            {
                // Selecionar um livro normal
                selectedBook = currentHighlightedBook;
                selectedBook.SetSelected(true);
                Debug.Log($"[BOOKSHELF PUZZLE] Livro selecionado: {selectedBook.name}");
            }
            UpdatePreview();
        }
        else
        {
            // Já tem um livro selecionado, tentando trocar/colocar
            if (currentHighlightedBook is BookSlot slot)
            {
                // Colocando em um slot
                if (slot.CurrentBook != null)
                {
                    // Trocar com o livro do slot
                    BookItem slotBook = slot.RemoveBook();
                    slot.SetBook(selectedBook);
                    
                    // O livro do slot agora é o selecionado
                    selectedBook.SetSelected(false);
                    slotBook.transform.position = selectedBook.originalPosition;
                    
                    // Atualizar a matriz de livros E as relações corretas
                    UpdateBookGridAfterSwap(selectedBook, slotBook);
                    
                    // IMPORTANTE: Atualizar as referências de slot dos livros
                    UpdateBookSlotReferences();
                    
                    selectedBook = null;
                    
                    Debug.Log($"[BOOKSHELF PUZZLE] Trocado com livro do slot");
                    
                    Debug.Log("[BOOKSHELF PUZZLE] Iniciando verificação de solução...");
                    // Verificar a solução após qualquer troca envolvendo slots
                    CheckSolution();
                }
                else
                {
                    // Slot vazio, apenas colocar
                    Vector3 oldPosition = selectedBook.transform.position;
                    slot.SetBook(selectedBook);
                    
                    // Atualizar a matriz de livros
                    UpdateBookGridAfterMove(selectedBook, FindGridPosition(selectedBook), FindGridPosition(slot));
                    
                    // IMPORTANTE: Atualizar as referências de slot dos livros
                    UpdateBookSlotReferences();
                    
                    selectedBook.SetSelected(false);
                    selectedBook = null;
                    
                    Debug.Log($"[BOOKSHELF PUZZLE] Livro colocado no slot");
                    
                    Debug.Log("[BOOKSHELF PUZZLE] Iniciando verificação de solução...");
                    // Verificar a solução após qualquer colocação em slot
                    CheckSolution();
                }
            }
            else if (currentHighlightedBook == selectedBook)
            {
                // Cancelar seleção
                selectedBook.SetSelected(false);
                selectedBook = null;
                Debug.Log("[BOOKSHELF PUZZLE] Seleção cancelada");
            }
            else
            {
                // Trocar posição com outro livro
                Vector3 tempPosition = selectedBook.transform.position;
                selectedBook.transform.position = currentHighlightedBook.transform.position;
                currentHighlightedBook.transform.position = tempPosition;
                
                // Armazenar uma referência ao livro que estava sob o cursor
                BookItem swappedBook = currentHighlightedBook;
                
                // Atualizar a matriz de livros
                UpdateBookGridAfterSwap(selectedBook, currentHighlightedBook);
                
                // IMPORTANTE: Atualizar as referências de slot dos livros
                UpdateBookSlotReferences();
                
                selectedBook.SetSelected(false);
                selectedBook = null;
                
                Debug.Log("[BOOKSHELF PUZZLE] Livros trocados de posição");
                CheckSolution();
            }
            
            if (previewObject != null)
                previewObject.SetActive(false);
        }
        
        // Garantir que o cursor permanece na mesma posição lógica após a troca
        cursorPosition = originalCursorPosition;
        
        // Atualizar o item destacado com base na posição atual do cursor
        UpdateHighlightedItem();
        
        Debug.Log($"[BOOKSHELF PUZZLE] Cursor mantido na posição: ({cursorPosition.x}, {cursorPosition.y})");
    }

    // Adicione este novo método para sincronizar as referências entre livros e slots
    private void UpdateBookSlotReferences()
    {
        Debug.Log("[BOOKSHELF PUZZLE] Atualizando todas as referências de slot...");
        
        // 1. Primeiro, limpar todas as referências de slot nos livros
        foreach (BookItem book in allBooks)
        {
            book.currentSlot = null;
        }
        
        // 2. Limpar todas as referências de livro nos slots
        foreach (BookSlot slot in allSlots)
        {
            // Importante: Não podemos usar RemoveBook() aqui para evitar efeitos colaterais
            // Estamos apenas limpando as referências internas para reconstruí-las corretamente
            slot._currentBook = null;
        }
        
        // 3. Reconstruir as referências com base nas posições físicas atuais
        foreach (BookSlot slot in allSlots)
        {
            // Encontrar o livro mais próximo a este slot
            BookItem closestBook = null;
            float closestDistance = 0.3f; // Limiar de distância - ajuste conforme necessário
            
            foreach (BookItem book in allBooks)
            {
                // Ignorar livros que já estão em slots
                if (book.currentSlot != null)
                    continue;
                    
                float distance = Vector3.Distance(slot.transform.position, book.transform.position);
                if (distance < closestDistance)
                {
                    closestBook = book;
                    closestDistance = distance;
                }
            }
            
            // Se encontrou um livro próximo, estabelecer a relação bidirecional
            if (closestBook != null)
            {
                // Estabelecer a relação bidirecional
                closestBook.currentSlot = slot;
                slot._currentBook = closestBook; // Acessando o campo privado diretamente para atualização
                
                Debug.Log($"[BOOKSHELF PUZZLE] Estabelecida relação: {closestBook.name} está no slot {slot.name} (distância: {closestDistance})");
            }
        }
        
        // 4. Atualizar a matriz de grid com as novas referências para os slots (fileira 2)
        for (int col = 0; col < colCount; col++)
        {
            if (bookGrid[2, col] is BookSlot slot)
            {
                // Garantir que o slot mantém sua referência na matriz
                bookGrid[2, col] = slot;
                
                Debug.Log($"[BOOKSHELF PUZZLE] Slot na matriz[2,{col}]: {slot.name}, contém: {(slot.CurrentBook != null ? slot.CurrentBook.name : "nada")}");
            }
        }
        
        Debug.Log("[BOOKSHELF PUZZLE] Todas as referências de slot foram atualizadas!");
    }

    // Encontra a posição na grade de um item
    private Vector2Int FindGridPosition(BookItem item)
    {
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {
                if (bookGrid[row, col] == item)
                {
                    return new Vector2Int(col, row);
                }
            }
        }
        return new Vector2Int(-1, -1); // Item não encontrado
    }
    
    // Atualiza a matriz após mover um livro
    private void UpdateBookGridAfterMove(BookItem movedBook, Vector2Int fromPos, Vector2Int toPos)
    {
        if (fromPos.x < 0 || toPos.x < 0) return; // Posição inválida
        
        // Remover o livro da posição original
        bookGrid[fromPos.y, fromPos.x] = null;
        
        // Colocar na nova posição
        bookGrid[toPos.y, toPos.x] = movedBook;
        
        Debug.Log($"[BOOKSHELF PUZZLE] Livro movido de [{fromPos.y},{fromPos.x}] para [{toPos.y},{toPos.x}]");
    }
    
    // Atualiza a matriz após trocar dois livros
    private void UpdateBookGridAfterSwap(BookItem book1, BookItem book2)
    {
        Vector2Int pos1 = FindGridPosition(book1);
        Vector2Int pos2 = FindGridPosition(book2);
        
        if (pos1.x < 0 || pos2.x < 0) return; // Posição inválida
        
        // Trocar os livros na matriz
        bookGrid[pos1.y, pos1.x] = book2;
        bookGrid[pos2.y, pos2.x] = book1;
        
        Debug.Log($"[BOOKSHELF PUZZLE] Trocados livros entre [{pos1.y},{pos1.x}] e [{pos2.y},{pos2.x}]");
    }
    
    // Verifica se o puzzle foi resolvido corretamente
    public void CheckSolution()
    {
        bool isSolved = true;
        
        // PRIMEIRO: garantir que todas as referências estejam atualizadas
        UpdateBookSlotReferences();
        
        // Verificar cada slot
        foreach (var kvp in correctSolution)
        {
            BookSlot slot = kvp.Key;
            BookItem correctBook = kvp.Value;
            
            // Verificar se o slot tem um livro
            if (slot.CurrentBook == null)
            {
                isSolved = false;
                Debug.Log($"[BOOKSHELF PUZZLE] Slot {slot.name} está vazio");
                continue; // Continuar verificando outros slots ao invés de retornar
            }
            
            // Verificar se é o livro correto
            if (slot.CurrentBook != correctBook)
            {
                isSolved = false;
                Debug.Log($"[BOOKSHELF PUZZLE] Solução incorreta: {slot.name} tem {slot.CurrentBook.name} mas deveria ter {correctBook.name}");
                continue; // Continuar verificando outros slots ao invés de retornar
            }
            
            // Verificar se o livro está na posição correta fisicamente
            float distance = Vector3.Distance(slot.transform.position, slot.CurrentBook.transform.position);
            if (distance > 0.3f) // Ajuste este valor conforme necessário
            {
                isSolved = false;
                Debug.Log($"[BOOKSHELF PUZZLE] Livro {slot.CurrentBook.name} não está bem posicionado no slot {slot.name} (distância: {distance})");
                continue; // Continuar verificando outros slots ao invés de retornar
            }
            
            Debug.Log($"[BOOKSHELF PUZZLE] Verificação OK: {slot.name} tem {correctBook.name} na posição correta");
        }
        
        // Adicionar mais logs para debug
        if (isSolved)
        {
            Debug.Log("[BOOKSHELF PUZZLE] Todos os livros estão nos slots corretos!");
            Debug.Log("[BOOKSHELF PUZZLE] Puzzle resolvido corretamente!");
            PuzzleSolved();
        }
        else
        {
            Debug.Log("[BOOKSHELF PUZZLE] Ainda não resolvido - continue tentando!");
        }
    }
    
    // Inicia a interação com o puzzle
    public void StartPuzzleInteraction()
    {
        puzzleActive = true;
        
        // Esconder ícone de interação
        if (interactionKeyIcon != null)
            interactionKeyIcon.SetActive(false);
        
        // Desativar o jogador e componentes relacionados
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("[BOOKSHELF PUZZLE] Desativando controles do jogador");
            playerController.enabled = false;
            playerController.spriteRenderer.enabled = false;
            
            // Desativar InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = false;
                
            // Desativar outros componentes do jogador
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(false);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(false);
        }
        
        // Ativar UI do puzzle
        if (puzzleUI != null)
        {
            puzzleUI.SetActive(true);
            Debug.Log("[BOOKSHELF PUZZLE] UI de interação ativada");
        }
        
        // Configurar câmera
        if (cameraController != null && bookshelfPosition != null)
        {
            Debug.Log("[BOOKSHELF PUZZLE] Ativando câmera do puzzle");
            cameraController.ActivateShelfPuzzleCamera(bookshelfPosition, puzzleCameraSize);
        }
        
        // Inicializar cursor no primeiro livro
        cursorPosition = new Vector2Int(0, 0);
        UpdateHighlightedItem();
    }
    
    // Finaliza a interação com o puzzle
    public void StopPuzzleInteraction()
    {
        puzzleActive = false;
        
        // Mostrar ícone de interação se ainda não resolvido
        if (interactionKeyIcon != null && playerInRange && !puzzleSolved)
            interactionKeyIcon.SetActive(true);
        
        // Esconder visualização prévia
        if (previewObject != null)
            previewObject.SetActive(false);
        
        // [resto do código permanece igual]
        
        // Reativar jogador
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("[BOOKSHELF PUZZLE] Reativando controles do jogador");
            playerController.enabled = true;
            playerController.spriteRenderer.enabled = true;
            
            // Reativar InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = true;
                
            // Reativar outros componentes do jogador
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(true);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(true);
        }
        
        // Desativar UI do puzzle
        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
            Debug.Log("[BOOKSHELF PUZZLE] UI de interação desativada");
        }
        
        // Retornar câmera ao normal
        if (cameraController != null)
        {
            Debug.Log("[BOOKSHELF PUZZLE] Retornando para a câmera principal");
            cameraController.ReturnToMainCamera();
        }
        
        // Limpar seleções
        if (selectedBook != null)
        {
            selectedBook.SetSelected(false);
            selectedBook = null;
        }
        
        if (currentHighlightedBook != null)
        {
            currentHighlightedBook.SetHighlighted(false);
            currentHighlightedBook = null;
        }
    }
    
    // Chamado quando o puzzle é resolvido
    private void PuzzleSolved()
    {
        puzzleSolved = true;
        
        // Ativar feedback de sucesso
        if (successFeedback != null)
            successFeedback.SetActive(true);
        
        Debug.Log("[BOOKSHELF PUZZLE] Puzzle resolvido! Completando interação.");
        
        // Fechar interação
        StopPuzzleInteraction();
    }
    
    // Detector para quando o jogador entra no alcance
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[BOOKSHELF PUZZLE] Jogador entrou na área de interação");
            
            // Mostrar ícone de interação se o puzzle ainda não foi resolvido
            if (!puzzleActive && !puzzleSolved && interactionKeyIcon != null)
            {
                interactionKeyIcon.SetActive(true);
                Debug.Log("[BOOKSHELF PUZZLE] Mostrou ícone de interação");
            }
        }
    }
    
    // Detector para quando o jogador sai do alcance
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("[BOOKSHELF PUZZLE] Jogador saiu da área de interação");
            
            // Esconder ícone de interação
            if (interactionKeyIcon != null)
            {
                interactionKeyIcon.SetActive(false);
                Debug.Log("[BOOKSHELF PUZZLE] Escondeu ícone de interação");
            }
        }
    }
    
    // Método para implementação da interface IInteractable, se necessário
    public void Interact()
    {
        if (!puzzleActive && !puzzleSolved && playerInRange)
        {
            Debug.Log("[BOOKSHELF PUZZLE] Interação iniciada através da interface IInteractable");
            StartPuzzleInteraction();
        }
    }
}