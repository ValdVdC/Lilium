using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BarrelInteraction : MonoBehaviour, IInteractable
{
    [Header("References")]
    public Transform barrelCameraPosition;
    public float barrelCameraSize = 2.0f;
    public GameObject barrelOpenView;
    public GameObject puzzleItem;
    public PuzzleItemType itemType;
    public PuzzleManager puzzleManager;
    public PuzzleAudioManager audioManager;
    public CameraController cameraController;

    private bool isOpen = false;
    private PlayerController playerController;

    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        barrelOpenView.SetActive(false);
        
        if (audioManager == null)
            audioManager = FindFirstObjectByType<PuzzleAudioManager>();
        if (puzzleManager == null)
            puzzleManager = FindFirstObjectByType<PuzzleManager>();
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();
    }

    public void Interact()
    {
        if (!isOpen)
        {
            OpenBarrel();
        }
        else
        {
            CloseBarrel();
        }
    }

    private void OpenBarrel()
    {
        isOpen = true;

        // Disable player movement and interaction
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
            playerController.spriteRenderer.enabled = false;
            
            // Desativar também o InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = false;
                
            // Se houver componentes de luz/sombra, desative-os
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(false);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(false);
            ShadowCaster2D shadowCaster = GetComponent<ShadowCaster2D>();
            if (shadowCaster != null) shadowCaster.enabled = false;
        }

        // Usar o CameraController para ativar a câmera do barril
        if (cameraController != null)
        {
            cameraController.ActivateBarrelCamera(barrelCameraPosition, barrelCameraSize);
        }
        
        barrelOpenView.SetActive(true);

        if (puzzleItem != null)
        {
            string barrelId = gameObject.name;
            bool alreadyCollected = puzzleManager.IsItemCollected(barrelId);
            puzzleItem.SetActive(!alreadyCollected);
        }
        if (audioManager != null)
            audioManager.PlayBarrelOpen();
    }

    private void CloseBarrel()
    {
        isOpen = false;

        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
            playerController.spriteRenderer.enabled = true;
            
            // Reativar o InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = true;
                
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(true);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(true);

            ShadowCaster2D shadowCaster = GetComponent<ShadowCaster2D>();
            if (shadowCaster != null) shadowCaster.enabled = true;
        }

        // Voltar para a câmera principal
        if (cameraController != null)
        {
            cameraController.ReturnToMainCamera();
        }
        
        barrelOpenView.SetActive(false);
        if (audioManager != null)
            audioManager.PlayBarrelClose();
    }

    private void Update()
    {
        if (isOpen)
        {
            // Verificar clique do mouse para pegar o item
            if (Input.GetMouseButtonDown(0))
            {
                // Usar a câmera do barril para o raycasting
                Camera currentCamera = cameraController.barrelCamera;
                
                // Verificar se clicou no item
                Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
                
                if (hit.collider != null && hit.collider.gameObject == puzzleItem)
                {
                    // Add item to inventory
                    puzzleManager.AddItemToInventory(itemType);
                    
                    // Disable the puzzle item permanently
                    puzzleItem.SetActive(false);
                    
                    // Store that this barrel's item was collected
                    string barrelId = gameObject.name; // Use the GameObject name as ID
                    puzzleManager.MarkItemCollected(barrelId);
                    
                    // Fechar o barril somente quando o item é coletado
                    CloseBarrel();
                }
                // Removemos o CloseBarrel() que estava fora do if, pois não queremos mais fechar o barril em qualquer clique
            }
            
            // Verificar se pressionou E novamente para fechar o barril
            if (Input.GetKeyDown(KeyCode.E))
            {
                CloseBarrel();
            }
        }
    }
}