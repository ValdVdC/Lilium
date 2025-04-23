using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class GameSaveData
{
    // Dados do jogador
    public Vector3 playerPosition;
    public string playerDirection;  // Convertemos o enum para string para facilitar a serialização
    public float playerHealth = 100f;
    
    // Inventário (simplificado)
    public List<string> inventoryItems = new List<string>();
    
    // Estado do mundo
    public List<string> openedDoors = new List<string>();
    public List<string> collectedItems = new List<string>();
    public List<string> completedPuzzles = new List<string>();
    
    // Metadados do save
    public string savePointName;
    public string saveDateTime;
    
    public GameSaveData() 
    {
        // Construtor padrão para inicialização de valores
        saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

public class GameSaveSystem : MonoBehaviour
{
    // Singleton para acesso global
    public static GameSaveSystem Instance { get; private set; }
    
    // Caminho para salvar
    private string savePath;
    private string saveFileName = "savegame.json";
    
    // Dados do save atual
    public GameSaveData currentSaveData = new GameSaveData();
    
    // Evento para notificar quando o jogo é carregado
    public delegate void OnGameLoadedDelegate();
    public event OnGameLoadedDelegate OnGameLoaded;
    
    // Evento para notificar quando o jogo é salvo
    public delegate void OnGameSavedDelegate();
    public event OnGameSavedDelegate OnGameSaved;
    
    private void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Inicializar caminho do save
            savePath = Path.Combine(Application.persistentDataPath, "Saves");
            Directory.CreateDirectory(savePath);  // Garantir que o diretório exista
            
            Debug.Log($"GameSaveSystem inicializado. Caminho de salvamento: {savePath}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Salvar o jogo
    public void SaveGame(string savePointName = "")
    {
        try
        {
            // Encontrar o jogador
            PlayerController player = FindFirstObjectByType<PlayerController>();
            
            if (player != null)
            {
                // Salvar dados do jogador
                currentSaveData.playerPosition = player.transform.position;
                currentSaveData.playerDirection = player.currentDirection.ToString();
                
                // Qualquer dado adicional que você queira salvar do jogador
                // currentSaveData.playerHealth = player.health;
            }
            
            // Salvar dados das portas
            DoorController[] doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
            
            foreach (var door in doors)
            {
                // Este é um exemplo - você precisará adicionar uma propriedade isOpen pública ao DoorController
                // para acessar o estado atual da porta
                // if (door.isOpen)
                // {
                //     currentSaveData.openedDoors.Add(door.gameObject.name);
                // }
            }
            
            // Metadata do save
            currentSaveData.savePointName = savePointName;
            currentSaveData.saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Serializar para JSON
            string json = JsonUtility.ToJson(currentSaveData, true);
            
            // Salvar no arquivo
            string fullPath = Path.Combine(savePath, saveFileName);
            File.WriteAllText(fullPath, json);
            
            Debug.Log($"Jogo salvo com sucesso em: {fullPath}");
            
            // Notificar que o jogo foi salvo
            OnGameSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao salvar o jogo: {e.Message}");
        }
    }
    
    // Carregar o jogo
    public bool LoadGame()
    {
        try
        {
            string fullPath = Path.Combine(savePath, saveFileName);
            
            // Verificar se o arquivo existe
            if (!File.Exists(fullPath))
            {
                Debug.Log("Nenhum arquivo de save encontrado.");
                return false;
            }
            
            // Ler o arquivo
            string json = File.ReadAllText(fullPath);
            
            // Deserializar
            currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
            
            // Aplicar os dados salvos
            ApplySaveData();
            
            Debug.Log($"Jogo carregado com sucesso de: {fullPath}");
            
            // Notificar que o jogo foi carregado
            OnGameLoaded?.Invoke();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao carregar o jogo: {e.Message}");
            return false;
        }
    }
    
    // Aplicar os dados salvos ao estado atual do jogo
    private void ApplySaveData()
    {
        // Encontrar o jogador
        PlayerController player = FindFirstObjectByType<PlayerController>();
        
        if (player != null)
        {
            // Reposicionar o jogador
            player.transform.position = currentSaveData.playerPosition;
            
            // Definir a direção do jogador (você precisará adicionar um método para isso no PlayerController)
            if (Enum.TryParse(currentSaveData.playerDirection, out PlayerController.FacingDirection direction))
            {
                player.currentDirection = direction;
            }
            
            // Qualquer dado adicional que você queira restaurar para o jogador
            // player.health = currentSaveData.playerHealth;
        }
        
        // Restaurar estado das portas
        DoorController[] doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
        
        foreach (var door in doors)
        {
            if (currentSaveData.openedDoors.Contains(door.gameObject.name))
            {
                // Você precisará adicionar um método público OpenDoor() ao DoorController
                // door.OpenDoor();
            }
            else
            {
                // Você precisará adicionar um método público CloseDoor() ao DoorController
                // door.CloseDoor();
            }
        }
        
        // Outros estados do jogo que você queira restaurar...
    }
    
    // Verificar se existe um save
    public bool SaveExists()
    {
        string fullPath = Path.Combine(savePath, saveFileName);
        return File.Exists(fullPath);
    }
    
    // Excluir o save
    public void DeleteSave()
    {
        try
        {
            string fullPath = Path.Combine(savePath, saveFileName);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Debug.Log("Save excluído com sucesso.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao excluir o save: {e.Message}");
        }
    }
    
    // Método auxiliar para criar um save rápido (para testes)
    public void QuickSave()
    {
        SaveGame("QuickSave");
    }
    
    // Método auxiliar para carregar o último save (para testes)
    public void QuickLoad()
    {
        LoadGame();
    }
}