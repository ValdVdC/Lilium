using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

// Interface que todos os objetos salváveis devem implementar
public interface ISaveable
{
    // Método para obter dados para salvar
    object GetSaveData();
    
    // Método para carregar dados salvos
    void LoadFromSaveData(object data);
}

// Classe para serializar um dicionário em formato compatível com Unity
[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    public List<TKey> keys = new List<TKey>();
    public List<TValue> values = new List<TValue>();
    
    public SerializableDictionary() { }
    
    public SerializableDictionary(Dictionary<TKey, TValue> dictionary)
    {
        FromDictionary(dictionary);
    }
    
    public void FromDictionary(Dictionary<TKey, TValue> dictionary)
    {
        keys.Clear();
        values.Clear();
        
        foreach (var kvp in dictionary)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }
    
    public Dictionary<TKey, TValue> ToDictionary()
    {
        Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
        
        for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
        {
            dict[keys[i]] = values[i];
        }
        
        return dict;
    }
}

[Serializable]
public class GameSaveData
{
    // Dados do jogador
    public Vector3 playerPosition;
    public string playerDirection;
    public float playerHealth = 100f;
    
    // Inventário (simplificado)
    public List<string> inventoryItems = new List<string>();
    
    // Estado do mundo
    public List<string> openedDoors = new List<string>();
    public List<string> collectedItems = new List<string>();
    public List<string> completedPuzzles = new List<string>();
    
    // Dicionário serializável para armazenar dados de componentes salvos
    public SerializableDictionary<string, string> objectsData = new SerializableDictionary<string, string>();
    
    // Metadados do save
    public string savePointName;
    public string saveDateTime;
    public string gameVersion;  // Para compatibilidade entre versões
    
    public GameSaveData() 
    {
        // Construtor padrão para inicialização de valores
        saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // gameVersion será definido após a criação do objeto, não no construtor
    }
    
    // Método para inicializar valores que dependem de Application
    public void Initialize()
    {
        gameVersion = Application.version;
    }
    
    // Método auxiliar para limpar dados de objetos
    public void ClearObjectsData()
    {
        objectsData.keys.Clear();
        objectsData.values.Clear();
    }
    
    // Método para adicionar dados de um objeto serializados como JSON
    public void AddObjectData(string objectId, string jsonData)
    {
        int existingIndex = objectsData.keys.IndexOf(objectId);
        
        if (existingIndex >= 0)
        {
            // Atualizar dados existentes
            objectsData.values[existingIndex] = jsonData;
        }
        else
        {
            // Adicionar novos dados
            objectsData.keys.Add(objectId);
            objectsData.values.Add(jsonData);
        }
    }
    
    // Método para obter dados de um objeto pelo ID
    public string GetObjectData(string objectId)
    {
        int index = objectsData.keys.IndexOf(objectId);
        
        if (index >= 0 && index < objectsData.values.Count)
        {
            return objectsData.values[index];
        }
        
        return null;
    }
}

public class GameSaveSystem : MonoBehaviour
{
    // Singleton para acesso global
    public static GameSaveSystem Instance { get; private set; }
    
    // Caminho para salvar
    private string savePath;
    public string saveFileName = "save_0.json";
    
    // Dados do save atual
    public GameSaveData currentSaveData = new GameSaveData();
    
    // Configurações de save
    [Header("Configurações")]
    public bool saveToDisk = true;  // Para testes, pode desativar a gravação
    public bool useCompression = true;  // Compressão dos dados
    public bool encryptData = false;  // Criptografia básica
    public string encryptionKey = "MySuperSecretKey";  // Chave para criptografia
    
    // Evento para notificar quando o jogo é carregado
    public delegate void OnGameLoadedDelegate();
    public event OnGameLoadedDelegate OnGameLoaded;
    
    // Evento para notificar quando o jogo é salvo
    public delegate void OnGameSavedDelegate();
    public event OnGameSavedDelegate OnGameSaved;
    
    // Evento antes de salvar o jogo (para componentes atualizarem seus dados)
    public delegate void OnBeforeSaveGameDelegate();
    public event OnBeforeSaveGameDelegate OnBeforeSaveGame;
    
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

    public bool SaveExistsInSlot(int slotIndex)
    {
        string slotFileName = $"save_{slotIndex}.json";
        string fullPath = Path.Combine(savePath, slotFileName);
        return File.Exists(fullPath);
    }
    public bool LoadGameFromSlot(int slotIndex)
    {
        string previousFileName = saveFileName;
        saveFileName = $"save_{slotIndex}.json";
        bool result = LoadGame();
        saveFileName = previousFileName; // Restaurar o arquivo padrão
        return result;
    }

    public void SaveGameToSlot(int slotIndex, string savePointName = "")
    {
        string previousFileName = saveFileName;
        saveFileName = $"save_{slotIndex}.json";
        SaveGame(savePointName);
        saveFileName = previousFileName; // Restaurar o arquivo padrão
    }

    public void DeleteSaveInSlot(int slotIndex)
    {
        try
        {
            string slotFileName = $"save_{slotIndex}.json";
            string fullPath = Path.Combine(savePath, slotFileName);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Debug.Log($"Save no slot {slotIndex} excluído com sucesso.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao excluir o save no slot {slotIndex}: {e.Message}");
        }
    }
    
    // Salvar o jogo
    public void SaveGame(string savePointName = "")
    {
        try
        {
            // Notificar todos os componentes antes de salvar
            OnBeforeSaveGame?.Invoke();
            
            // Limpar dados antigos
            currentSaveData.ClearObjectsData();
            
            // Encontrar o jogador
            PlayerController player = FindFirstObjectByType<PlayerController>();
            
            if (player != null)
            {
                // Salvar dados do jogador
                currentSaveData.playerPosition = player.transform.position;
                currentSaveData.playerDirection = player.currentDirection.ToString();
                
                // Salvar vida do jogador se existir o componente
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    currentSaveData.playerHealth = health.currentHealth;
                }
            }
            
            // Encontrar todos os objetos que implementam ISaveable
            ISaveable[] saveableObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<ISaveable>()
                .ToArray();
            
            Debug.Log($"Encontrados {saveableObjects.Length} objetos salvos para salvar.");
            
            // Salvar dados de cada objeto
            foreach (ISaveable saveable in saveableObjects)
            {
                MonoBehaviour mb = (MonoBehaviour)saveable;
                string objectId = GetUniqueObjectId(mb);
                
                object saveData = saveable.GetSaveData();
                if (saveData != null)
                {
                    string jsonData = JsonUtility.ToJson(saveData);
                    currentSaveData.AddObjectData(objectId, jsonData);
                    Debug.Log($"Salvando dados de: {objectId}");
                }
            }
            
            // Metadata do save
            currentSaveData.savePointName = savePointName;
            currentSaveData.saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            currentSaveData.gameVersion = Application.version;
            
            if (saveToDisk)
            {
                // Serializar para JSON
                string json = JsonUtility.ToJson(currentSaveData, true);
                
                // Processar dados (compressão/criptografia) se necessário
                byte[] saveData = System.Text.Encoding.UTF8.GetBytes(json);
                
                if (useCompression)
                {
                    saveData = CompressData(saveData);
                }
                
                if (encryptData)
                {
                    saveData = EncryptData(saveData);
                }
                
                // Salvar no arquivo
                string fullPath = Path.Combine(savePath, saveFileName);
                
                if (useCompression || encryptData)
                {
                    File.WriteAllBytes(fullPath, saveData);
                }
                else
                {
                    File.WriteAllText(fullPath, json);
                }
                
                Debug.Log($"Jogo salvo com sucesso em: {fullPath}");
            }
            else
            {
                Debug.Log("Save simulado (não salvo em disco)");
            }
            
            // Notificar que o jogo foi salvo
            OnGameSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao salvar o jogo: {e.Message}\n{e.StackTrace}");
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
            
            string json;
            
            // Verificar se estamos usando compressão ou criptografia
            if (useCompression || encryptData)
            {
                byte[] saveData = File.ReadAllBytes(fullPath);
                
                if (encryptData)
                {
                    saveData = DecryptData(saveData);
                }
                
                if (useCompression)
                {
                    saveData = DecompressData(saveData);
                }
                
                json = System.Text.Encoding.UTF8.GetString(saveData);
            }
            else
            {
                // Ler o arquivo diretamente como texto
                json = File.ReadAllText(fullPath);
            }
            
            // Deserializar
            currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
            
            // Verificar compatibilidade de versão se necessário
            if (currentSaveData.gameVersion != Application.version)
            {
                Debug.LogWarning($"O save foi criado em uma versão diferente do jogo. Atual: {Application.version}, Save: {currentSaveData.gameVersion}");
                // Adicione aqui lógica para migração de dados entre versões se necessário
            }
            
            // Aplicar os dados salvos
            ApplySaveData();
            
            Debug.Log($"Jogo carregado com sucesso de: {fullPath}");
            
            // Notificar que o jogo foi carregado
            OnGameLoaded?.Invoke();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao carregar o jogo: {e.Message}\n{e.StackTrace}");
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
            
            // Definir a direção do jogador
            if (Enum.TryParse(currentSaveData.playerDirection, out PlayerController.FacingDirection direction))
            {
                player.currentDirection = direction;
            }
            
            // Configurar vida do jogador
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.currentHealth = currentSaveData.playerHealth;
            }
        }
        
        // Encontrar todos os objetos salvos na cena
        ISaveable[] saveableObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<ISaveable>()
            .ToArray();
        
        Debug.Log($"Encontrados {saveableObjects.Length} objetos salvos para carregar.");
        
        // Aplicar dados salvos a cada objeto
        foreach (ISaveable saveable in saveableObjects)
        {
            MonoBehaviour mb = (MonoBehaviour)saveable;
            string objectId = GetUniqueObjectId(mb);
            
            string jsonData = currentSaveData.GetObjectData(objectId);
            
            if (!string.IsNullOrEmpty(jsonData))
            {
                // Determinar o tipo de dados da classe ISaveable
                Type componentType = mb.GetType();
                Type saveDataType = null;
                
                // Primeiro, tentar encontrar uma classe nested chamada SaveData
                saveDataType = componentType.GetNestedType("SaveData");
                
                // Se não encontrar, procurar por uma classe com nome ComponentNameSaveData
                if (saveDataType == null)
                {
                    saveDataType = Type.GetType($"{componentType.FullName}SaveData");
                }
                
                // Se não encontrar, usar um tipo genérico
                if (saveDataType == null)
                {
                    Debug.LogWarning($"Tipo de dados de save não encontrado para {componentType.Name}");
                    continue;
                }
                
                try
                {
                    object saveData = JsonUtility.FromJson(jsonData, saveDataType);
                    saveable.LoadFromSaveData(saveData);
                    Debug.Log($"Dados carregados para: {objectId}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Erro ao deserializar dados para {objectId}: {e.Message}");
                }
            }
        }
    }
    
    // Obter um ID único para cada objeto
    private string GetUniqueObjectId(MonoBehaviour mb)
    {
        // Usar uma combinação de nome, tipo e caminho hierárquico para garantir unicidade
        string path = GetGameObjectPath(mb.transform);
        return $"{path}_{mb.GetType().Name}";
    }
    
    // Obter caminho hierárquico de um GameObject
    private string GetGameObjectPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
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
    
    // Método para compressão de dados (exemplo simples)
    private byte[] CompressData(byte[] data)
    {
        try
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new System.IO.Compression.GZipStream(
                    memoryStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return memoryStream.ToArray();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro na compressão: {e.Message}");
            return data; // Retorna dados sem compressão em caso de erro
        }
    }
    
    // Método para descompressão de dados
    private byte[] DecompressData(byte[] data)
    {
        try
        {
            using (var compressedStream = new MemoryStream(data))
            using (var decompressStream = new System.IO.Compression.GZipStream(
                compressedStream, System.IO.Compression.CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                decompressStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro na descompressão: {e.Message}");
            return data; // Retorna dados sem descompressão em caso de erro
        }
    }
    
    // Método simples para criptografia de dados
    private byte[] EncryptData(byte[] data)
    {
        try
        {
            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(encryptionKey);
            byte[] result = new byte[data.Length];
            
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            }
            
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro na criptografia: {e.Message}");
            return data; // Retorna dados sem criptografia em caso de erro
        }
    }
    
    // Método simples para descriptografia de dados (usa o mesmo algoritmo XOR)
    private byte[] DecryptData(byte[] data)
    {
        return EncryptData(data); // XOR é reversível com a mesma chave
    }

    public string GetMostRecentSaveFile()
    {
        try
        {
            // Verificar cada slot e encontrar o mais recente
            string mostRecentFile = null;
            DateTime mostRecentDate = DateTime.MinValue;
            
            for (int i = 0; i < 10; i++)
            {
                string slotFileName = $"save_{i}.json";
                string fullPath = Path.Combine(savePath, slotFileName);
                
                if (File.Exists(fullPath))
                {
                    try
                    {
                        // Ler os metadados do save
                        string json = File.ReadAllText(fullPath);
                        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                        
                        // Converter a string de data para DateTime
                        if (DateTime.TryParse(saveData.saveDateTime, out DateTime saveDate))
                        {
                            if (saveDate > mostRecentDate)
                            {
                                mostRecentDate = saveDate;
                                mostRecentFile = slotFileName;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignorar arquivos corrompidos
                        continue;
                    }
                }
            }
            
            return mostRecentFile;
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao buscar save mais recente: {e.Message}");
            return null;
        }
    }

    // Adicione também este método para verificar se existe pelo menos um save
    public bool HasAnySave()
    {
        for (int i = 0; i < 10; i++)
        {
            if (SaveExistsInSlot(i))
                return true;
        }
        return false;
    }
}