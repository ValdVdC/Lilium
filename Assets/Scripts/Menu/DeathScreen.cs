using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DeathScreen : MonoBehaviour
{
    // Referências para os elementos de UI
    [Header("UI Elements")]
    public GameObject deathScreenPanel;
    public Button retryButton;
    public Button mainMenuButton;
    
    // Referências para elementos visuais de seleção
    public Image retryButtonHighlight;
    public Image mainMenuButtonHighlight;
    
    // Configurações
    [Header("Settings")]
    public Color selectedColor = Color.yellow;
    public Color unselectedColor = Color.white;
    
    // Estado atual
    private bool isActive = false;
    private int currentSelection = 0; // 0 = Retry, 1 = Main Menu
    
    // Singleton para acesso fácil
    public static DeathScreen Instance { get; private set; }
    
    private MenuManager menuManager;
    private InventoryManager inventoryManager;

    private void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Encontrar o MenuManager
        menuManager = FindFirstObjectByType<MenuManager>();
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager não encontrado na cena!");
        }
        // Desativar o painel de morte inicialmente
        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(false);
    }
    
    private void Update()
    {
        // Só processar input se a tela estiver ativa
        if (!isActive) return;
        
        // Alternar entre opções com A e D
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentSelection = 0; // Retry
            UpdateSelection();
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentSelection = 1; // Main Menu
            UpdateSelection();
        }
        
        // Selecionar opção com Enter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (currentSelection == 0)
            {
                RetryGame();
            }
            else
            {
                ReturnToMainMenu();
            }
        }
    }
    
    // Mostrar a tela de morte
    public void ShowDeathScreen()
    {
        if (inventoryManager != null)
        {
            inventoryManager.inventoryPanel.SetActive(false);
        }
        // Pausar o jogo
        Time.timeScale = 0f;
        
        // Mostrar a tela de morte
        deathScreenPanel.SetActive(true);
        isActive = true;
        
        // Definir seleção inicial para Retry
        currentSelection = 0;
        UpdateSelection();
        
        // Desabilitar outros controles do jogador aqui se necessário
    }
    
    // Esconder a tela de morte
    public void HideDeathScreen()
    {
        // Retomar o jogo
        Time.timeScale = 1f;
        
        // Esconder a tela de morte
        deathScreenPanel.SetActive(false);
        if (inventoryManager != null)
        {
            inventoryManager.inventoryPanel.SetActive(false);
        }
        isActive = false;
    }
    
    // Atualizar a seleção visual
    private void UpdateSelection()
    {
        if (retryButtonHighlight != null)
            retryButtonHighlight.color = (currentSelection == 0) ? selectedColor : unselectedColor;
            
        if (mainMenuButtonHighlight != null)
            mainMenuButtonHighlight.color = (currentSelection == 1) ? selectedColor : unselectedColor;
    }
    
    // Tentar novamente (voltar ao último save)
    public void RetryGame()
    {
        Debug.Log("Tentando novamente - carregando último save");
        
        // Esconder a tela de morte
        HideDeathScreen();
        
        // Encontrar o save mais recente
        string mostRecentSave = GameSaveSystem.Instance.GetMostRecentSaveFile();
        
        if (!string.IsNullOrEmpty(mostRecentSave))
        {
            // Extrair o número do slot do nome do arquivo (save_X.json)
            string slotStr = mostRecentSave.Replace("save_", "").Replace(".json", "");
            
            if (int.TryParse(slotStr, out int slotIndex))
            {
                // Carregar o save mais recente
                GameSaveSystem.Instance.LoadGameFromSlot(slotIndex);
            }
            else
            {
                Debug.LogError("Não foi possível extrair o número do slot do arquivo: " + mostRecentSave);
            }
        }
        else
        {
            Debug.LogWarning("Nenhum save encontrado para carregar!");
            // Implementar lógica alternativa aqui caso não haja saves
            // Por exemplo, reiniciar o nível atual
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    
    // Voltar ao menu principal
    public void ReturnToMainMenu()
    {
        Debug.Log("Voltando ao menu principal");
        
        // Esconder a tela de morte
        HideDeathScreen();
        
        // Garantir que o tempo volta ao normal
        Time.timeScale = 1f;

        menuManager.mainMenuCanvas.SetActive(true);
    }
}