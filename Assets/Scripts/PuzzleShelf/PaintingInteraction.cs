using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class PaintingInteraction : MonoBehaviour
{
    [Header("Estado do Painting")]
    public bool paintingActive = false;       // Indica se o painting está ativo

    [Header("Interface e Feedback")]
    public GameObject interactionKeyIcon;     // Ícone de tecla E para interagir
    public GameObject paintingUI;             // UI com instruções

    [Header("Câmera")]
    public CameraController cameraController;  // Controlador da câmera
    public Transform paintingPosition;         // Posição do quadro para a câmera focar
    public float paintingCameraSize = 3.0f;    // Tamanho da câmera durante o painting
    
    private bool playerInRange = false;        // Jogador está no alcance de interação

    private void Awake()
    {
        // Desativar interface inicialmente
        if (interactionKeyIcon != null)
            interactionKeyIcon.SetActive(false);

        // Desativar componentes de UI no início
        if (paintingUI != null)
            paintingUI.SetActive(false);
            
        Debug.Log("[PAINTING] Componente inicializado");
    }

    private void Update()
    {
        // Verificar interação quando o painting está ativo
        if (paintingActive)
        {
            // Tecla E para sair da interação
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[PAINTING] Saindo da interação com o quadro");
                StopPaintingInteraction();
                return;
            }
        }
        // Verificar interação do jogador quando não estiver no painting
        else if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[PAINTING] Iniciando interação com o quadro");
            StartPaintingInteraction();
        }
    }

    // Inicia a interação com o painting
    public void StartPaintingInteraction()
    {
        paintingActive = true;
        
        // Esconder ícone de interação
        if (interactionKeyIcon != null)
            interactionKeyIcon.SetActive(false);
        
        // Desativar o jogador e componentes relacionados
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("[PAINTING] Desativando controles do jogador");
            playerController.enabled = false;
            playerController.spriteRenderer.enabled = false;
            
            // Desativar InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = false;
                
            // Desativar outros componentes do jogador
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(false);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(false);
        }

        // Ativar UI do puzzle
        if (paintingUI != null)
        {
            paintingUI.SetActive(true);
            Debug.Log("[PAINTING] UI de interação ativada");
        }

        // Configurar câmera
        if (cameraController != null && paintingPosition != null)
        {
            Debug.Log("[PAINTING] Ativando câmera do painting");
            // Verifique se esse método existe no seu CameraController!
            cameraController.ActivateShelfPuzzleCamera(paintingPosition, paintingCameraSize);
            // OU crie um método específico para o quadro no CameraController:
            // cameraController.ActivatePaintingCamera(paintingPosition, paintingCameraSize);
        }
        else
        {
            Debug.LogError("[PAINTING] Referências de câmera não definidas!");
        }
    }
    
    // Finaliza a interação com o painting
    public void StopPaintingInteraction()
    {
        paintingActive = false;
        
        // Mostrar ícone de interação
        if (interactionKeyIcon != null && playerInRange)
            interactionKeyIcon.SetActive(true);
        
        // Reativar jogador
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("[PAINTING] Reativando controles do jogador");
            playerController.enabled = true;
            playerController.spriteRenderer.enabled = true;
            
            // Reativar InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = true;
                
            // Reativar outros componentes do jogador
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(true);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(true);
        }

        // Desativar UI do puzzle
        if (paintingUI != null)
        {
            paintingUI.SetActive(false);
            Debug.Log("[BOOKSHELF PUZZLE] UI de interação desativada");
        }

        // Retornar câmera ao normal
        if (cameraController != null)
        {
            Debug.Log("[PAINTING] Retornando para a câmera principal");
            cameraController.ReturnToMainCamera();
        }
    }

    // Detector para quando o jogador entra no alcance
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[PAINTING] Jogador entrou na área de interação");
            
            // Mostrar ícone de interação
            if (!paintingActive && interactionKeyIcon != null)
            {
                interactionKeyIcon.SetActive(true);
                Debug.Log("[PAINTING] Mostrou ícone de interação");
            }
        }
    }
    
    // Detector para quando o jogador sai do alcance
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("[PAINTING] Jogador saiu da área de interação");
            
            // Esconder ícone de interação
            if (interactionKeyIcon != null)
            {
                interactionKeyIcon.SetActive(false);
                Debug.Log("[PAINTING] Escondeu ícone de interação");
            }
        }
    }
    
    // Método para implementação da interface IInteractable, se necessário
    public void Interact()
    {
        if (!paintingActive && playerInRange)
        {
            Debug.Log("[PAINTING] Interação iniciada através da interface IInteractable");
            StartPaintingInteraction();
        }
    }
}