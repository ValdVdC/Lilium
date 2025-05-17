using UnityEngine;

// Slot para colocar livros, herda de BookItem para compartilhar funcionalidades de interação
public class BookSlot : BookItem
{
    [Header("Slot Settings")]
    public SpriteRenderer slotSprite;           // Sprite do slot vazio (naipe)
    public SpriteRenderer outlineSprite;        // Sprite de contorno para destacar o slot
    public string slotName;                     // Nome do slot (ex: "Espadas", "Paus", etc)
    
    public BookItem _currentBook;              // Livro atualmente colocado neste slot
    
    public BookItem CurrentBook 
    { 
        get { return _currentBook; }
    }
    
    protected override void Awake()
    {
        base.Awake();
        
        // Verificar sprites
        if (slotSprite == null)
            slotSprite = GetComponent<SpriteRenderer>();
            
        // Inicialmente sem livro
        _currentBook = null;
        
        // Configurar outline (se existir)
        if (outlineSprite != null)
            outlineSprite.enabled = false;
    }
    
    // Sobrescreve o método de destaque para o slot
    public override void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;
        
        // Destacar o outline do slot ao invés do sprite principal
        if (outlineSprite != null)
            outlineSprite.enabled = highlighted;
            
        // Se tiver um livro, destacar o livro também
        if (_currentBook != null)
            _currentBook.SetHighlighted(highlighted);
    }
    
    // Define um livro para este slot
    public void SetBook(BookItem book)
    {
        // Se já tiver um livro, retirar
        if (_currentBook != null)
        {
            _currentBook.currentSlot = null;
            _currentBook.ResetPosition();
        }

        _currentBook = book;
        Debug.Log($"[BOOK SLOT] {name}: Definindo livro {(book != null ? book.name : "null")}");

        if (_currentBook != null)
        {
            // Ajustar posição do livro para o centro do slot
            _currentBook.transform.position = transform.position;
            
            // Atualizar a referência bidirecional
            _currentBook.currentSlot = this;
            
            Debug.Log($"[BOOK SLOT] {name}: Livro {book.name} colocado e posicionado em {transform.position}");
        }
    }
    
    // Remove o livro deste slot - MODIFICADO
    public BookItem RemoveBook()
    {
        BookItem removedBook = _currentBook;
        
        if (removedBook != null)
        {
            removedBook.currentSlot = null;
            _currentBook = null;
            Debug.Log($"[BOOK SLOT] {slotName}: Livro {removedBook.name} removido e referências limpas");
        }
        
        return removedBook;
    }
}