using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    
    [SerializeField] private GameObject pauseMenuCanvas;
    
    private bool isPaused = false;
    private PlayerHealth playerHealth;
    private SaveMenuUI saveMenuUI;
    private InventoryManager inventoryManager;
    private MenuManager menuManager;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Encontrar o componente PlayerHealth
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        inventoryManager = FindFirstObjectByType<InventoryManager>();

        menuManager = FindFirstObjectByType<MenuManager>();

        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager não encontrado na cena!");
        }
        
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth não encontrado na cena!");
        }

        // Encontrar o componente SaveMenuUI
        saveMenuUI = FindFirstObjectByType<SaveMenuUI>();
        if (saveMenuUI){
            Debug.LogWarning("SaveMenuUI não encontrado na cena!");
        }
        
        // Certifique-se de que o menu de pausa começa desativado
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError("Pause Menu Canvas não atribuído no Inspector!");
        }
    }
    
    void Update()
    {
        // Verificar se Esc foi pressionado e o jogador está vivo
        if (Input.GetKeyDown(KeyCode.Escape) && !IsPlayerDead() && !IsSaveMenuActive() && !IsMainMenuActive())
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }
    
    public void Pause()
    {
        Time.timeScale = 0f; // Pausa o tempo do jogo
        if (inventoryManager != null)
        {
            inventoryManager.inventoryPanel.SetActive(false);
        }
        pauseMenuCanvas.SetActive(true);
        isPaused = true;
    }
    
    public void Resume()
    {
        Time.timeScale = 1f; // Normaliza o tempo do jogo
        if (inventoryManager != null)
        {
            inventoryManager.inventoryPanel.SetActive(true);
        }
        pauseMenuCanvas.SetActive(false);
        isPaused = false;
    }
    
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Garante que o tempo volta ao normal
        menuManager.mainMenuCanvas.SetActive(true);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private bool IsPlayerDead()
    {
        if (playerHealth == null)
            return false;
            
        return playerHealth.currentHealth <= 0;
    }

    private bool IsSaveMenuActive()
    {
        if (saveMenuUI == null)
            return false;
            
        return saveMenuUI.IsOpen;
    }
    
    private bool IsMainMenuActive(){
        if(menuManager == null)
            return false;
        
        return saveMenuUI.IsOpen;
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}