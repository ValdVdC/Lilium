using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InventoryManager : MonoBehaviour, ISaveable
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Image slotImage;            // Imagem do slot
    public Image itemImage;            // Imagem do item atual
    public Image slotBorderImage;      // Borda do slot para feedback visual
    public CanvasGroup panelCanvasGroup; // Componente CanvasGroup para controlar o alpha
    
    [Header("Item Sprites")]
    public Sprite daggerSprite;        // Sprite para adaga
    public Sprite shieldSprite;        // Sprite para escudo
    public Sprite keySprite;           // Sprite para chave
    public Sprite emptySlotSprite;     // Sprite para slot vazio
    
    [Header("Configuration")]
    public Color normalBorderColor = Color.white;
    public Color errorBorderColor = Color.red;
    public float errorFlashDuration = 0.5f;
    public float hideDelay = 1.0f;     // Tempo de espera antes de começar o fadeout
    public float fadeOutDuration = 0.5f; // Duração do efeito de fadeout
    
    private PuzzleItemType currentItem = PuzzleItemType.Empty;
    private Coroutine flashCoroutine;
    private Coroutine fadeCoroutine;
    
    [Serializable]
    public class SaveData
    {
        public string currentItem; // Armazenado como string do enum
    }

    // Implementar a interface ISaveable
    public object GetSaveData()
    {
        SaveData data = new SaveData
        {
            currentItem = currentItem.ToString()
        };
        return data;
    }

    public void LoadFromSaveData(object saveData)
    {
        if (saveData is SaveData data)
        {
            // Converter de string para enum
            if (Enum.TryParse(data.currentItem, out PuzzleItemType itemType))
            {
                currentItem = itemType;
                // Atualizar a UI
                UpdateInventoryUI();
            }
        }
    }

    void Start()
    {
        // Inicializar o estado do inventário
        currentItem = PuzzleItemType.Empty;
        
        // Garantir que temos uma referência ao CanvasGroup
        if (panelCanvasGroup == null && inventoryPanel != null)
        {
            panelCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Começar com o painel de inventário invisível
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
            
        // Atualizar UI uma vez para garantir configuração correta
        UpdateInventoryUI();
    }
    
    public bool AddItem(PuzzleItemType itemType)
    {
        // Verificação de debug
        Debug.Log("Tentando adicionar item ao inventário: " + itemType);
        
        // Se o item for Empty, não adicionar nada
        if (itemType == PuzzleItemType.Empty)
        {
            Debug.Log("Item é Empty, não adicionando");
            return false;
        }
        
        // Se já tiver um item e não for Empty, não pode adicionar outro
        if (currentItem != PuzzleItemType.Empty && itemType != PuzzleItemType.Empty)
        {
            Debug.Log("Inventário já tem um item, não pode adicionar outro");
            ShowErrorFeedback();
            return false;
        }
        
        // Adicionar novo item
        currentItem = itemType;
        Debug.Log("Item adicionado com sucesso: " + itemType);
        
        // Mostrar o painel de inventário
        if (inventoryPanel != null)
        {
            // Cancelar qualquer fadeout em andamento
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            
            // Garantir que o alpha está em 1
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1.0f;
            }
            
            inventoryPanel.SetActive(true);
            Debug.Log("Painel de inventário ativado");
        }
        else
        {
            Debug.LogWarning("Painel de inventário é null!");
        }
        
        UpdateInventoryUI();
        return true;
    }
    
    public bool RemoveItem(PuzzleItemType itemType)
    {
        if (currentItem == itemType)
        {
            currentItem = PuzzleItemType.Empty;
            UpdateInventoryUI();
            
            // Iniciar o fadeout do painel de inventário
            HidePanelWithFade();
                
            return true;
        }
        return false;
    }
    
    public bool HasItem(PuzzleItemType itemType)
    {
        return currentItem == itemType;
    }
    
    public PuzzleItemType GetCurrentItem()
    {
        return currentItem;
    }
    
    public void Clear()
    {
        currentItem = PuzzleItemType.Empty;
        UpdateInventoryUI();
        
        // Iniciar o fadeout do painel de inventário
        HidePanelWithFade();
    }
    
    private void UpdateInventoryUI()
    {
        Debug.Log("Atualizando UI do inventário. Item atual: " + currentItem);
        
        if (itemImage != null)
        {
            switch (currentItem)
            {
                case PuzzleItemType.Dagger:
                    itemImage.sprite = daggerSprite;
                    itemImage.enabled = true;
                    Debug.Log("Exibindo sprite da adaga");
                    break;
                case PuzzleItemType.Shield:
                    itemImage.sprite = shieldSprite;
                    itemImage.enabled = true;
                    Debug.Log("Exibindo sprite do escudo");
                    break;
                case PuzzleItemType.Key:
                    itemImage.sprite = keySprite;
                    itemImage.enabled = true;
                    Debug.Log("Exibindo sprite da chave");
                    break;    
                case PuzzleItemType.Empty:
                default:
                    itemImage.enabled = false;
                    Debug.Log("Nenhum sprite exibido (vazio)");
                    break;
            }
        }
        else
        {
            Debug.LogWarning("itemImage é null!");
        }
        
        // Atualizar a visibilidade do painel
        UpdatePanelVisibility();
    }
    
    // Método auxiliar para atualizar a visibilidade do painel
    private void UpdatePanelVisibility()
    {
        if (inventoryPanel != null)
        {
            bool shouldShow = currentItem != PuzzleItemType.Empty;
            
            if (shouldShow)
            {
                // Se deve mostrar, cancelar qualquer fadeout e ativar o painel
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                    fadeCoroutine = null;
                }
                
                if (panelCanvasGroup != null)
                {
                    panelCanvasGroup.alpha = 1.0f;
                }
                
                inventoryPanel.SetActive(true);
            }
            else
            {
                // Se deve esconder, iniciar fadeout
                HidePanelWithFade();
            }
            
            Debug.Log("Atualizando visibilidade do painel: " + (shouldShow ? "Visível" : "Iniciando fadeout"));
        }
    }
    
    // Método para esconder o painel com fadeout
    private void HidePanelWithFade()
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            // Cancelar qualquer fadeout anterior
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            // Iniciar nova coroutine de fadeout
            fadeCoroutine = StartCoroutine(FadeOutPanel());
        }
    }
    
    // Coroutine para controlar o fadeout
    private IEnumerator FadeOutPanel()
    {
        // Primeiro, esperar o delay configurado
        yield return new WaitForSeconds(hideDelay);
        
        // Garantir que temos uma referência ao CanvasGroup
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Garantir que o alpha começa em 1
        panelCanvasGroup.alpha = 1.0f;
        
        // Fazer o fadeout gradualmente
        float elapsedTime = 0;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            
            // Aplicar o alpha no CanvasGroup
            panelCanvasGroup.alpha = 1.0f - normalizedTime;
            
            yield return null;
        }
        
        // Garantir que o alpha está em 0
        panelCanvasGroup.alpha = 0.0f;
        
        // Finalmente, desativar o painel
        inventoryPanel.SetActive(false);
        
        // Resetar o alpha para o próximo uso
        panelCanvasGroup.alpha = 1.0f;
        
        fadeCoroutine = null;
    }
    
    public void ShowErrorFeedback()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        
        flashCoroutine = StartCoroutine(FlashBorder());
    }
    
    private IEnumerator FlashBorder()
    {
        if (slotBorderImage != null)
        {
            // Guardar cor original
            Color originalColor = slotBorderImage.color;
            
            // Mudar para cor de erro
            slotBorderImage.color = errorBorderColor;
            
            // Esperar pelo tempo definido
            yield return new WaitForSeconds(errorFlashDuration);
            
            // Restaurar cor original
            slotBorderImage.color = originalColor;
        }
        
        flashCoroutine = null;
    }
}