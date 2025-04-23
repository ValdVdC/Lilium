using UnityEngine;

public class InteractionHighlight : MonoBehaviour
{
    public SpriteRenderer highlightSprite;
    public float pulseSpeed = 2f;
    public float minScale = 0.9f;
    public float maxScale = 1.1f;
    public KeyCode interactionKey = KeyCode.E;
    public GameObject keyPrompt;
    
    private bool isPlayerNearby = false;
    
    void Start()
    {
        if (highlightSprite == null)
            highlightSprite = GetComponent<SpriteRenderer>();
            
            
        if (keyPrompt != null)
            keyPrompt.SetActive(false);
    }
    
    void Update()
    {
        if (isPlayerNearby && highlightSprite != null)
        {
            // Efeito de pulso
            float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1) / 2);
            highlightSprite.transform.localScale = new Vector3(scale, scale, 1f);
            
            // Mostrar dica de tecla
            if (keyPrompt != null)
                keyPrompt.SetActive(true);
        }
        else
        {    
            // Esconder dica de tecla
            if (keyPrompt != null)
                keyPrompt.SetActive(false);
        }
    }
    
    // Chamado quando o jogador entra na área de interação
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }
    
    // Chamado quando o jogador sai da área de interação
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }
}