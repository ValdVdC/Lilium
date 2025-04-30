using UnityEngine;

public class InteractorController : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactableLayers;
    public GameObject interactionPrompt;
    
    private BoxCollider2D playerCollider;
    private IInteractable currentInteractable;

    private void Start()
    {
        // Obter o BoxCollider2D do player
        playerCollider = GetComponent<BoxCollider2D>();
        
        if (playerCollider == null)
        {
            Debug.LogError("BoxCollider2D não encontrado no GameObject do player. Adicione um BoxCollider2D ao player.");
        }
        
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (playerCollider == null)
            return;
            
        // Encontrar objetos interativos usando o BoxCollider2D
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            transform.position, 
            playerCollider.size, 
            transform.rotation.eulerAngles.z, 
            interactableLayers
        );
        
        float closestDistance = float.MaxValue;
        IInteractable closestInteractable = null;
        
        foreach (Collider2D collider in colliders)
        {
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }
        
        // Atualizar objeto interativo atual
        if (closestInteractable != currentInteractable)
        {
            currentInteractable = closestInteractable;
            
            if (interactionPrompt != null)
                interactionPrompt.SetActive(currentInteractable != null);
        }
        
        // Verificar input para interagir
        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact();
        }
    }
    
    // Método opcional para visualizar o range de interação na janela Scene do editor
    private void OnDrawGizmos()
    {
        if (playerCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, playerCollider.size);
        }
    }
}