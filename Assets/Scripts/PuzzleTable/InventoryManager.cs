using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform itemContainer;
    public GameObject daggerItemPrefab;
    public GameObject shieldItemPrefab;
    
    [Header("Configuration")]
    public int maxItems = 8;
    
    private List<PuzzleItemType> items = new List<PuzzleItemType>();
    private List<GameObject> itemObjects = new List<GameObject>();
    
    void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }
    
    public void ShowInventory(bool show)
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(show);
    }
    
    public bool AddItem(PuzzleItemType itemType)
    {
        if (items.Count >= maxItems || itemType == PuzzleItemType.Empty)
            return false;
            
        items.Add(itemType);
        UpdateInventoryUI();
        return true;
    }
    
    public bool RemoveItem(PuzzleItemType itemType)
    {
        if (items.Contains(itemType))
        {
            items.Remove(itemType);
            UpdateInventoryUI();
            return true;
        }
        
        return false;
    }
    
    public bool HasItem(PuzzleItemType itemType)
    {
        return items.Contains(itemType);
    }
    
    public int GetItemCount(PuzzleItemType itemType)
    {
        int count = 0;
        foreach (PuzzleItemType item in items)
        {
            if (item == itemType)
                count++;
        }
        
        return count;
    }
    
    private void UpdateInventoryUI()
    {
        // Limpar objetos existentes
        foreach (GameObject obj in itemObjects)
        {
            Destroy(obj);
        }
        
        itemObjects.Clear();
        
        // Criar novos objetos para cada item
        for (int i = 0; i < items.Count; i++)
        {
            GameObject prefab = null;
            
            switch (items[i])
            {
                case PuzzleItemType.Dagger:
                    prefab = daggerItemPrefab;
                    break;
                case PuzzleItemType.Shield:
                    prefab = shieldItemPrefab;
                    break;
            }
            
            if (prefab != null)
            {
                GameObject itemObj = Instantiate(prefab, itemContainer);
                itemObjects.Add(itemObj);
                
                // Configurar posição na grade de inventário
                RectTransform rt = itemObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Configuração de acordo com seu layout de grade
                    // Este é apenas um exemplo básico
                    float slotSize = 70f; // Tamanho do slot + margem
                    int columns = 4;
                    int row = i / columns;
                    int col = i % columns;
                    
                    rt.anchoredPosition = new Vector2(col * slotSize, -row * slotSize);
                }
            }
        }
    }
    
    public void Clear()
    {
        items.Clear();
        UpdateInventoryUI();
    }
}