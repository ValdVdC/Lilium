using UnityEngine;

public class CursorController : MonoBehaviour
{
    [Header("Cursores")]
    [SerializeField] private Texture2D defaultCursor;  // Cursor personalizado padrão
    [SerializeField] private Texture2D interactiveCursor;  // Cursor para áreas interativas
    [SerializeField] private Vector2 hotspot = Vector2.zero;  // Ponto de clique do cursor

    [Header("Configurações")]
    [SerializeField] private bool cursorVisibleByDefault = true;  // Define se o cursor estará visível por padrão
    
    // Nova variável para controlar a inicialização
    private bool isInitialized = false;

    private void Start()
    {
        // Verifica se as texturas são válidas antes de tentar usá-las
        if (defaultCursor != null && IsValidCursorTexture(defaultCursor))
        {
            // Configuração inicial do cursor
            SetDefaultCursor();
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("Textura de cursor inválida. Usando cursor padrão do sistema.");
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            isInitialized = true;
        }
        
        // Define a visibilidade inicial do cursor
        Cursor.visible = cursorVisibleByDefault;
    }
    
    // Método para verificar se a textura do cursor é válida
    private bool IsValidCursorTexture(Texture2D texture)
    {
        return texture.isReadable && texture.format == TextureFormat.RGBA32;
    }

    // Define o cursor padrão
    public void SetDefaultCursor()
    {
        if (defaultCursor != null && isInitialized)
        {
            Cursor.SetCursor(defaultCursor, hotspot, CursorMode.Auto);
        }
    }

    // Define o cursor interativo (para menus, botões, etc.)
    public void SetInteractiveCursor()
    {
        if (interactiveCursor != null && isInitialized)
        {
            Cursor.SetCursor(interactiveCursor, hotspot, CursorMode.Auto);
        }
    }

    // Mostra o cursor
    public void ShowCursor()
    {
        Cursor.visible = true;
    }

    // Esconde o cursor
    public void HideCursor()
    {
        Cursor.visible = false;
    }

    // Para alternar a visibilidade do cursor
    public void ToggleCursorVisibility()
    {
        Cursor.visible = !Cursor.visible;
    }
    
    // Método público para forçar a inicialização
    public void Initialize(bool showCursor)
    {
        cursorVisibleByDefault = showCursor;
        Start();
    }
}