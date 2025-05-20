using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Principal")]
    [SerializeField] public GameObject mainMenuCanvas;

    [Header("Botões do Menu")]
    public Button continueButton;
    public Button newGameButton;
    public Button loadGameButton;
    public Button settingsButton;
    public Button exitButton;

    [Header("Painéis")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("Navegação Por Teclado")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    private Button[] menuButtons;
    private int currentButtonIndex = 0;

    [Header("Controle do Cursor")]
    [SerializeField] private bool showCursorInMenu = false;  // Define se o cursor deve ser mostrado no menu
    private CursorController cursorController;

    // Flag para novo jogo
    private static bool isNewGameStarted = false;
    public bool IsOpen => mainMenuPanel.activeSelf;

    // Referências ao sistema de save e ao menu de saves
    private GameSaveSystem saveSystem;
    private SaveMenuUI saveMenuUI;
    private PlayerController playerController;
    
    void Awake()
    {
        // Obter as referências necessárias
        saveSystem = GameSaveSystem.Instance;
        saveMenuUI = FindFirstObjectByType<SaveMenuUI>();
        cursorController = FindFirstObjectByType<CursorController>();
        
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(true);
        }
        else
        {
            Debug.LogError("Canvas do Menu Principal não encontrado!");
        }
        
        // Verificar se estamos inicializando após um "New Game"
        if (isNewGameStarted)
        {
            mainMenuCanvas.SetActive(false);
            isNewGameStarted = false; // Resetar flag
            
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMovementEnabled(true);
            }
            
            // Esconder cursor no jogo se necessário
            if (cursorController != null && !showCursorInMenu)
            {
                cursorController.HideCursor();
            }
        }
    }

    void Start()
    {
        // Configurar o cursor no menu
        SetupCursor();
        
        // Se o menu não estiver ativo (caso de New Game), não inicializar
        if (!mainMenuCanvas.activeSelf)
            return;
            
        // Verificar existência de saves e atualizar botões
        UpdateSaveButtonsVisibility();
        
        // Configurar os botões para navegação por teclado
        InitializeMenuButtons();
        
        // Configurando os ouvintes de clique para os botões
        if (newGameButton != null) newGameButton.onClick.AddListener(StartNewGame);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (loadGameButton != null) loadGameButton.onClick.AddListener(LoadGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        
        // Garantir que o painel principal está ativo e o de configurações está inativo no início
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Verificar se estamos no menu principal ou no jogo
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null && mainMenuCanvas.activeSelf)
        {
            playerController.SetMovementEnabled(false);
        }
        
        // Selecionar o primeiro botão disponível
        SelectFirstAvailableButton();
    }
    
    private void SetupCursor()
    {
        // Configurar o cursor com base nas preferências
        if (cursorController != null)
        {
            if (showCursorInMenu && mainMenuCanvas.activeSelf)
            {
                cursorController.ShowCursor();
                cursorController.SetInteractiveCursor(); // Usar cursor interativo no menu
            }
            else if (!showCursorInMenu)
            {
                cursorController.HideCursor();
            }
        }
        else
        {
            // Controle direto do cursor se não encontrar o CursorController
            Cursor.visible = showCursorInMenu && mainMenuCanvas.activeSelf;
        }
    }

    private void InitializeMenuButtons()
    {
        // Criar lista temporária para guardar botões
        System.Collections.Generic.List<Button> buttonsList = new System.Collections.Generic.List<Button>();
        
        // Adicionar apenas botões ativos
        if (continueButton != null && continueButton.gameObject.activeSelf) buttonsList.Add(continueButton);
        if (newGameButton != null) buttonsList.Add(newGameButton);
        if (loadGameButton != null && loadGameButton.gameObject.activeSelf) buttonsList.Add(loadGameButton);
        if (settingsButton != null) buttonsList.Add(settingsButton);
        if (exitButton != null) buttonsList.Add(exitButton);
        
        // Converter para array
        menuButtons = buttonsList.ToArray();
        
        // Selecionar o primeiro botão
        currentButtonIndex = 0;
        UpdateButtonsVisual();
    }

    private void SelectFirstAvailableButton()
    {
        // Se não há botões, retornar
        if (menuButtons == null || menuButtons.Length == 0)
            return;
            
        // Seleciona o primeiro botão disponível (continuar se houver saves, senão novo jogo)
        if (saveSystem != null && saveSystem.HasAnySave() && continueButton != null && continueButton.gameObject.activeSelf)
        {
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i] == continueButton)
                {
                    currentButtonIndex = i;
                    break;
                }
            }
        }
        else if (newGameButton != null)
        {
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i] == newGameButton)
                {
                    currentButtonIndex = i;
                    break;
                }
            }
        }
        
        UpdateButtonsVisual();
    }

    void Update()
    {
        // Só processar se o menu principal estiver visível
        if (!mainMenuCanvas.activeSelf) return;
        
        // Se o menu de saves estiver aberto, não processar entradas do menu principal
        if (saveMenuUI != null && saveMenuUI.IsOpen) return;
        
        // Se o painel de configurações estiver aberto e o jogador apertar ESC, feche-o
        if (settingsPanel != null && settingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettings();
            return;
        }
        
        // Navegação pelos botões do menu
        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
        {
            // Navegação para cima (W)
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentButtonIndex--;
                if (currentButtonIndex < 0)
                    currentButtonIndex = menuButtons.Length - 1;
                    
                UpdateButtonsVisual();
            }
            
            // Navegação para baixo (S)
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentButtonIndex++;
                if (currentButtonIndex >= menuButtons.Length)
                    currentButtonIndex = 0;
                    
                UpdateButtonsVisual();
            }
            
            // Seleção (Enter)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ActivateCurrentButton();
            }
        }
    }
    
    private void UpdateButtonsVisual()
    {
        // Verificar se os botões do menu foram inicializados
        if (menuButtons == null || menuButtons.Length == 0)
            return;
            
        // Atualiza a aparência de todos os botões
        for (int i = 0; i < menuButtons.Length; i++)
        {
            // Verifica se o botão atual tem TextMeshPro
            TextMeshProUGUI buttonText = menuButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.color = (i == currentButtonIndex) ? selectedColor : normalColor;
            }
            else
            {
                // Caso use o Text padrão do Unity
                Text legacyText = menuButtons[i].GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.color = (i == currentButtonIndex) ? selectedColor : normalColor;
                }
            }
        }
    }
    
    private void ActivateCurrentButton()
    {
        if (menuButtons != null && currentButtonIndex >= 0 && currentButtonIndex < menuButtons.Length)
        {
            menuButtons[currentButtonIndex].onClick.Invoke();
        }
    }

    // Atualiza a visibilidade dos botões baseado na existência de saves
    public void UpdateSaveButtonsVisibility()
    {
        if (saveSystem == null) 
        {
            Debug.LogWarning("SaveSystem é nulo! Tentando obter referência novamente.");
            saveSystem = GameSaveSystem.Instance;
            
            if (saveSystem == null)
            {
                Debug.LogError("Não foi possível obter referência ao GameSaveSystem!");
                return;
            }
        }
        
        bool hasSaves = saveSystem.HasAnySave();
        Debug.Log("Verificando saves. Existem saves? " + hasSaves);
        
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSaves);
            Debug.Log("Botão Continuar: " + (hasSaves ? "ativado" : "desativado"));
        }
            
        if (loadGameButton != null)
        {
            loadGameButton.gameObject.SetActive(hasSaves);
            Debug.Log("Botão Carregar: " + (hasSaves ? "ativado" : "desativado"));
        }
    }

    public void StartNewGame()
    {
        Debug.Log("Iniciando novo jogo...");

        // Definir flag para o novo jogo
        isNewGameStarted = true;
        
        // Recarregar a cena atual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ContinueGame()
    {
        Debug.Log("Continuando jogo mais recente...");
        
        if (saveSystem == null) return;
        
        // Encontrar o save mais recente
        string mostRecentSave = saveSystem.GetMostRecentSaveFile();
        
        if (!string.IsNullOrEmpty(mostRecentSave))
        {
            // Extrair o número do slot do nome do arquivo (save_X.json)
            string slotStr = mostRecentSave.Replace("save_", "").Replace(".json", "");
            
            if (int.TryParse(slotStr, out int slotIndex))
            {
                // Carregar o save mais recente usando o método LoadGameFromSlot
                saveSystem.LoadGameFromSlot(slotIndex);
                
                mainMenuCanvas.SetActive(false);
                
                if (playerController != null)
                    playerController.SetMovementEnabled(true);
                
                // Esconder o cursor ao entrar no jogo se necessário
                if (cursorController != null && !showCursorInMenu)
                {
                    cursorController.HideCursor();
                }
                
                Debug.Log("Jogo mais recente (slot " + slotIndex + ") carregado com sucesso!");
            }
            else
            {
                Debug.LogError("Não foi possível extrair o número do slot do arquivo: " + mostRecentSave);
            }
        }
        else
        {
            Debug.LogWarning("Nenhum save encontrado para carregar!");
        }
    }

    public void LoadGame()
    {
        Debug.Log("Abrindo menu de carregamento...");
        
        if (saveMenuUI == null)
        {
            Debug.LogError("SaveMenuUI não encontrado!");
            return;
        }
        
        // Abrir o menu de saves no modo somente carregamento
        saveMenuUI.OpenLoadOnlyMenu();
        
        // Importante: adicionei esta linha para garantir que o controlador do jogador seja desativado
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }
        
        // Mostrar o cursor para navegação no menu de saves
        if (cursorController != null && !showCursorInMenu)
        {
            cursorController.ShowCursor();
        }
    }

    public void OpenSettings()
    {
        Debug.Log("Abrindo configurações...");
        // Alterna entre os painéis
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        Debug.Log("Fechando configurações...");
        // Retorna ao menu principal
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        // Verificar saves novamente ao retornar para o menu principal
        UpdateSaveButtonsVisibility();
        
        // Reinicializar os botões após fechar as configurações
        InitializeMenuButtons();
        SelectFirstAvailableButton();
    }

    public void ExitGame()
    {
        Debug.Log("Saindo do jogo...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}