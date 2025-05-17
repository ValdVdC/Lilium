using UnityEngine;

public class InteractorController : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactableLayers;
    public float interactionRange = 2.0f; // Range de interação
    
    private BoxCollider2D playerCollider;
    private IInteractable currentInteractable;
    private GameObject currentInteractableObject;

    private void Start()
    {
        // Obter o BoxCollider2D do player
        playerCollider = GetComponent<BoxCollider2D>();
        
        if (playerCollider == null)
        {
            Debug.LogError("BoxCollider2D não encontrado no GameObject do player. Adicione um BoxCollider2D ao player.");
        }
    }

    private void Update()
    {
        if (playerCollider == null)
            return;
            
        // Encontrar objetos interativos usando o BoxCollider2D
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            transform.position, 
            new Vector2(interactionRange, interactionRange), 
            transform.rotation.eulerAngles.z, 
            interactableLayers
        );
        
        float closestDistance = float.MaxValue;
        IInteractable closestInteractable = null;
        GameObject closestObject = null;
        
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
                    closestObject = collider.gameObject;
                }
            }
        }
        
        // Verificar se o objeto interativo mudou
        if (closestObject != currentInteractableObject)
        {
            // Desativar o ícone do objeto anterior
            if (currentInteractableObject != null)
            {
                BarrelInteraction barrel = currentInteractableObject.GetComponent<BarrelInteraction>();
                if (barrel != null)
                {
                    barrel.HideInteractionIcon();
                }
                
                // Adicionado: verificar também TableInteraction
                TableInteraction table = currentInteractableObject.GetComponent<TableInteraction>();
                if (table != null)
                {
                    table.HideInteractionIcon();
                }
            }
            
            // Atualizar referências
            currentInteractable = closestInteractable;
            currentInteractableObject = closestObject;
            
            // Ativar o ícone do novo objeto
            if (currentInteractableObject != null)
            {
                BarrelInteraction barrel = currentInteractableObject.GetComponent<BarrelInteraction>();
                if (barrel != null)
                {
                    barrel.ShowInteractionIcon();
                }
                
                // Adicionado: verificar também TableInteraction
                TableInteraction table = currentInteractableObject.GetComponent<TableInteraction>();
                if (table != null)
                {
                    table.ShowInteractionIcon();
                }
            }
        }
        else if (closestObject != null && currentInteractableObject == closestObject)
        {
            // Garantir que o ícone esteja visível se ainda estivermos no range
            BarrelInteraction barrel = closestObject.GetComponent<BarrelInteraction>();
            if (barrel != null && !barrel.isOpen)
            {
                barrel.ShowInteractionIcon();
            }
            
            // Adicionado: verificar também TableInteraction
            TableInteraction table = closestObject.GetComponent<TableInteraction>();
            if (table != null && !table.isPuzzleOpen && table.interactionEnabled)
            {
                table.ShowInteractionIcon();
            }
        }
        
        // Verificar input para interagir
        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact();
        }
    }
    
    // Método para forçar a reavaliação do objeto interativo atual
    public void ResetCurrentInteractable()
    {
        currentInteractableObject = null;
        currentInteractable = null;
    }
    
    // Método opcional para visualizar o range de interação na janela Scene do editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(interactionRange, interactionRange, 0.1f));
    }
}