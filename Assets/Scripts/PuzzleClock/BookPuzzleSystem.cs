using UnityEngine;
using UnityEngine.UI;

public class BookPuzzleSystem : MonoBehaviour, IInteractable
{
    [Header("Referências")]
    public GameObject bookViewUI;              // Canvas/Painel que mostra o livro
    public TMPro.TMP_Text bookContentText;     // Componente de texto que mostra o conteúdo do livro
    public GameObject interactionKeyIcon;      // Ícone da tecla de interação

    [Header("Conteúdo do Livro")]
    [TextArea(5, 10)]
    public string bookContent = "O tempo é o mestre dos segredos. Quando o grande relógio da torre alinhar suas agulhas, o mistério se revelará.\n\nPrimeiro, a hora deve apontar para o número de apóstolos que seguiram o mestre.\n\nEm seguida, os minutos devem marcar o dobro das horas mundanas em um dia.\n\nPor fim, os segundos devem indicar o número de dias em dois meses lunares.\n\nLembre-se: A hora avança como o sol, do leste para oeste. Os minutos recuam como as marés, contra a natureza. E os segundos seguem o caminho natural do tempo, sempre em frente.";

    [Header("Configurações")]
    public bool hasBeenRead = false;           // Indica se o jogador já leu o livro

    [Header("Referências Externas")]
    public ClockPuzzleManager clockPuzzleManager;  // Referência ao gerenciador do puzzle do relógio

    private PlayerController playerController;    // Referência ao controlador do jogador
    private bool isBookOpen = false;              // Indica se o livro está aberto
    private float lastInteractionTime = 0f;       // Para evitar que abra e feche instantaneamente
    private const float interactionCooldown = 0.5f; // Tempo mínimo entre interações
    private bool playerInRange = false;           // Indica se o jogador está na área de interação

    private void Start()
    {
        // Desativar a UI do livro no início
        if (bookViewUI != null)
            bookViewUI.SetActive(false);
            
        // Esconder o ícone de interação no início
        if (interactionKeyIcon != null)
            interactionKeyIcon.SetActive(false);
            
        // Encontrar o PlayerController na cena
        playerController = FindFirstObjectByType<PlayerController>();
        
        if (clockPuzzleManager == null)
            clockPuzzleManager = FindFirstObjectByType<ClockPuzzleManager>();
    }

    private void Update()
    {
        // Verificar se o livro está aberto e se o jogador pressionou E para fechá-lo
        if (isBookOpen && Input.GetKeyDown(KeyCode.E) && Time.time > lastInteractionTime + interactionCooldown)
        {
            lastInteractionTime = Time.time;
            CloseBook();
            Debug.Log("Book closed with E key");
        }
    }

    public void Interact()
    {
        // Quando o jogador interage com o livro (chamado pelo sistema de interação)
        if (!isBookOpen && Time.time > lastInteractionTime + interactionCooldown)
        {
            lastInteractionTime = Time.time;
            OpenBook();
            Debug.Log("Book opened through Interact()");
        }
    }

    public void OpenBook()
    {
        // Desativar movimento do jogador
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
            
            // Desativar também o InteractorController para evitar outras interações
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = false;
        }

        // Ativar a UI do livro
        if (bookViewUI != null)
        {
            bookViewUI.SetActive(true);
            Debug.Log("Book UI activated");
        }
        
        // Esconder o ícone de interação enquanto o livro está aberto
        if (interactionKeyIcon != null)
            interactionKeyIcon.SetActive(false);

        // Definir o conteúdo do livro
        if (bookContentText != null)
            bookContentText.text = bookContent;

        // Marcar o livro como lido
        hasBeenRead = true;
        
        // Marcar o livro como aberto
        isBookOpen = true;

        // Notificar o gerenciador do puzzle do relógio que o livro foi lido
        if (clockPuzzleManager != null)
            clockPuzzleManager.BookWasRead();
    }

    public void CloseBook()
    {
        // Reativar movimento do jogador
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
            
            // Reativar o InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = true;
        }

        // Desativar a UI do livro
        if (bookViewUI != null)
        {
            bookViewUI.SetActive(false);
            Debug.Log("Book UI deactivated");
        }
            
        // Mostrar novamente o ícone de interação se o jogador ainda estiver na área
        if (interactionKeyIcon != null && playerInRange)
            interactionKeyIcon.SetActive(true);
            
        // Marcar o livro como fechado
        isBookOpen = false;
    }
    
    // Chamado quando um jogador entra na área de interação (trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar se é o player que entrou no trigger
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            // Mostrar o ícone de interação se o livro não estiver aberto
            if (!isBookOpen && interactionKeyIcon != null)
                interactionKeyIcon.SetActive(true);
                
            Debug.Log("Player entered interaction range");
        }
    }
    
    // Chamado quando um jogador sai da área de interação (trigger)
    private void OnTriggerExit2D(Collider2D other)
    {
        // Verificar se é o player que saiu do trigger
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            // Esconder o ícone de interação
            if (interactionKeyIcon != null)
                interactionKeyIcon.SetActive(false);
                
            Debug.Log("Player left interaction range");
        }
    }
}