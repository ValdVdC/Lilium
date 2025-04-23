using UnityEngine;

public class InteractorController : MonoBehaviour
{
    public float interactionRadius = 1.5f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactableLayers;
    public GameObject interactionPrompt;

    private IInteractable currentInteractable;

    private void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        // Encontrar objeto interativo mais próximo
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayers);
        
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
}