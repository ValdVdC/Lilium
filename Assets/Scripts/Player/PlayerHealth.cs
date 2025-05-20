using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour, ISaveable
{
    public float maxHealth = 100f;
    public float currentHealth;

    [Serializable]
    public class SaveData
    {
        public float currentHealth;
        public float maxHealth;
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Player tomou {damage} de dano! Vida atual: {currentHealth}");

        // Limite a vida para não ficar negativa
        currentHealth = Mathf.Max(currentHealth, 0);

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log("Player morreu!");
        
        // Verificar se a tela de morte está disponível
        DeathScreen deathScreen = DeathScreen.Instance;
        
        if (deathScreen != null)
        {
            // Mostrar a tela de morte
            deathScreen.ShowDeathScreen();
        }
        else
        {
            Debug.LogError("DeathScreen não encontrada! Certifique-se de que ela existe na cena.");
        }
    }
    
    // Implementação de ISaveable
    public object GetSaveData()
    {
        SaveData data = new SaveData
        {
            currentHealth = this.currentHealth,
            maxHealth = this.maxHealth
        };
        return data;
    }
    
    public void LoadFromSaveData(object saveData)
    {
        if (saveData is SaveData data)
        {
            currentHealth = data.currentHealth;
            maxHealth = data.maxHealth;
        }
    }
}