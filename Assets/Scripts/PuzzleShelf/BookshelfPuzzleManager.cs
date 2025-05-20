using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using System;

public class BookshelfPuzzleManager : MonoBehaviour, ISaveable
{   
        [Serializable]
    public class BookData
    {
        public string bookName;
        public Vector3 position;
        public int gridRow;
        public int gridCol;
        public string currentSlotName; // Nome do slot atual, null se não estiver em slot
    }
    
    [Serializable]
    public class SlotData
    {
        public string slotName;
        public string currentBookName; // Nome do livro atual no slot, null se vazio
    }
    
    [Serializable]
    public class SaveData
    {
        public bool puzzleActive;
        public bool puzzleSolved;
        public List<BookData> books = new List<BookData>();
        public List<SlotData> slots = new List<SlotData>();
        public Vector2Int cursorPosition;
        public string selectedBookName; // Nome do livro selecionado, null se nenhum
    }

    public object GetSaveData()
    {
        SaveData data = new SaveData
        {
            puzzleActive = this.puzzleActive,
            puzzleSolved = this.puzzleSolved,
            cursorPosition = this.cursorPosition,
            selectedBookName = selectedBook != null ? selectedBook.name : null,
            books = new List<BookData>(),
            slots = new List<SlotData>()
        };
        
        // Salvar dados de todos os livros
        foreach (BookItem book in allBooks)
        {
            BookData bookData = new BookData
            {
                bookName = book.name,
                position = book.transform.position,
                currentSlotName = book.currentSlot != null ? book.currentSlot.name : null
            };
            
            // Encontrar posição na grade
            Vector2Int gridPos = FindGridPosition(book);
            bookData.gridRow = gridPos.y;
            bookData.gridCol = gridPos.x;
            
            data.books.Add(bookData);
        }
        
        // Salvar dados de todos os slots
        foreach (BookSlot slot in allSlots)
        {
            SlotData slotData = new SlotData
            {
                slotName = slot.name,
                currentBookName = slot.CurrentBook != null ? slot.CurrentBook.name : null
            };
            
            data.slots.Add(slotData);
        }
        
        return data;
    }

    public void LoadFromSaveData(object saveData)
    {
        SaveData data = saveData as SaveData;
        if (data == null) return;
        
        // Restaurar estado do puzzle
        puzzleActive = data.puzzleActive;
        puzzleSolved = data.puzzleSolved;
        cursorPosition = data.cursorPosition;
        
        // Certificar-se de que a grade está inicializada
        if (bookGrid == null)
        {
            InitializeBookGrid();
        }
        
        // Limpar o estado atual da grade
        ClearBookGrid();
        
        // Restaurar posições dos livros
        foreach (BookData bookData in data.books)
        {
            BookItem book = FindBookByExactName(bookData.bookName);
            if (book != null)
            {
                // Restaurar posição física
                book.transform.position = bookData.position;
                
                // Restaurar posição na grade se válida
                if (bookData.gridRow >= 0 && bookData.gridRow < rowCount && 
                    bookData.gridCol >= 0 && bookData.gridCol < colCount)
                {
                    bookGrid[bookData.gridRow, bookData.gridCol].SetBook(book);
                }
            }
        }
        
        // Restaurar estados dos slots
        foreach (SlotData slotData in data.slots)
        {
            BookSlot slot = FindSlotByName(slotData.slotName);
            if (slot != null && !string.IsNullOrEmpty(slotData.currentBookName))
            {
                BookItem book = FindBookByExactName(slotData.currentBookName);
                if (book != null)
                {
                    slot.SetBook(book);
                }
            }
        }
        
        // Restaurar livro selecionado
        selectedBook = null;
        if (!string.IsNullOrEmpty(data.selectedBookName))
        {
            selectedBook = FindBookByExactName(data.selectedBookName);
            if (selectedBook != null)
            {
                selectedBook.SetSelected(true);
            }
        }
        
        // Atualizar todas as referências
        UpdateBookSlotReferences();
        
        // Atualizar UI e configurações visuais
        UpdateHighlightedItem();
        
        // Se o puzzle estava ativo, restaurar o estado de interação
        if (puzzleActive)
        {
            // Ativar UI do puzzle
            if (puzzleUI != null)
                puzzleUI.SetActive(true);
                
            if (Light != null)
                Light.SetActive(true);
                
            // Configurar câmera
            if (cameraController != null && bookshelfPosition != null)
            {
                cameraController.ActivateShelfPuzzleCamera(bookshelfPosition, puzzleCameraSize);
            }
        }
        else
        {
            // Mostrar ícone de interação se necessário
            if (interactionKeyIcon != null && playerInRange && !puzzleSolved)
                interactionKeyIcon.SetActive(true);
                
            if (Light != null)
                Light.SetActive(false);
                
            if (puzzleUI != null)
                puzzleUI.SetActive(false);
        }
        
        // Se o puzzle foi resolvido, mostrar feedback
        if (puzzleSolved && successFeedback != null)
        {
            successFeedback.SetActive(true);
            
            MoveBookshelfSmoothly(0f);
        }
    }
    
    // Método helper para limpar o estado atual da grade
    private void ClearBookGrid()
    {
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {
                // Não remover a referência ao slot
                BookSlot currentSlot = bookGrid[row, col].Slot;
                bookGrid[row, col].SetBook(null);
                
                // Manter os slots na última linha
                if (row == 2 && currentSlot != null)
                {
                    bookGrid[row, col].SetSlot(currentSlot);
                }
            }
        }
    }
    
    // Método helper para encontrar um slot pelo nome
    private BookSlot FindSlotByName(string slotName)
    {
        foreach (BookSlot slot in allSlots)
        {
            if (slot.name == slotName)
                return slot;
        }
        return null;
    }

    [System.Serializable]
    private class GridCell
    {
        public BookSlot Slot { get; private set; }    
        public BookItem Book { get; private set; }    
        public Vector3 WorldPosition { get; private set; }    

        public GridCell(Transform bookshelfTransform, Vector2 localOffset)
        {
            // Converte posição local para mundial baseada no transform da estante
            WorldPosition = bookshelfTransform.TransformPoint(new Vector3(localOffset.x, localOffset.y, 0));
            Slot = null;
            Book = null;
        }

        public void SetSlot(BookSlot slot)
        {
            Slot = slot;
            if (slot != null)
            {
                // Usar posição local em relação à estante
                slot.transform.position = WorldPosition;
            }
        }

        public void SetBook(BookItem book)
        {
            Book = book;
            if (book != null)
            {
                // Usar posição local em relação à estante
                book.transform.position = WorldPosition;
                
                if (Slot != null)
                {
                    Slot._currentBook = book;
                    book.currentSlot = Slot;
                }
            }
        }

        public bool IsEmpty()
        {
            if (Slot != null)
            {
                return Book == null;
            }
            return Book == null;
        }
    }

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
    public GameObject Light;
    public GameObject successFeedback;      // Feedback visual de sucesso
    public GameObject failureFeedback;      // Feedback visual de falha
    
    [Header("UI de Naipes")]
    public GameObject spadesUIHighlight;    // Destaque UI para o naipe de espadas
    public GameObject clubsUIHighlight;      // Destaque UI para o naipe de paus
    public GameObject diamondsUIHighlight;   // Destaque UI para o naipe de ouros
    public GameObject heartsUIHighlight;     // Destaque UI para o naipe de copas

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
    private GridCell[,] bookGrid;          // Matriz para armazenar os livros por posição lógica
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

        if (Light != null)
            Light.SetActive(false);    
            
        if (successFeedback != null)
            successFeedback.SetActive(false);
            
        if (failureFeedback != null)
            failureFeedback.SetActive(false);
            
        Debug.Log("[BOOKSHELF PUZZLE] Inicializado com " + allBooks.Count + " livros e " + allSlots.Count + " slots");
        InitializeBookGrid();
    }
//ponto de partida
private void InitializeBookGrid()
{
    bookGrid = new GridCell[rowCount, colCount];
    
    // Definir posições locais para cada célula
    Vector2[,] localPositions = new Vector2[3, 4] 
    {
        // Primeira fileira (row 0)
        { 
            new Vector2(0.05f*0.75f -0.291f, 0.29f*0.75f +0.0845f),   // [0,0]
            new Vector2(0.29f*0.75f -0.291f, 0.29f*0.75f +0.0845f),   // [0,1]
            new Vector2(0.509f*0.75f -0.291f, 0.29f*0.75f +0.0845f),  // [0,2]
            new Vector2(0.74f*0.75f -0.291f, 0.29f*0.75f +0.0845f)    // [0,3]
        },
        // Segunda fileira (row 1)
        { 
            new Vector2(0.05f*0.75f -0.291f, -0.033f*0.75f+0.0845f),   // [1,0]
            new Vector2(0.29f*0.75f -0.291f, -0.033f*0.75f+0.0845f),   // [1,1]
            new Vector2(0.509f*0.75f -0.291f, -0.033f*0.75f+0.0845f),  // [1,2]
            new Vector2(0.74f*0.75f -0.291f, -0.033f*0.75f+0.0845f)    // [1,3]
        },
        // Fileira dos naipes (row 2)
        { 
            new Vector2(-0.245f, -0.18f),  // [2,0]
            new Vector2(-0.075f, -0.18f),  // [2,1]
            new Vector2(0.0906f, -0.18f),  // [2,2]
            new Vector2(0.2649f, -0.18f)   // [2,3]
        }
    };

    // Ajuste esses valores conforme necessário para sua escala e layout

    // Criar células com posições locais
    for (int row = 0; row < rowCount; row++)
    {
        for (int col = 0; col < colCount; col++)
        {
            bookGrid[row, col] = new GridCell(transform, localPositions[row, col]);
            Debug.Log($"Criada célula [{row},{col}] na posição local: {localPositions[row, col]}, posição mundial: {bookGrid[row, col].WorldPosition}");
        }
    }

    // Configurar slots na última linha (row 2)
    bookGrid[2, 0].SetSlot(spadesSlot);
    bookGrid[2, 1].SetSlot(clubsSlot);
    bookGrid[2, 2].SetSlot(diamondsSlot);
    bookGrid[2, 3].SetSlot(heartsSlot);
    
    MapGridPositions();
}
    
    // Mapeia cada livro para sua posição na grade com base em suas posições físicas
    private void MapGridPositions()
    {
        // Primeira prateleira (row 0)
        bookGrid[0, 0].SetBook(FindBookByName("RedStarBook"));
        bookGrid[0, 1].SetBook(redYellowBook);
        bookGrid[0, 2].SetBook(blueHorizontalBook);
        bookGrid[0, 3].SetBook(FindBookByExactName("GreenBook"));
        
        // Segunda prateleira (row 1)
        bookGrid[1, 0].SetBook(FindBookByExactName("BlueBook"));
        bookGrid[1, 1].SetBook(FindBookByName("HorizontalRedBook"));
        bookGrid[1, 2].SetBook(purpleSpottedBook);
        bookGrid[1, 3].SetBook(greenHorizontalBook);
        
        // Debug do estado inicial
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {
                var cell = bookGrid[row, col];
                string content = cell.Book != null ? cell.Book.name : "vazio";
                string slotInfo = cell.Slot != null ? $" (Slot: {cell.Slot.name})" : "";
                Debug.Log($"Grid[{row},{col}] = {content}{slotInfo}");
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
    Vector2Int startPosition = cursorPosition;
    
    // Lógica diferente baseada se um livro está selecionado ou não
    if (selectedBook != null)
    {
        // Com livro selecionado, podemos navegar para espaços vazios
        MoveWithSelectedBook(direction);
    }
    else
    {
        // Sem livro selecionado, comportamento original (apenas espaços ocupados)
        MoveWithoutSelectedBook(direction);
    }
    
    // Atualizar o item destacado
    UpdateHighlightedItem();
}

// Método para mover o cursor quando um livro está selecionado (pode ir para espaços vazios)
private void MoveWithSelectedBook(Vector2Int direction)
{
    // Inverter a direção Y para corresponder à lógica do grid (positivo = baixo)
    if (direction == Vector2Int.up)
    {
        direction = Vector2Int.down;
    }
    else if (direction == Vector2Int.down)
    {
        direction = Vector2Int.up;
    }
    
    Vector2Int targetPosition = cursorPosition + direction;
    
    // Verificar se a posição alvo está dentro dos limites da grade
    if (targetPosition.x >= 0 && targetPosition.x < colCount && 
        targetPosition.y >= 0 && targetPosition.y < rowCount)
    {
        // Mover para a posição alvo diretamente se estiver dentro dos limites
        cursorPosition = targetPosition;
        Debug.Log($"Cursor com livro selecionado movido para: ({cursorPosition.x}, {cursorPosition.y})");
        return;
    }
    
    // Se chegou aqui, a posição alvo está fora dos limites
    Debug.Log("Tentativa de mover para fora dos limites da grade");
}

// Método original de movimento do cursor (apenas para espaços ocupados)
private void MoveWithoutSelectedBook(Vector2Int direction)
{
    // Pegar o item atual
    BookItem currentItem = GetItemAtCursorPosition();
    if (currentItem == null) return;

    Vector3 currentPosition = currentItem.transform.position;
    BookItem nextItem = null;
    float minDistance = float.MaxValue;
    bool foundInColumn = false;
    
    // Primeiro, tentar encontrar na mesma coluna (comportamento original)
    for (int row = 0; row < rowCount; row++)
    {
        for (int col = 0; col < colCount; col++)
        {
            BookItem gridItem = bookGrid[row, col].Slot ?? bookGrid[row, col].Book;
            if (gridItem == null || gridItem == currentItem) continue;

            Vector3 itemPosition = gridItem.transform.position;
            Vector3 difference = itemPosition - currentPosition;

            bool isInDirection = false;
            if (direction == Vector2Int.right && difference.x > 0.1f && Mathf.Abs(difference.y) < 0.5f)
                isInDirection = true;
            else if (direction == Vector2Int.left && difference.x < -0.1f && Mathf.Abs(difference.y) < 0.5f)
                isInDirection = true;
            else if (direction == Vector2Int.up && difference.y > 0.1f && Mathf.Abs(difference.x) < 0.5f)
            {
                isInDirection = true;
                if (col == cursorPosition.x) foundInColumn = true;
            }
            else if (direction == Vector2Int.down && difference.y < -0.1f && Mathf.Abs(difference.x) < 0.5f)
                isInDirection = true;

            if (isInDirection)
            {
                float distance = Vector3.Distance(currentPosition, itemPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nextItem = gridItem;
                    cursorPosition = new Vector2Int(col, row);
                }
            }
        }
    }

    // Se não encontrou nenhum item acima na mesma coluna, procurar o mais próximo
    if (direction == Vector2Int.up && !foundInColumn)
    {
        minDistance = float.MaxValue;
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {
                BookItem gridItem = bookGrid[row, col].Slot ?? bookGrid[row, col].Book;
                if (gridItem == null || gridItem == currentItem) continue;

                Vector3 itemPosition = gridItem.transform.position;
                Vector3 difference = itemPosition - currentPosition;

                if (difference.y > 0.1f) // Se está acima
                {
                    float distance = Vector3.Distance(currentPosition, itemPosition);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nextItem = gridItem;
                        cursorPosition = new Vector2Int(col, row);
                    }
                }
            }
        }
    }

    if (nextItem != null)
    {
        Debug.Log($"Cursor movido para: ({cursorPosition.x}, {cursorPosition.y}) - Item: {nextItem.name}");
    }
}
//ponto de partida
    private bool IsPositionEmpty(int row, int col)
    {
        if (row >= 0 && row < rowCount && col >= 0 && col < colCount)
        {
            GridCell cell = bookGrid[row, col];
            
            // Se for um slot
            if (cell.Slot != null)
            {
                // Se tem livro selecionado, slot nunca está vazio
                if (selectedBook != null)
                    return false;
                    
                // Senão, está vazio se não tiver livro
                return cell.Book == null;
            }
            
            // Para posições normais, está vazio se não tiver livro
            return cell.Book == null;
        }
        return false;
    }
    
    // Atualiza o item que está destacado pelo cursor
    private void UpdateHighlightedItem()
{
    // 1. Primeiro, remover TODOS os highlights
    foreach (BookItem book in allBooks)
    {
        book.SetHighlighted(false);
    }
    foreach (BookSlot slot in allSlots)
    {
        slot.SetHighlighted(false);
    }

    // 2. Encontrar novo livro/slot sob o cursor
    currentHighlightedBook = GetItemAtCursorPosition();
    
    // 3. Aplicar destaque APENAS ao item atual
    if (currentHighlightedBook != null)
    {
        currentHighlightedBook.SetHighlighted(true);
        Debug.Log($"[BOOKSHELF PUZZLE] Destacando: {currentHighlightedBook.name}");
    }
    else if (selectedBook != null)
    {
        // Estamos em uma posição vazia com um livro selecionado
        // Não destacamos nada, mas mostramos o preview
        Debug.Log("[BOOKSHELF PUZZLE] Mostrando preview em posição vazia");
        UpdatePreview();
    }
    else
    {
        Debug.Log("[BOOKSHELF PUZZLE] Nenhum item para destacar na posição do cursor");
    }
    
    // 4. Atualizar UI dos naipes
    UpdateSuitUIHighlights();
    
    // 5. Atualizar visualização prévia
    UpdatePreview();
}

    // Atualiza o destaque visual dos naipes na UI
    private void UpdateSuitUIHighlights()
    {
        // Desativar todos primeiro
        spadesUIHighlight.SetActive(false);
        clubsUIHighlight.SetActive(false);
        diamondsUIHighlight.SetActive(false);
        heartsUIHighlight.SetActive(false);
        
        // Se não estivermos na fileira de naipes (row 2), não mostrar nenhum destaque
        if (cursorPosition.y != 2)
            return;
        
        // Verificar qual slot de naipe está atualmente selecionado
        switch (cursorPosition.x)
        {
            case 0: // Espadas (primeiro slot)
                spadesUIHighlight.SetActive(true);
                break;
            case 1: // Paus (segundo slot)
                clubsUIHighlight.SetActive(true);
                break;
            case 2: // Ouros (terceiro slot)
                diamondsUIHighlight.SetActive(true);
                break;
            case 3: // Copas (quarto slot)
                heartsUIHighlight.SetActive(true);
                break;
        }
        
        Debug.Log($"[BOOKSHELF PUZZLE] Destacando UI do naipe na coluna {cursorPosition.x}");
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
    
    int row = cursorPosition.y;
    int col = cursorPosition.x;
    bool shouldShowPreview = false;
    Vector3 previewPosition = Vector3.zero;
    float scaleMultiplier = 0.8f;  // Escala padrão
    
    // Verificar se estamos em uma posição válida
    if (row >= 0 && row < rowCount && col >= 0 && col < colCount)
    {
        GridCell cell = bookGrid[row, col];
        
        if (row == 2 && cell.Slot != null)
        {
            // Para slots de naipes, mostrar o preview se o slot estiver vazio
            shouldShowPreview = cell.Slot.CurrentBook == null;
            previewPosition = cell.Slot.transform.position; // Usar a posição atual do slot
            scaleMultiplier = 0.6f;  // Escala menor para slots de naipes
        }
        else
        {
            // Para outras posições, usar lógica normal
            shouldShowPreview = cell.Book == null; // Mostrar se não há livro na posição
            previewPosition = cell.WorldPosition;
        }
        
        if (shouldShowPreview)
        {
            // Configurar a visualização prévia
            previewObject.SetActive(true);
            previewObject.transform.position = previewPosition;
            
            // Copiar o sprite do livro selecionado
            previewRenderer.sprite = selectedBook.bookSprite.sprite;
            
            // Aplicar transparência
            Color previewColor = selectedBook.bookSprite.color;
            previewColor.a = previewAlpha;
            previewRenderer.color = previewColor;
            
            // Aplicar escala
            previewObject.transform.localScale = Vector3.one * scaleMultiplier;
        }
        else
        {
            previewObject.SetActive(false);
        }
    }
    else
    {
        // Fora da grade, não mostrar preview
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
                BookItem item = bookGrid[checkRow, checkCol].Book ?? bookGrid[checkRow, checkCol].Slot;
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
    
    if (row >= 0 && row < rowCount && col >= 0 && col < colCount)
    {
        GridCell cell = bookGrid[row, col];
        
        // Se tiver um slot, retornar o slot
        if (cell.Slot != null)
            return cell.Slot;
            
        // Se tiver um livro, retornar o livro
        if (cell.Book != null)
            return cell.Book;
            
        // Se tivermos um livro selecionado, retornar uma posição "fantasma" para preview
        if (selectedBook != null)
        {
            // Para posições vazias quando um livro está selecionado, 
            // podemos usar o preview para visualização
            UpdatePreview();
            return null; // Retorna null, mas o UpdateHighlightedItem saberá lidar com isso
        }
    }
    return null;
}
    
    // Seleciona ou troca o livro atual
    private void SelectOrSwapBook()
{
    int row = cursorPosition.y;
    int col = cursorPosition.x;
    bool isEmptyPosition = GetItemAtCursorPosition() == null;
    
    // Armazenar a posição atual do cursor antes de qualquer operação
    Vector2Int originalCursorPosition = cursorPosition;
    
    if (selectedBook == null)
    {
        // Tentando selecionar um item, mas se estivermos em uma posição vazia, não há nada para selecionar
        if (isEmptyPosition) return;
        
        // Continuar com a lógica normal para selecionar um item
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
        // Já tem um livro selecionado, tentando colocar em algum lugar
        
        if (isEmptyPosition)
        {
            // Estamos em uma posição vazia, colocar o livro aqui
            PlaceBookInEmptyPosition(row, col);
        }
        else if (currentHighlightedBook is BookSlot slot)
        {
            // Colocando em um slot
            if (slot.CurrentBook != null)
            {
                // Slot ocupado, trocar os livros
                Debug.Log("=== INICIANDO TROCA ENTRE SLOTS ===");
                
                // Guardar referências antes da troca
                BookItem slotBook = slot.CurrentBook;
                
                // Remover livro atual do slot
                slot.RemoveBook();
                
                // Colocar novo livro no slot
                slot.SetBook(selectedBook);
                
                // Colocar livro antigo na posição do livro selecionado
                slotBook.transform.position = selectedBook.originalPosition;
                
                // Atualizar grid uma única vez
                UpdateBookGridAfterSwap(selectedBook, slotBook);
                
                // Limpar seleção
                selectedBook.SetSelected(false);
                selectedBook = null;
                
                // Atualizar referências
                UpdateBookSlotReferences();
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
            Vector3 targetPosition = currentHighlightedBook.transform.position;
            
            Debug.Log($"=== INICIANDO TROCA DE LIVROS ===");
            Debug.Log($"Livro selecionado: {selectedBook.name} na posição {tempPosition}");
            Debug.Log($"Livro alvo: {currentHighlightedBook.name} na posição {targetPosition}");
            Debug.Log($"Posição do cursor antes da troca: ({cursorPosition.x}, {cursorPosition.y})");
            
            // Fazer a troca física
            selectedBook.transform.position = targetPosition;
            currentHighlightedBook.transform.position = tempPosition;
            
            // Armazenar uma referência ao livro que estava sob o cursor
            BookItem swappedBook = currentHighlightedBook;
            
            Debug.Log("=== ESTADO DA MATRIZ ANTES DA TROCA ===");
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    Debug.Log($"Grid[{r},{c}] = {(bookGrid[r, c].Book?.name ?? bookGrid[r, c].Slot?.name ?? "vazio")}");
                }
            }
            
            // Atualizar a matriz de livros
            UpdateBookGridAfterSwap(selectedBook, currentHighlightedBook);
            
            Debug.Log("=== ESTADO DA MATRIZ APÓS A TROCA ===");
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    Debug.Log($"Grid[{r},{c}] = {(bookGrid[r, c].Book?.name ?? bookGrid[r, c].Slot?.name ?? "vazio")}");
                }
            }
            
            // IMPORTANTE: Atualizar as referências de slot dos livros
            UpdateBookSlotReferences();
            
            // NOVO: Encontrar a nova posição de ambos os livros após a troca
            Vector2Int newSelectedPos = FindGridPosition(selectedBook);
            Vector2Int newSwappedPos = FindGridPosition(swappedBook);
            
            Debug.Log($"Nova posição do livro selecionado ({selectedBook.name}): ({newSelectedPos.x}, {newSelectedPos.y})");
            Debug.Log($"Nova posição do livro trocado ({swappedBook.name}): ({newSwappedPos.x}, {newSwappedPos.y})");
            
            selectedBook.SetSelected(false);
            selectedBook = null;
            
            Debug.Log("[BOOKSHELF PUZZLE] Livros trocados de posição");
            CheckSolution();
            
            Debug.Log($"=== ESTADO FINAL ===");
            Debug.Log($"Posição final do cursor: ({cursorPosition.x}, {cursorPosition.y})");
            Debug.Log($"Item sob o cursor: {GetItemAtCursorPosition()?.name ?? "nenhum"}");
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
private void PlaceBookInEmptyPosition(int row, int col)
{
    if (row < 0 || row >= rowCount || col < 0 || col >= colCount || selectedBook == null)
        return;
    
    GridCell cell = bookGrid[row, col];
    
    // Verificar se a célula está realmente vazia
    if (cell.Book != null)
    {
        Debug.LogWarning($"[BOOKSHELF PUZZLE] Tentativa de colocar em posição não vazia: [{row},{col}]");
        return;
    }
    
    // Colocar o livro na célula vazia
    Vector2Int oldPos = FindGridPosition(selectedBook);
    
    // Definir a posição do livro no mundo
    selectedBook.transform.position = cell.WorldPosition;
    
    // Atualizar a referência do livro na matriz
    cell.SetBook(selectedBook);
    
    // Se o livro estava em outra célula antes, limpar aquela referência
    if (oldPos.x >= 0 && oldPos.y >= 0)
    {
        bookGrid[oldPos.y, oldPos.x].SetBook(null);
    }
    
    // Limpar seleção
    selectedBook.SetSelected(false);
    selectedBook = null;
    
    Debug.Log($"[BOOKSHELF PUZZLE] Livro colocado na posição vazia: [{row},{col}]");
    
    // Atualizar referências
    UpdateBookSlotReferences();
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
            if (bookGrid[2, col].Slot != null)
            {
                BookSlot currentSlot = bookGrid[2, col].Slot;
                Debug.Log($"[BOOKSHELF PUZZLE] Slot na matriz[2,{col}]: {currentSlot.name}, contém: {(currentSlot.CurrentBook != null ? currentSlot.CurrentBook.name : "nada")}");
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
                GridCell cell = bookGrid[row, col];
                if (cell.Book == item || cell.Slot == item)
                {
                    return new Vector2Int(col, row);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }
        
    // Atualiza a matriz após mover um livro
private void UpdateBookGridAfterMove(BookItem movedBook, Vector2Int fromPos, Vector2Int toPos)
{
    if (fromPos.x < 0 || toPos.x < 0) return;
    
    // Remover o livro da posição original
    bookGrid[fromPos.y, fromPos.x].SetBook(null);
    
    // Colocar o livro na nova posição
    bookGrid[toPos.y, toPos.x].SetBook(movedBook);
    
    Debug.Log($"[BOOKSHELF PUZZLE] Livro movido de [{fromPos.y},{fromPos.x}] para [{toPos.y},{toPos.x}]");
}

private void UpdateBookGridAfterSwap(BookItem book1, BookItem book2)
{
    Debug.Log($"=== INICIANDO TROCA DE GRID ===");
    Debug.Log($"Tentando trocar: {book1.name} com {book2.name}");

    // Mostrar estado inicial do grid
    Debug.Log("Estado inicial do grid:");
    for (int row = 0; row < rowCount; row++)
    {
        for (int col = 0; col < colCount; col++)
        {
            var content = bookGrid[row, col].Book != null ? bookGrid[row, col].Book.name : "vazio";
            Debug.Log($"Grid[{row},{col}] = {content}");
        }
    }

    GridCell cell1 = null;
    GridCell cell2 = null;
    Vector2Int pos1 = Vector2Int.zero;
    Vector2Int pos2 = Vector2Int.zero;

    // Encontrar as células que contêm os livros
    for (int row = 0; row < rowCount; row++)
    {
        for (int col = 0; col < colCount; col++)
        {
            if (bookGrid[row, col].Book == book1)
            {
                cell1 = bookGrid[row, col];
                pos1 = new Vector2Int(col, row);
                Debug.Log($"Encontrado {book1.name} na posição [{row},{col}]");
            }
            if (bookGrid[row, col].Book == book2)
            {
                cell2 = bookGrid[row, col];
                pos2 = new Vector2Int(col, row);
                Debug.Log($"Encontrado {book2.name} na posição [{row},{col}]");
            }
        }
    }

    if (cell1 != null && cell2 != null)
    {
        Debug.Log($"Executando troca entre posições [{pos1.y},{pos1.x}] e [{pos2.y},{pos2.x}]");
        
        // Trocar os livros
        BookItem tempBook = cell1.Book;
        Debug.Log($"Guardando {tempBook.name} temporariamente");
        
        cell1.SetBook(cell2.Book);
        Debug.Log($"Movendo {cell2.Book.name} para [{pos1.y},{pos1.x}]");
        
        cell2.SetBook(tempBook);
        Debug.Log($"Movendo {tempBook.name} para [{pos2.y},{pos2.x}]");

        // Atualizar lastKnownGridPosition
        if (book1 != null)
        {
            book1.lastKnownGridPosition = pos2;
            Debug.Log($"Atualizando lastKnownGridPosition de {book1.name} para [{pos2.y},{pos2.x}]");
        }
        if (book2 != null)
        {
            book2.lastKnownGridPosition = pos1;
            Debug.Log($"Atualizando lastKnownGridPosition de {book2.name} para [{pos1.y},{pos1.x}]");
        }
    }
    else
    {
        Debug.LogError("Não foi possível encontrar uma ou ambas as células para troca!");
    }

    // Mostrar estado final do grid
    Debug.Log("Estado final do grid:");
    for (int row = 0; row < rowCount; row++)
    {
        for (int col = 0; col < colCount; col++)
        {
            var content = bookGrid[row, col].Book != null ? bookGrid[row, col].Book.name : "vazio";
            Debug.Log($"Grid[{row},{col}] = {content}");
        }
    }
    
    Debug.Log("=== FIM DA TROCA DE GRID ===");
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

        if (Light != null)
            Light.SetActive(true);    
        
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
        UpdateSuitUIHighlights();
        UpdateHighlightedItem();
    }
    
    // Finaliza a interação com o puzzle
    public void StopPuzzleInteraction()
    {
        puzzleActive = false;
        
        // Mostrar ícone de interação se ainda não resolvido
        if (interactionKeyIcon != null && playerInRange && !puzzleSolved)
            interactionKeyIcon.SetActive(true);

        if (Light != null)
            Light.SetActive(false);    
        
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
            spadesUIHighlight.SetActive(false);
            clubsUIHighlight.SetActive(false);
            diamondsUIHighlight.SetActive(false);
            heartsUIHighlight.SetActive(false);
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
    
    private IEnumerator MoveBookshelfSmoothly(float duration = 8f)
    {
        Vector3 startPosition = transform.position;
        // Calcular a posição final relativa à posição atual
        Vector3 endPosition = startPosition + new Vector3(-40.8f, 0f, 0f);
        float elapsedTime = 0;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // Usar curva de suavização para movimento mais natural
            float smoothProgress = Mathf.SmoothStep(0, 1, progress);
            
            // Usar Lerp para interpolação linear entre as posições
            transform.position = Vector3.Lerp(startPosition, endPosition, smoothProgress);
            
            yield return null;
        }
        
        // Garantir que chegue exatamente na posição final
        transform.position = endPosition;
        
        DoorController doorController = FindFirstObjectByType<DoorController>();
        if (doorController != null)
        {
            doorController.EnableTransition();
            Debug.Log("[BOOKSHELF PUZZLE] Porta aberta após movimento da estante");
        }

        Debug.Log($"Movimento concluído. Posição inicial X: {startPosition.x}, Posição final X: {endPosition.x}");
    }

    // Chamado quando o puzzle é resolvido
    private void PuzzleSolved()
    {
        puzzleSolved = true;
        
        // Ativar feedback de sucesso
        if (successFeedback != null)
            successFeedback.SetActive(true);
        
        Debug.Log("[BOOKSHELF PUZZLE] Puzzle resolvido! Completando interação.");
        
        // Iniciar movimento suave da estante
        StartCoroutine(MoveBookshelfSmoothly());

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