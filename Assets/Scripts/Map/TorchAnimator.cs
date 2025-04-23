using UnityEngine;

public class TorchAnimator : MonoBehaviour
{
    [Header("Configurações de Animação")]
    public Sprite[] torchSprites;
    public float minChangeInterval = 0.4f;
    public float maxChangeInterval = 0.7f;
    
    private SpriteRenderer spriteRenderer;
    private int currentSpriteIndex;
    private float timer;
    private float interval;
    
    void Awake()
    {
        // Obtém o componente SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError("TorchAnimator requer um componente SpriteRenderer!", this);
            enabled = false;
            return;
        }
        
        if (torchSprites == null || torchSprites.Length == 0)
        {
            Debug.LogError("TorchAnimator requer pelo menos um sprite!", this);
            enabled = false;
            return;
        }
        
        // Inicializa com um sprite aleatório
        currentSpriteIndex = Random.Range(0, torchSprites.Length);
        spriteRenderer.sprite = torchSprites[currentSpriteIndex];
        
        // Define um temporizador inicial aleatório
        timer = Random.Range(0f, maxChangeInterval);
        interval = Random.Range(minChangeInterval, maxChangeInterval);
    }
    
    void Update()
    {
        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            ChangeSprite();
            
            // Reinicia o temporizador com um novo intervalo
            interval = Random.Range(minChangeInterval, maxChangeInterval);
            timer = interval;
        }
    }
    
    private void ChangeSprite()
    {
        // Escolhe um novo sprite diferente do atual
        if (torchSprites.Length > 1)
        {
            int newIndex;
            do
            {
                newIndex = Random.Range(0, torchSprites.Length);
            } while (newIndex == currentSpriteIndex);
            
            currentSpriteIndex = newIndex;
        }
        
        spriteRenderer.sprite = torchSprites[currentSpriteIndex];
    }
    
    // Método público para trocar o sprite manualmente se necessário
    public void ForceChangeSprite()
    {
        ChangeSprite();
        timer = Random.Range(minChangeInterval, maxChangeInterval);
    }
}