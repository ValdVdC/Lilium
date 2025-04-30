using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class ClockPuzzleManager : MonoBehaviour
{
    [Header("Estado do Puzzle")]
    public bool bookHasBeenRead = false;       // Indica se o livro foi lido
    public bool puzzleSolved = false;          // Indica se o puzzle foi resolvido
    
    [Header("Configuração da Solução")]
    [Range(1, 12)]
    public int correctHour = 12;               // Posição correta do ponteiro das horas (12 = apóstolos)
    [Range(0, 59)]
    public int correctMinute = 48;             // Posição correta do ponteiro dos minutos (48 = 2x24 horas)
    [Range(0, 59)]
    public int correctSecond = 56;             // Posição correta do ponteiro dos segundos (56 = 2x28 dias lunares)
    
    public enum Direction { Clockwise, CounterClockwise }
    public Direction correctHourDirection = Direction.Clockwise;      // Direção correta para o ponteiro das horas (como o sol)
    public Direction correctMinuteDirection = Direction.CounterClockwise; // Direção correta para o ponteiro dos minutos (contra a natureza)
    public Direction correctSecondDirection = Direction.Clockwise;    // Direção correta para o ponteiro dos segundos (caminho natural)
    
    [Header("Referências")]
    public ClockHandController hourHand;       // Controlador do ponteiro das horas
    public ClockHandController minuteHand;     // Controlador do ponteiro dos minutos
    public ClockHandController secondHand;     // Controlador do ponteiro dos segundos
    public GameObject clockInteractionUI;      // UI para interação com o relógio
    public Button confirmButton;               // Botão para confirmar a configuração
    public GameObject interactionKeyIcon;      // Ícone da tecla de interação (NOVO!)
    
    [Header("Feedback")]
    public GameObject successFeedback;         // Objeto/Efeito para feedback de sucesso
    public GameObject failureFeedback;         // Objeto/Efeito para feedback de falha
    
    [Header("Câmera")]
    public CameraController cameraController;  // Referência ao controlador de câmera
    public Transform clockPosition;            // Posição do relógio para a câmera olhar

    public float puzzleCameraSize = 2.0f;

    [Header("Áudio")]
    public PuzzleAudioManager audioManager;    // Referência ao gerenciador de áudio
    
    [Header("Recompensa")]
    public GameObject rewardObject;            // Objeto que aparece quando o puzzle é resolvido
    
    [Header("Requisitos")]
    public bool requiresBookToBeRead = true;   // Controla o requisito de livro
    
    private int currentActiveHandIndex = 0;     // 0 = horas, 1 = minutos, 2 = segundos
    private ClockHandController[] hands;       // Array com todos os ponteiros
    private Direction[] handDirections;        // Direções usadas para cada ponteiro
    private bool playerInRange = false;        // Indica se o jogador está na área de interação (NOVO!)
    private bool isClockActive = false;        // Indica se o relógio está em interação (NOVO!)
    
    private void Awake()
    {
        // Inicializar arrays
        hands = new ClockHandController[3];
        hands[0] = hourHand;
        hands[1] = minuteHand;
        hands[2] = secondHand;
        
        handDirections = new Direction[3];
        
        // Configurar botão de confirmação
        if (confirmButton != null)
            confirmButton.onClick.AddListener(CheckSolution);
        
        // Desativar UI de interação e feedbacks no início
        if (clockInteractionUI != null)
            clockInteractionUI.SetActive(false);
        
        if (successFeedback != null)
            successFeedback.SetActive(false);
        
        if (failureFeedback != null)
            failureFeedback.SetActive(false);
        
        if (rewardObject != null)
            rewardObject.SetActive(false);
            
        // Desativar o ícone de interação no início (NOVO!)
        if (interactionKeyIcon != null)
            interactionKeyIcon.SetActive(false);
        
        // Configurar os ponteiros
        SetupClockHands();
        
        // Log de configuração inicial
        Debug.Log("[CLOCK PUZZLE] Puzzle inicializado com valores esperados:");
        Debug.Log($"[CLOCK PUZZLE] Solução: Hora={correctHour} (Direção={correctHourDirection}), Minuto={correctMinute} (Direção={correctMinuteDirection}), Segundo={correctSecond} (Direção={correctSecondDirection})");
    }

    private void Update()
    {
        // Verificar saída da interação
        if (clockInteractionUI != null && clockInteractionUI.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[CLOCK PUZZLE] Saindo da interação com o relógio");
                FinishInteraction();
                return;
            }
            
            // Verificar confirmação do ponteiro atual
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                string ponteiro = currentActiveHandIndex == 0 ? "Horas" : 
                                  currentActiveHandIndex == 1 ? "Minutos" : "Segundos";
                
                int posicao = hands[currentActiveHandIndex].CurrentPosition;
                Direction direcao = handDirections[currentActiveHandIndex];
                
                Debug.Log($"[CLOCK PUZZLE] Confirmando seleção do ponteiro: {ponteiro}, Posição: {posicao + (currentActiveHandIndex == 0 ? 1 : 0)}, Direção: {direcao}");
                NextHand();
            }
        }
        // Verificar interação com o relógio quando o jogador estiver no alcance (NOVO!)
        else if (playerInRange && !isClockActive && !puzzleSolved && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[CLOCK PUZZLE] Jogador pressionou E para interagir com o relógio");
            StartClockInteraction();
        }
    }
    
    private void SetupClockHands()
    {
        // Configurar cada ponteiro
        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i] != null)
            {
                hands[i].SetupHand(i == 0 ? 12 : 60); // 12 posições para horas, 60 para minutos e segundos
                hands[i].OnHandMoved += OnHandMoved;  // Registrar evento de movimento
            }
        }
    }
    
    // Chamado quando o livro é lido
    public void BookWasRead()
    {
        bookHasBeenRead = true;
        Debug.Log("[CLOCK PUZZLE] Livro foi lido: " + bookHasBeenRead);
    }
    
    // Inicia a interação com o relógio
    public void StartClockInteraction()
    {
        if (requiresBookToBeRead && !bookHasBeenRead)
        {
            Debug.Log("[CLOCK PUZZLE] O jogador precisa ler o livro primeiro!");
            return;
        }
        
        Debug.Log("[CLOCK PUZZLE] Iniciando interação com o relógio");
        isClockActive = true;  // Marcar o relógio como ativo (NOVO!)
        
        // Esconder o ícone de interação enquanto estiver interagindo (NOVO!)
        if (interactionKeyIcon != null)
            interactionKeyIcon.SetActive(false);
        
        // Desativar o jogador e componentes relacionados
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("[CLOCK PUZZLE] Desativando controles do jogador");
            playerController.enabled = false;
            playerController.spriteRenderer.enabled = false;
            
            // Desativar InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = false;

            // Desativar percepção e lanterna
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(false);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(false);

            // Desativar sombras
            ShadowCaster2D shadowCaster = GetComponent<ShadowCaster2D>();
            if (shadowCaster != null) shadowCaster.enabled = false;
        }
        
        // Ativar UI de interação
        if (clockInteractionUI != null)
        {
            clockInteractionUI.SetActive(true);
            Debug.Log("[CLOCK PUZZLE] UI de interação do relógio ativada");
        }
        
        // Mudar para a câmera do relógio
        if (cameraController != null && clockPosition != null)
        {
            Debug.Log("[CLOCK PUZZLE] Ativando câmera do relógio");
            cameraController.ActivateClockPuzzleCamera(clockPosition, puzzleCameraSize);
        }
        
        // Ativar o primeiro ponteiro (horas)
        currentActiveHandIndex = 0;
        ActivateCurrentHand();
        
        // Tocar música de puzzle
        if (audioManager != null)
            audioManager.PlayPuzzleMusic();
            
        // Log status inicial dos ponteiros
        LogClockHandsStatus();
    }
    
    // Ativa o ponteiro atual e desativa os outros
    private void ActivateCurrentHand()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i] != null)
                hands[i].SetActive(i == currentActiveHandIndex);
        }
        
        string ponteiro = currentActiveHandIndex == 0 ? "Horas" : 
                         currentActiveHandIndex == 1 ? "Minutos" : "Segundos";
        Debug.Log($"[CLOCK PUZZLE] Ponteiro ativo agora: {ponteiro}");
    }
    
    // Evento chamado quando um ponteiro é movido
    private void OnHandMoved(ClockHandController hand, Direction direction)
    {
        // Registrar a direção do movimento para o ponteiro atual
        handDirections[currentActiveHandIndex] = direction;
        
        // Identificar qual ponteiro foi movido
        string ponteiro = "Desconhecido";
        if (hand == hourHand) ponteiro = "Horas";
        else if (hand == minuteHand) ponteiro = "Minutos";
        else if (hand == secondHand) ponteiro = "Segundos";
        
        // Mostrar informação sobre o movimento
        Debug.Log($"[CLOCK PUZZLE] Movimento do ponteiro: {ponteiro}, Posição: {hand.CurrentPosition + (hand == hourHand ? 1 : 0)}, Direção: {direction}");
        
        // Tocar som de movimento
        if (audioManager != null)
            audioManager.PlayPieceMovement();
    }
    
    // Avança para o próximo ponteiro
    public void NextHand()
    {
        currentActiveHandIndex++;
        
        if (currentActiveHandIndex >= hands.Length)
        {
            // Se já configuramos todos os ponteiros, verificar a solução
            Debug.Log("[CLOCK PUZZLE] Todos os ponteiros configurados, verificando solução...");
            CheckSolution();
        }
        else
        {
            // Caso contrário, ativar o próximo ponteiro
            ActivateCurrentHand();
        }
    }
    
    // Verifica se a solução está correta
    public void CheckSolution()
    {
        bool isCorrect = true;
        bool correctPositions = true;
        bool correctDirections = true;
        
        // Log detalhado do estado atual
        LogClockHandsStatus();
        
        // Verificar posições dos ponteiros
        if (hourHand != null && hourHand.CurrentPosition != correctHour - 1) // -1 porque os índices começam em 0
        {
            correctPositions = false;
            Debug.Log($"[CLOCK PUZZLE] [ERRO] Ponteiro de horas incorreto: {hourHand.CurrentPosition + 1}, esperava: {correctHour}");
        }
        
        if (minuteHand != null && minuteHand.CurrentPosition != correctMinute)
        {
            correctPositions = false;
            Debug.Log($"[CLOCK PUZZLE] [ERRO] Ponteiro de minutos incorreto: {minuteHand.CurrentPosition}, esperava: {correctMinute}");
        }
        
        if (secondHand != null && secondHand.CurrentPosition != correctSecond)
        {
            correctPositions = false;
            Debug.Log($"[CLOCK PUZZLE] [ERRO] Ponteiro de segundos incorreto: {secondHand.CurrentPosition}, esperava: {correctSecond}");
        }
        
        // Verificar direções de movimento
        if (handDirections[0] != correctHourDirection)
        {
            correctDirections = false;
            Debug.Log($"[CLOCK PUZZLE] [ERRO] Direção do ponteiro de horas incorreta: {handDirections[0]}, esperava: {correctHourDirection}");
        }
        
        if (handDirections[1] != correctMinuteDirection)
        {
            correctDirections = false;
            Debug.Log($"[CLOCK PUZZLE] [ERRO] Direção do ponteiro de minutos incorreta: {handDirections[1]}, esperava: {correctMinuteDirection}");
        }
        
        if (handDirections[2] != correctSecondDirection)
        {
            correctDirections = false;
            Debug.Log($"[CLOCK PUZZLE] [ERRO] Direção do ponteiro de segundos incorreta: {handDirections[2]}, esperava: {correctSecondDirection}");
        }
        
        isCorrect = correctPositions && correctDirections;
        
        if (isCorrect)
        {
            Debug.Log("[CLOCK PUZZLE] [SUCESSO] Puzzle resolvido corretamente!");
            PuzzleSolved();
        }
        else
        {
            Debug.Log("[CLOCK PUZZLE] [FALHA] Puzzle não resolvido.");
            Debug.Log($"[CLOCK PUZZLE] [DIAGNÓSTICO] Posições corretas: {correctPositions}, Direções corretas: {correctDirections}");
            PuzzleFailed();
        }
    }
    
    // Log do status atual dos ponteiros
    private void LogClockHandsStatus()
    {
        Debug.Log("[CLOCK PUZZLE] ----- STATUS ATUAL DOS PONTEIROS -----");
        
        if (hourHand != null)
        {
            string dirHoras = handDirections.Length > 0 ? handDirections[0].ToString() : "Não definida";
            Debug.Log($"[CLOCK PUZZLE] Horas: Posição = {hourHand.CurrentPosition + 1}/12, Direção = {dirHoras}, (Esperado: {correctHour}, {correctHourDirection})");
        }
        
        if (minuteHand != null)
        {
            string dirMinutos = handDirections.Length > 1 ? handDirections[1].ToString() : "Não definida";
            Debug.Log($"[CLOCK PUZZLE] Minutos: Posição = {minuteHand.CurrentPosition}/60, Direção = {dirMinutos}, (Esperado: {correctMinute}, {correctMinuteDirection})");
        }
        
        if (secondHand != null)
        {
            string dirSegundos = handDirections.Length > 2 ? handDirections[2].ToString() : "Não definida";
            Debug.Log($"[CLOCK PUZZLE] Segundos: Posição = {secondHand.CurrentPosition}/60, Direção = {dirSegundos}, (Esperado: {correctSecond}, {correctSecondDirection})");
        }
        
        Debug.Log("[CLOCK PUZZLE] ---------------------------------------");
    }
    
    // Chamado quando o puzzle é resolvido
    private void PuzzleSolved()
    {
        puzzleSolved = true;
        
        // Ativar feedback de sucesso
        if (successFeedback != null)
            successFeedback.SetActive(true);
        
        // Desativar UI de interação
        if (clockInteractionUI != null)
            clockInteractionUI.SetActive(false);
        
        // Tocar som de sucesso
        if (audioManager != null)
        {
            audioManager.PlayPuzzleSolved();
            audioManager.PlaySolvedMusic();
        }
        
        Debug.Log("[CLOCK PUZZLE] Puzzle resolvido! Mostrando recompensa em breve.");
        
        // Mostrar recompensa
        StartCoroutine(ShowRewardAfterDelay(2f));
        
        // Fechar a interação automaticamente após um pequeno delay
        FinishInteraction();
    }
    
    // Chamado quando o puzzle falha
    private void PuzzleFailed()
    {
        // Ativar feedback de falha
        if (failureFeedback != null)
        {
            failureFeedback.SetActive(true);
            StartCoroutine(HideFeedbackAfterDelay(failureFeedback, 2f));
        }
        
        Debug.Log("[CLOCK PUZZLE] Resetando ponteiros após falha...");
        
        // Resetar ponteiros
        ResetClockHands();
        
        // Tocar som de falha
        if (audioManager != null)
            audioManager.PlayPuzzleShuffle();
    }
    
    // Reseta os ponteiros para a posição inicial
    private void ResetClockHands()
    {
        foreach (var hand in hands)
        {
            if (hand != null)
                hand.ResetHand();
        }
        
        // Voltar para o primeiro ponteiro
        currentActiveHandIndex = 0;
        ActivateCurrentHand();
        
        // Também reiniciar as direções
        for (int i = 0; i < handDirections.Length; i++)
        {
            // Inicializar com um valor "nulo" para facilitar depuração
            handDirections[i] = Direction.Clockwise; // Valor padrão apenas para inicialização
        }
        
        Debug.Log("[CLOCK PUZZLE] Ponteiros resetados para posição inicial");
    }
    
    // Mostra a recompensa após um atraso
    private IEnumerator ShowRewardAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (rewardObject != null)
        {
            rewardObject.SetActive(true);
            Debug.Log("[CLOCK PUZZLE] Recompensa ativada!");
        }
        
        // Tocar som de aparecer recompensa
        if (audioManager != null)
            audioManager.PlayKeyAppear();
    }
    
    // Esconde um objeto de feedback após um atraso
    private IEnumerator HideFeedbackAfterDelay(GameObject feedback, float delay)
    {
        yield return new WaitForSeconds(delay);
        feedback.SetActive(false);
    }
    
    // Finaliza a interação e volta para a visão principal
    public void FinishInteraction()
    {
        Debug.Log("[CLOCK PUZZLE] Finalizando interação com o relógio");
        isClockActive = false;  // Marcar o relógio como inativo (NOVO!)
        
        // Mostrar novamente o ícone de interação se o jogador ainda estiver na área e o puzzle não estiver resolvido (NOVO!)
        if (interactionKeyIcon != null && playerInRange && !puzzleSolved)
            interactionKeyIcon.SetActive(true);
        
        // Reativar jogador e componentes relacionados
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("[CLOCK PUZZLE] Reativando controles do jogador");
            playerController.enabled = true;
            playerController.spriteRenderer.enabled = true;
            
            // Reativar InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = true;

            // Reativar percepção e lanterna
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(true);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(true);

            // Reativar sombras
            ShadowCaster2D shadowCaster = GetComponent<ShadowCaster2D>();
            if (shadowCaster != null) shadowCaster.enabled = true;
        }
        
        // Desativar UI de interação
        if (clockInteractionUI != null)
        {
            clockInteractionUI.SetActive(false);
            Debug.Log("[CLOCK PUZZLE] UI de interação do relógio desativada");
        }
        
        // Voltar para a câmera principal
        if (cameraController != null)
        {
            Debug.Log("[CLOCK PUZZLE] Retornando para a câmera principal");
            cameraController.ReturnToMainCamera();
        }
        
        // Desativar ponteiros e atualizar suas aparências
        foreach (var hand in hands)
        {
            if (hand != null)
            {
                hand.SetActive(false);
                
                // Garantir que a aparência volte ao normal
                SpriteRenderer handSprite = hand.GetComponent<SpriteRenderer>();
                if (handSprite != null)
                {
                    handSprite.color = Color.white;
                }
            }
        }
    }
    
    // Chamado quando o jogador entra na área de interação do relógio (NOVO!)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar se é o jogador que entrou no trigger
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[CLOCK PUZZLE] Jogador entrou na área de interação do relógio");
            
            // Mostrar o ícone de interação se o puzzle não foi resolvido e o relógio não está em uso
            if (!isClockActive && !puzzleSolved && interactionKeyIcon != null)
            {
                interactionKeyIcon.SetActive(true);
                Debug.Log("[CLOCK PUZZLE] Mostrou ícone de interação");
            }
        }
    }
    
    // Chamado quando o jogador sai da área de interação do relógio (NOVO!)
    private void OnTriggerExit2D(Collider2D other)
    {
        // Verificar se é o jogador que saiu do trigger
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("[CLOCK PUZZLE] Jogador saiu da área de interação do relógio");
            
            // Esconder o ícone de interação
            if (interactionKeyIcon != null)
            {
                interactionKeyIcon.SetActive(false);
                Debug.Log("[CLOCK PUZZLE] Escondeu ícone de interação");
            }
        }
    }
    
    // Método para implementação da interface IInteractable, se necessário (NOVO!)
    public void Interact()
    {
        if (!isClockActive && !puzzleSolved)
        {
            Debug.Log("[CLOCK PUZZLE] Interação iniciada através da interface IInteractable");
            StartClockInteraction();
        }
    }
}