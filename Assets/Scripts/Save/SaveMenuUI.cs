using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

public class SaveMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject saveMenuPanel;
    [SerializeField] private GameObject[] slotButtons; // Array de 10 slots
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private GameObject buttonsPanel; // Painel com os botões Salvar, Carregar e Excluir
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI[] slotTexts; // Texto de cada slot
    [SerializeField] private Image[] slotBackgrounds; // Referências para os componentes de imagem de fundo
    [SerializeField] private float activeImageAlpha = 0.2f; // Transparência da imagem quando slot tem save

    [Header("Navigation Settings")]
    [SerializeField] private float navigationCooldown = 0.2f;
    [SerializeField] private Color selectedSlotColor = Color.yellow;
    [SerializeField] private Color emptySlotColor = Color.gray;
    [SerializeField] private Color filledSlotColor = Color.white;
    
    [Header("Save Point Images")]
    [SerializeField] private Sprite salaArmasSprite; // Imagem da sala de armas
    [SerializeField] private Sprite salaDocumentosSprite; // Imagem da sala de documentos
    [SerializeField] private Sprite bibliotecaSprite; // Imagem da biblioteca
    [SerializeField] private Dictionary<string, Sprite> savePointImages = new Dictionary<string, Sprite>();

    // Estado de navegação
    private int currentSlotIndex = 0;
    public bool isConfirmationOpen = false;
    public bool isSlotOptionsOpen = false;
    private float lastNavigationTime;
    private SaveAction currentAction = SaveAction.None;
    
    // Referência para o GameSaveSystem
    private GameSaveSystem saveSystem;

    // Referência para o InventoryUI
    private InventoryManager inventoryManager;
    
    // Caminho para verificar os saves
    private string savePath;
    private List<SaveSlotInfo> saveSlots = new List<SaveSlotInfo>();
    
    private string currentSavePointName = "";

    private bool loadOnlyMode = false;
    public bool IsOpen => saveMenuPanel.activeSelf;

    private MenuManager menuManager;
    private float menuOpenTime = 0f;
    private const float INPUT_IGNORE_DURATION = 0.3f; // Tempo em segundos para ignorar input após abrir menu

    // Estados de ação para o sistema de salvamento
    private enum SaveAction
    {
        None,
        Save,
        Load,
        Delete
    }
    
    // Informações de cada slot de salvamento
    [System.Serializable]
    private class SaveSlotInfo
    {
        public bool hasData;
        public string saveDate;
        public string playerInfo;
        public string savePointName;
        
        public SaveSlotInfo(bool hasData, string saveDate, string playerInfo, string savePointName)
        {
            this.hasData = hasData;
            this.saveDate = saveDate;
            this.playerInfo = playerInfo;
            this.savePointName = savePointName;
        }
    }
    
    private void Start()
    {
        // Obter referência para o GameSaveSystem
        saveSystem = GameSaveSystem.Instance;
        
        inventoryManager = FindFirstObjectByType<InventoryManager>();

        menuManager = FindFirstObjectByType<MenuManager>();

        if (saveSystem == null)
        {
            Debug.LogError("SaveMenuUI não conseguiu encontrar GameSaveSystem!");
        }
        
        // Inicializar o caminho dos saves
        savePath = Path.Combine(Application.persistentDataPath, "Saves");
        
        // Inicializar lista de slots
        for (int i = 0; i < 10; i++)
        {
            saveSlots.Add(new SaveSlotInfo(false, "", "", ""));
        }
        
        // Adicionar eventos aos botões (para funcionalidade via UI, caso necessário)
        saveButton.onClick.AddListener(() => TriggerAction(SaveAction.Save));
        loadButton.onClick.AddListener(() => TriggerAction(SaveAction.Load));
        deleteButton.onClick.AddListener(() => TriggerAction(SaveAction.Delete));
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        
        // Inicialmente, esconder painéis
        saveMenuPanel.SetActive(false);
        confirmationPanel.SetActive(false);
        buttonsPanel.SetActive(false);

        if (savePointImages == null)
            savePointImages = new Dictionary<string, Sprite>();
            
        savePointImages.Clear();
        savePointImages.Add("Sala de Armas", salaArmasSprite);
        savePointImages.Add("Sala de Documentos", salaDocumentosSprite);
        savePointImages.Add("Biblioteca", bibliotecaSprite);

        // Verificar se as imagens foram carregadas
        foreach (var pair in savePointImages)
        {
            Debug.Log($"SavePoint Image: {pair.Key} - Sprite carregado: {pair.Value != null}");
        }
    }
    
    private void Update()
    {
        if (!saveMenuPanel.activeSelf) return;
        
        // Verificar input apenas se passou o tempo de cooldown
        if (Time.time - lastNavigationTime < navigationCooldown) return;
        
        // Navegação com WASD
        bool navigated = HandleNavigation();
        
        // Ações com teclas
        if (!navigated)
        {
            HandleKeyActions();
        }
    }
    
    // Lidar com entrada de navegação
    private bool HandleNavigation()
    {
        bool moved = false;
        
        // Não navegar se estiver no painel de confirmação
        if (isConfirmationOpen) return false;
        if (isSlotOptionsOpen) return false;
        
        int oldIndex = currentSlotIndex;
        
        // Navegação horizontal
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // Navegação horizontal em uma matriz 2x5 (ou qualquer formato adequado à sua UI)
            if (currentSlotIndex % 5 == 0)
                currentSlotIndex += 4; // Vai para o último elemento da linha
            else
                currentSlotIndex--;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentSlotIndex % 5 == 4 || currentSlotIndex == slotButtons.Length - 1)
                currentSlotIndex = currentSlotIndex - (currentSlotIndex % 5); // Vai para o primeiro elemento da linha
            else
                currentSlotIndex++;
            moved = true;
        }
        
        // Navegação vertical (assumindo layout 2x5)
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSlotIndex = (currentSlotIndex < 5) ? currentSlotIndex + 5 : currentSlotIndex - 5;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSlotIndex = (currentSlotIndex >= 5) ? currentSlotIndex - 5 : currentSlotIndex + 5;
            moved = true;
        }
        
        // Se houver navegação, atualizar a UI
        if (moved && oldIndex != currentSlotIndex)
        {
            lastNavigationTime = Time.time;
            UpdateSlotSelection();
            return true;
        }
        
        return false;
    }
    
    // Lidar com teclas de ação
    private void HandleKeyActions()
    {
        // Ignorar inputs por um curto período após abrir o menu
        if (Time.unscaledTime - menuOpenTime < INPUT_IGNORE_DURATION)
        {
            return;
        }

        // O resto do código permanece igual
        // Se o painel de confirmação estiver aberto
        if (isConfirmationOpen)
        {
            // Enter para confirmar no painel de confirmação
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnConfirmButtonClicked();
                lastNavigationTime = Time.time;
            }
            // E para cancelar no painel de confirmação
            else if (Input.GetKeyDown(KeyCode.E))
            {
                CloseConfirmationPanel();
                lastNavigationTime = Time.time;
            }
            return;
        }
        
        // Se o painel de opções do slot estiver aberto
        if (isSlotOptionsOpen)
        {
            // Teclas para ações específicas
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                // Enter para salvar
                TriggerAction(SaveAction.Save);
                lastNavigationTime = Time.time;
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                // C para carregar (apenas se tiver dados)
                if (saveSlots[currentSlotIndex].hasData)
                {
                    TriggerAction(SaveAction.Load);
                    lastNavigationTime = Time.time;
                }
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                // R para excluir (apenas se tiver dados)
                if (saveSlots[currentSlotIndex].hasData)
                {
                    TriggerAction(SaveAction.Delete);
                    lastNavigationTime = Time.time;
                }
            }
            // E para fechar opções do slot
            else if (Input.GetKeyDown(KeyCode.E))
            {
                CloseSlotOptions();
                lastNavigationTime = Time.time;
            }
        }
        // Navegação principal (quando nenhum painel está aberto)
        else
        {
            // Enter para selecionar slot e abrir opções
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OpenSlotOptions();
                lastNavigationTime = Time.time;
            }
            // E para sair do menu
            else if (Input.GetKeyDown(KeyCode.E))
            {
                CloseSaveMenu();
                lastNavigationTime = Time.time;
            }
        }
    }
    
    // Desencadear uma ação específica
    private void TriggerAction(SaveAction action)
    {
        currentAction = action;
        
        switch (action)
        {
            case SaveAction.Save:
                // Mostrar confirmação para salvar
                string confirmMsg = saveSlots[currentSlotIndex].hasData ? 
                    $"Este salvamento vai subscrever o save. Deseja mesmo salvar no slot {currentSlotIndex + 1}?" :
                    $"Deseja salvar no slot {currentSlotIndex + 1}?";
                ShowConfirmationPanel(confirmMsg);
                break;
                
            case SaveAction.Load:
                // Mostrar confirmação para carregar
                ShowConfirmationPanel($"Deseja carregar o jogo do slot {currentSlotIndex + 1}?");
                break;
                
            case SaveAction.Delete:
                // Mostrar confirmação para excluir
                ShowConfirmationPanel($"Deseja excluir o salvamento do slot {currentSlotIndex + 1}?");
                break;
        }
    }
    
    // Abrir o menu de saves
    public void OpenSaveMenu(string savePointName)
    {
        if (inventoryManager != null)
        {
            inventoryManager.inventoryPanel.SetActive(false);
        }

        // Store the current save point name
        currentSavePointName = savePointName;
        
        RefreshSaveSlots(savePointName);
        saveMenuPanel.SetActive(true);
        currentSlotIndex = 0;
        UpdateSlotSelection();
        
        // Fechar outros painéis
        confirmationPanel.SetActive(false);
        buttonsPanel.SetActive(false);
        isConfirmationOpen = false;
        isSlotOptionsOpen = false;
        
        // Pausar o jogo se necessário
        Time.timeScale = 0f;
    }
    
    // Fechar o menu de saves
    public void CloseSaveMenu()
    {
        if (inventoryManager != null)
        {
            inventoryManager.inventoryPanel.SetActive(true);
        }

        saveMenuPanel.SetActive(false);
        Time.timeScale = 1f; // Restaurar o tempo do jogo
        loadOnlyMode = false; // Resetar o modo
        
        // Notificar todos os SavePointControllers na cena que o menu foi fechado
        SavePointController[] savePoints = FindObjectsByType<SavePointController>(FindObjectsSortMode.None);
        foreach (var savePoint in savePoints)
        {
            savePoint.OnSaveMenuClosed();
        }
        
        Debug.Log("Menu de salvamento fechado - notificação enviada");
    }
    
    // Atualizar a seleção do slot na UI
    private void UpdateSlotSelection()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            // Atualizar cores dos slots
            Image buttonImage = slotButtons[i].GetComponent<Image>();
            if (i == currentSlotIndex)
            {
                buttonImage.color = selectedSlotColor;
            }
            else
            {
                buttonImage.color = saveSlots[i].hasData ? filledSlotColor : emptySlotColor;
            }
        }
        
        // Fechar opções de slot se estiverem abertas
        if (isSlotOptionsOpen)
        {
            CloseSlotOptions();
        }
    }
    
    // Abrir opções para o slot selecionado
    private void OpenSlotOptions()
    {
        // Verificar se o slot tem dados
        bool hasData = saveSlots[currentSlotIndex].hasData;
        
        // Em modo somente carregamento, só mostra opções se o slot tiver dados
        if (loadOnlyMode && !hasData)
            return;
        
        // Posicionar o painel de botões sobre o slot selecionado
        RectTransform slotTransform = slotButtons[currentSlotIndex].GetComponent<RectTransform>();
        RectTransform panelTransform = buttonsPanel.GetComponent<RectTransform>();
        
        // Ajuste para posicionar o painel no centro do slot
        panelTransform.position = slotTransform.position;
        
        // Ativar/desativar botões com base no estado do slot e modo
        loadButton.gameObject.SetActive(hasData);
        deleteButton.gameObject.SetActive(hasData);
        saveButton.gameObject.SetActive(!loadOnlyMode);  // Oculta o botão "Salvar" no modo somente carregamento
        
        // Mostrar o painel
        buttonsPanel.SetActive(true);
        isSlotOptionsOpen = true;
    }
    
    // Fechar o painel de opções
    private void CloseSlotOptions()
    {
        buttonsPanel.SetActive(false);
        isSlotOptionsOpen = false;
    }
    
    // Mostrar painel de confirmação
    private void ShowConfirmationPanel(string message)
    {
        confirmationText.text = message;
        confirmationPanel.SetActive(true);
        isConfirmationOpen = true;
    }
    
    // Fechar painel de confirmação
    private void CloseConfirmationPanel()
    {
        confirmationPanel.SetActive(false);
        isConfirmationOpen = false;
        currentAction = SaveAction.None;
    }
    
    // Quando o botão de confirmação é clicado
    private void OnConfirmButtonClicked()
    {
        // Executar a ação com base no estado atual
        switch (currentAction)
        {
            case SaveAction.Save:
                // Salvar jogo no slot atual
                SaveGame();
                break;
                
            case SaveAction.Load:
                // Carregar jogo do slot atual
                LoadGame();
                break;
                
            case SaveAction.Delete:
                // Excluir save do slot atual
                DeleteSave();
                break;
        }
        
        // Fechar painéis
        CloseConfirmationPanel();
        buttonsPanel.SetActive(false);
        menuManager.mainMenuCanvas.SetActive(false);
        
        // Atualizar informações dos slots após a ação
        RefreshSaveSlots();
    }
    
    // Salvar o jogo no slot atual
    private void SaveGame()
    {
        if (saveSystem != null)
        {
            string slotFileName = $"save_{currentSlotIndex}.json";
            saveSystem.saveFileName = slotFileName;
            
            // Use the saved point name instead of the slot number
            saveSystem.SaveGame(currentSavePointName);
            Debug.Log($"Jogo salvo no slot {currentSlotIndex + 1} em '{currentSavePointName}'");
        }
    }

    private void OnValidate()
    {
        // Verificar se todas as referências para sprites estão configuradas
        if (salaArmasSprite == null)
            Debug.LogWarning("salaArmasSprite não está atribuído no Inspector!");
        if (salaDocumentosSprite == null)
            Debug.LogWarning("salaDocumentosSprite não está atribuído no Inspector!");
        if (bibliotecaSprite == null)
            Debug.LogWarning("bibliotecaSprite não está atribuído no Inspector!");
            
        // Verificar referências de backgrounds
        if (slotBackgrounds == null || slotBackgrounds.Length == 0)
            Debug.LogWarning("slotBackgrounds não estão configurados no Inspector!");
    }
    
    // Carregar o jogo do slot atual
    private void LoadGame()
    {
        if (saveSystem != null)
        {
            string slotFileName = $"save_{currentSlotIndex}.json";
            saveSystem.saveFileName = slotFileName;
            bool success = saveSystem.LoadGame();
            
            if (success)
            {
                Debug.Log($"Jogo carregado do slot {currentSlotIndex + 1}");
                CloseSaveMenu(); // Fechar o menu após carregar
            }
            else
            {
                Debug.LogError($"Falha ao carregar o jogo do slot {currentSlotIndex + 1}");
            }
        }
    }
    
    // Excluir o save do slot atual
    private void DeleteSave()
    {
        if (saveSystem != null)
        {
            string slotFileName = $"save_{currentSlotIndex}.json";
            string fullPath = Path.Combine(savePath, slotFileName);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Debug.Log($"Save do slot {currentSlotIndex + 1} excluído com sucesso");
                
                // Atualizar o slot na lista
                saveSlots[currentSlotIndex].hasData = false;
                saveSlots[currentSlotIndex].saveDate = "";
                saveSlots[currentSlotIndex].playerInfo = "";
                saveSlots[currentSlotIndex].savePointName = "";
                
                // Atualizar texto do slot
                UpdateSlotText(currentSlotIndex);
            }
        }
    }
    
    // Atualizar informações de todos os slots
    private void RefreshSaveSlots(string currentSavePointName = "")
    {
        Debug.Log($"Refreshing save slots with current save point: '{currentSavePointName}'");
        
        // Verificar cada slot
        for (int i = 0; i < 10; i++)
        {
            string slotFileName = $"save_{i}.json";
            string fullPath = Path.Combine(savePath, slotFileName);
            
            if (File.Exists(fullPath))
            {
                try
                {
                    // Tentar ler os dados básicos do save
                    string json = File.ReadAllText(fullPath);
                    GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                    
                    // Atualizar informações do slot
                    saveSlots[i].hasData = true;
                    saveSlots[i].saveDate = saveData.saveDateTime;
                    
                    // Verificar se o nome do savePoint está vazio e usar um valor padrão
                    if (string.IsNullOrEmpty(saveData.savePointName))
                    {
                        saveSlots[i].savePointName = "Save desconhecido";
                        Debug.LogWarning($"Slot {i}: Nome do savePoint está vazio, usando valor padrão");
                    }
                    else
                    {
                        saveSlots[i].savePointName = saveData.savePointName;
                        Debug.Log($"Slot {i}: Carregado nome do savePoint = '{saveData.savePointName}'");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Erro ao ler dados do slot {i+1}: {e.Message}");
                    saveSlots[i].hasData = true;
                    saveSlots[i].saveDate = "Dados corrompidos";
                    saveSlots[i].savePointName = "Erro";
                    saveSlots[i].playerInfo = "Desconhecido";
                }
            }
            else
            {
                // Slot vazio
                saveSlots[i].hasData = false;
                saveSlots[i].saveDate = "";
                saveSlots[i].playerInfo = "";
                saveSlots[i].savePointName = currentSavePointName;
            }
            
            // Atualizar texto do slot
            UpdateSlotText(i);
        }
    }
    
    // Atualizar o texto exibido em um slot
    private void UpdateSlotText(int slotIndex)
    {
        if (slotTexts != null && slotIndex < slotTexts.Length)
        {
            TextMeshProUGUI text = slotTexts[slotIndex];
            Image background = slotBackgrounds[slotIndex];
            
            if (saveSlots[slotIndex].hasData)
            {
                // Texto para slot com dados
                text.text = $"{saveSlots[slotIndex].savePointName}\n" +
                        $"Data: {saveSlots[slotIndex].saveDate}\n" +
                        $"{saveSlots[slotIndex].playerInfo}";
                text.color = Color.white; // Cor branca para texto em slot com save
                
                // Log para diagnóstico
                Debug.Log($"Slot {slotIndex}: Nome do savePoint = '{saveSlots[slotIndex].savePointName}'");
                
                // Definir a imagem de fundo baseada no local de salvamento
                string savePointName = saveSlots[slotIndex].savePointName;
                if (savePointImages.ContainsKey(savePointName))
                {
                    background.sprite = savePointImages[savePointName];
                    background.color = new Color(1, 1, 1, activeImageAlpha); // 20% de opacidade
                    background.enabled = true;
                    Debug.Log($"Slot {slotIndex}: Imagem encontrada para '{savePointName}' e aplicada");
                }
                else
                {
                    background.enabled = false;
                    Debug.LogWarning($"Slot {slotIndex}: Nenhuma imagem encontrada para '{savePointName}'");
                }
            }
            else
            {
                // Texto para slot vazio
                text.text = $"Slot {slotIndex + 1}\n<Vazio>";
                text.color = Color.black; // Cor preta para texto em slot vazio
                
                // Desativar a imagem de fundo
                background.enabled = false;
                Debug.Log($"Slot {slotIndex}: Está vazio, desativando imagem");
            }
        }
    }

    public void OpenLoadOnlyMenu()
    {
        loadOnlyMode = true;
        
        // Registrar o tempo de abertura do menu
        menuOpenTime = Time.unscaledTime;
        
        // Atualizar slots sem um ponto de salvamento específico
        RefreshSaveSlots();
        saveMenuPanel.SetActive(true);
        currentSlotIndex = 0;
        UpdateSlotSelection();
        
        // Fechar outros painéis
        confirmationPanel.SetActive(false);
        buttonsPanel.SetActive(false);
        isConfirmationOpen = false;
        isSlotOptionsOpen = false;
        
        // Pausar o jogo se necessário
        Time.timeScale = 0f;
    }
}