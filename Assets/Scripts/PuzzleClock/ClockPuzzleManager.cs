using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering.Universal;
using System;

public class ClockPuzzleManager : MonoBehaviour, ISaveable
{
    [Serializable]
    public class SaveData
    {
        // Estado principal do puzzle
        public bool bookHasBeenRead;
        public bool puzzleSolved;
        public bool keyCollected;
        
        // Estado dos ponteiros
        public int[] savedPositions = new int[3];
        public Direction[] savedDirections = new Direction[3];  
        public bool[] handConfirmed = new bool[3];
        
        // Ponteiro atual selecionado
        public int currentActiveHandIndex;
    }

    public object GetSaveData()
    {
        SaveData data = new SaveData
        {
            // Estado principal
            bookHasBeenRead = this.bookHasBeenRead,
            puzzleSolved = this.puzzleSolved,
            keyCollected = this.keyCollected,
            
            // Estado dos ponteiros
            savedPositions = this.savedPositions, // Cria uma cópia
            handConfirmed = this.handConfirmed,   // Cria uma cópia
            currentActiveHandIndex = this.currentActiveHandIndex
        };
        
        // Como Direction é um enum, precisamos converter para uma forma serializável
        data.savedDirections = new Direction[3];
        for (int i = 0; i < 3; i++)
        {
            data.savedDirections[i] = this.savedDirections[i];
        }
        
        return data;
    }

    public void LoadFromSaveData(object saveData)
    {
        if (saveData is SaveData data)
        {
            // Estado principal
            this.bookHasBeenRead = data.bookHasBeenRead;
            this.puzzleSolved = data.puzzleSolved;
            this.keyCollected = data.keyCollected;
            
            // Estado dos ponteiros
            for (int i = 0; i < 3; i++)
            {
                if (i < data.savedPositions.Length)
                    this.savedPositions[i] = data.savedPositions[i];
                
                if (i < data.savedDirections.Length)
                    this.savedDirections[i] = (Direction)data.savedDirections[i];
                
                if (i < data.handConfirmed.Length)
                    this.handConfirmed[i] = data.handConfirmed[i];
            }
            
            this.currentActiveHandIndex = data.currentActiveHandIndex;
            
            // Desativar ponteiros quando o puzzle estiver resolvido
            if (this.puzzleSolved)
            {
                foreach (var hand in hands)
                {
                    if (hand != null)
                        hand.SetActive(false);
                }
                
                // Mostrar ou esconder a recompensa dependendo se foi coletada
                if (rewardObject != null)
                    rewardObject.SetActive(!this.keyCollected);
                    
                // Iniciar animação apenas depois que todos os outros estados estiverem configurados
                StartClockAnimation();
            }
        }
    }

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
    public SpriteRenderer clockSpriteRenderer;    // Referência ao SpriteRenderer do relógio
    public Sprite[] clockAnimationSprites;        // Array de sprites para a animação
    public float animationSpeed = 0.2f;           // Velocidade da animação (segundos por frame)
    private Coroutine clockAnimationCoroutine;
    
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

    [Header("Estado da Chave")]
    public bool keyCollected = false;  // Indica se a chave já foi coletada

    [Header("Key UI")]
    public ClockKeyUIController keyUIController;  // Controlador da UI da chave

    [Header("Key UI Interaction")]
    public RectTransform keyHighlight;        // Highlight retangular para a chave
    public GameObject keyClickIndicator;      // Indicador de clique quando hover na chave
    public Canvas puzzleCanvas;               // Canvas que contém os elementos UI do puzzle
    public PuzzleItemType keyItemType;        // Tipo do item da chave para o inventário
    
    private int currentActiveHandIndex = 0;     // 0 = horas, 1 = minutos, 2 = segundos
    private ClockHandController[] hands;       // Array com todos os ponteiros
    private Direction[] handDirections;        // Direções usadas para cada ponteiro
    private bool playerInRange = false;        // Indica se o jogador está na área de interação
    private bool isClockActive = false;        // Indica se o relógio está em interação 

    private bool isKeyInteractable = false;   // Indica se a chave pode ser interagida
    private bool isHoveringOverKey = false;   // Indica se o mouse está sobre a chave

    private bool[] handConfirmed;         // Array para controlar quais ponteiros já foram confirmados
    private int[] savedPositions;         // Array para salvar as posições dos ponteiros
    private Direction[] savedDirections;  // Array para salvar as direções dos ponteiros

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

        handConfirmed = new bool[3] { false, false, false };
        savedPositions = new int[3] { 0, 0, 0 };
        savedDirections = new Direction[3] { Direction.Clockwise, Direction.Clockwise, Direction.Clockwise };
    }

    private void Update()
    {
        // Verificar saída da interação com prioridade
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Se estiver em qualquer tipo de interação, finaliza
            if (isClockActive || isKeyInteractable)
            {
                Debug.Log("[CLOCK PUZZLE] Saindo da interação");
                FinishInteraction();
                return;
            }
            // Se não estiver em interação e estiver na área, inicia
            else if (playerInRange)
            {
                Debug.Log("[CLOCK PUZZLE] Jogador pressionou E para interagir");
                if (puzzleSolved && !keyCollected)
                {
                    StartKeyInteraction();
                }
                else if (!puzzleSolved)
                {
                    StartClockInteraction();
                }
                return;
            }
        }

        // Verificar confirmação do ponteiro atual (somente se a UI do relógio estiver ativa)
        if (clockInteractionUI != null && clockInteractionUI.activeSelf)
        {
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
        
        // Atualizar UI da chave se estiver ativa
        if (isKeyInteractable && !keyCollected)
        {
            UpdateKeyInteractionUI();
            
            if (Input.GetMouseButtonDown(0) && isHoveringOverKey)
            {
                Debug.Log("[CLOCK PUZZLE] Clique detectado quando hover na chave!");
                CollectKey();
            }
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
        // Se o puzzle foi resolvido e a chave não foi coletada, vamos direto para a interação da chave
        if (puzzleSolved && !keyCollected)
        {
            Debug.Log("[CLOCK PUZZLE] Puzzle já resolvido. Mostrando interface da chave.");
            StartKeyInteraction();
            return;
        }

        // Se o puzzle não foi resolvido, verificamos o requisito do livro
        if (!puzzleSolved && requiresBookToBeRead && !bookHasBeenRead)
        {
            Debug.Log("[CLOCK PUZZLE] O jogador precisa ler o livro primeiro!");
            return;
        }

        Debug.Log("[CLOCK PUZZLE] Iniciando interação com o relógio");
        isClockActive = true;
        
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
        
        for (int i = 0; i < handConfirmed.Length; i++)
        {
            if (!handConfirmed[i])
            {
                currentActiveHandIndex = i;
                break;
            }
        }

        // Restaurar as posições salvas
        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i] != null)
            {
                hands[i].SetPosition(savedPositions[i]);
                handDirections[i] = savedDirections[i];
            }
        }

        // Ativar o ponteiro atual
        ActivateCurrentHand();
        
        // Tocar música de puzzle
        if (audioManager != null)
            audioManager.PlayPuzzleMusic();
            
        // Log status inicial dos ponteiros
        LogClockHandsStatus();
    }

    public void StartKeyInteraction()
    {
        if (keyCollected)
        {
            Debug.Log("[CLOCK PUZZLE] Chave já foi coletada!");
            return;
        }

        Debug.Log("[CLOCK PUZZLE] Iniciando interação com a chave");
        isClockActive = true;
        isKeyInteractable = false; // Começa como false até a câmera terminar
        
        // Esconder o ícone de interação
        if (interactionKeyIcon != null)
            interactionKeyIcon.SetActive(false);
        
        // Desativar o jogador e componentes relacionados
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("[CLOCK PUZZLE] Desativando controles do jogador para interação com a chave");
            playerController.enabled = false;
            playerController.spriteRenderer.enabled = false;
            
            // Desativar InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = false;

            // Desativar percepção e lanterna
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) 
                perception.gameObject.SetActive(false);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) 
                flashLight.gameObject.SetActive(false);
                        
            // Desativar sombras
            ShadowCaster2D shadowCaster = GetComponent<ShadowCaster2D>();
            if (shadowCaster != null) 
                shadowCaster.enabled = false;
        }

        // IMPORTANTE: Modificação aqui - Primeiro ativar a câmera do puzzle
        if (cameraController != null)
        {
            Debug.Log("[CLOCK PUZZLE] Ativando câmera do puzzle");
            
            // Garantir que a câmera principal seja desativada
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                mainCamera.gameObject.SetActive(false);

            // Ativar câmera do puzzle usando a posição do relógio
            cameraController.ActivateClockPuzzleCamera(clockPosition, puzzleCameraSize);
            
            // Depois ajustar para a posição da chave
            StartCoroutine(AdjustCameraForKey());
        }
        
        // A UI será ativada depois que a câmera terminar a transição
        if (keyUIController != null)
        {
            keyUIController.gameObject.SetActive(false);
        }
    }

    private IEnumerator DelaySetupKeyInteraction()
    {
        yield return null; // Aguardar um frame
        SetupKeyInteractionUI();
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
        // Marcar o ponteiro atual como confirmado
        handConfirmed[currentActiveHandIndex] = true;
        
        // Salvar a posição e direção final deste ponteiro
        savedPositions[currentActiveHandIndex] = hands[currentActiveHandIndex].CurrentPosition;
        savedDirections[currentActiveHandIndex] = hands[currentActiveHandIndex].GetLastMoveDirection();

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
        
        // Desativar UI de interação do relógio
        if (clockInteractionUI != null)
            clockInteractionUI.SetActive(false);

        // Desativar ponteiros
        foreach (var hand in hands)
        {
            if (hand != null)
                hand.SetActive(false);
        }

        // Tocar som de sucesso
        if (audioManager != null)
        {
            audioManager.PlayPuzzleSolved();
            audioManager.PlaySolvedMusic();
        }
        
        Debug.Log("[CLOCK PUZZLE] Puzzle resolvido! Iniciando sequência de recompensa.");
        
        // Iniciar a animação do relógio
        StartClockAnimation();
        
        // Primeiro ativar a recompensa
        if (rewardObject != null)
        {
            rewardObject.SetActive(true);
            Debug.Log("[CLOCK PUZZLE] Recompensa ativada!");
            
            // Verificar se a chave tem um collider
            Collider2D keyCollider = rewardObject.GetComponent<Collider2D>();
            if (keyCollider == null)
            {
                BoxCollider2D boxCollider = rewardObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                Debug.Log("[CLOCK PUZZLE] Adicionado BoxCollider2D à chave para interação");
            }
        }
        
        // Depois ajustar a câmera e configurar a UI
        StartCoroutine(AdjustCameraForKey());
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
        ResetPuzzleProgress();
        
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
            
            // Verificar se a chave tem um collider
            Collider2D keyCollider = rewardObject.GetComponent<Collider2D>();
            if (keyCollider == null)
            {
                // Adicionar um BoxCollider2D à chave
                BoxCollider2D boxCollider = rewardObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true; // Para não ter colisão física
                Debug.Log("[CLOCK PUZZLE] Adicionado BoxCollider2D à chave para interação");
            }
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
        // Salvar o estado atual antes de finalizar
        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i] != null)
            {
                savedPositions[i] = hands[i].CurrentPosition;
                savedDirections[i] = hands[i].GetLastMoveDirection();
            }
        }
        Debug.Log("[CLOCK PUZZLE] Finalizando interação com o relógio");
        isClockActive = false;
        isKeyInteractable = false;
        // Mostrar novamente o ícone de interação se o jogador ainda estiver na área
        if (interactionKeyIcon != null && playerInRange)
        {
            // Mostrar ícone se o puzzle não foi resolvido OU se foi resolvido mas a chave não foi coletada
            if (!puzzleSolved || (puzzleSolved && !keyCollected))
            {
                interactionKeyIcon.SetActive(true);
                Debug.Log("[CLOCK PUZZLE] Mostrou ícone de interação após finalizar interação");
            }
        }
        
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
        
        // Esconder a UI da chave se estiver ativa
        if (keyUIController != null)
        {
            keyUIController.HideKeyInteractionUI();
            keyUIController.gameObject.SetActive(false); // Garantir que o objeto inteiro seja desativado
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

    public void ResetPuzzleProgress()
    {
        for (int i = 0; i < handConfirmed.Length; i++)
        {
            handConfirmed[i] = false;
            savedPositions[i] = 0;
            savedDirections[i] = Direction.Clockwise;
        }
        currentActiveHandIndex = 0;
        
        // Resetar os ponteiros para a posição inicial
        ResetClockHands();
    }
    
    // Chamado quando o jogador entra na área de interação do relógio (NOVO!)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[CLOCK PUZZLE] Jogador entrou na área de interação do relógio");
            
            // Mostrar o ícone de interação se o puzzle não foi resolvido OU
            // se foi resolvido mas a chave ainda não foi coletada
            if (!isClockActive && (!puzzleSolved || (puzzleSolved && !keyCollected)))
            {
                if (interactionKeyIcon != null)
                {
                    interactionKeyIcon.SetActive(true);
                    Debug.Log("[CLOCK PUZZLE] Mostrou ícone de interação");
                }
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
    
    // Método para implementação da interface IInteractable
    public void Interact()
    {
        if (!isClockActive)
        {
            Debug.Log("[CLOCK PUZZLE] Interação iniciada através da interface IInteractable");
            
            // Se o puzzle foi resolvido mas a chave não foi coletada
            if (puzzleSolved && !keyCollected)
            {
                StartKeyInteraction();
            }
            else if (!puzzleSolved)
            {
                StartClockInteraction();
            }
        }
    }

    private void StartClockAnimation()
    {
        // Garantir que qualquer animação existente seja interrompida primeiro
        if (clockAnimationCoroutine != null)
        {
            StopCoroutine(clockAnimationCoroutine);
            clockAnimationCoroutine = null;
        }
        
        // Iniciar nova animação
        clockAnimationCoroutine = StartCoroutine(AnimateClockAfterSolved());
    }

    private IEnumerator AnimateClockAfterSolved()
    {
        if (clockSpriteRenderer == null || clockAnimationSprites == null || clockAnimationSprites.Length < 2)
        {
            Debug.LogWarning("[CLOCK PUZZLE] Componentes de animação não configurados corretamente!");
            yield break;
        }
        
        Debug.Log("[CLOCK PUZZLE] Iniciando animação do relógio");
        
        // Garantir que o sprite inicial seja configurado corretamente
        int currentSpriteIndex = 0;
        clockSpriteRenderer.sprite = clockAnimationSprites[currentSpriteIndex];
        
        yield return new WaitForSeconds(0.1f); // Pequena pausa para sincronização
        
        bool goingForward = true;
        
        while (true) // Loop infinito para animação contínua
        {
            // Atualizar o sprite atual
            clockSpriteRenderer.sprite = clockAnimationSprites[currentSpriteIndex];
            
            // Esperar pelo tempo determinado
            yield return new WaitForSeconds(animationSpeed);
            
            // Lógica para ir e voltar na sequência de sprites (ping-pong)
            if (goingForward)
            {
                currentSpriteIndex++;
                if (currentSpriteIndex >= clockAnimationSprites.Length - 1)
                {
                    goingForward = false;
                }
            }
            else
            {
                currentSpriteIndex--;
                if (currentSpriteIndex <= 0)
                {
                    goingForward = true;
                }
            }
        }
    }

    private IEnumerator AdjustCameraForKey()
    {
        yield return new WaitForSeconds(0f);
        
        if (cameraController != null && rewardObject != null)
        {
            Camera currentCamera = cameraController.GetCurrentCamera();
            if (currentCamera == null)
            {
                Debug.LogError("[CLOCK PUZZLE] Camera atual é nula!");
                yield break;
            }

            Vector3 originalPosition = currentCamera.transform.position;
            float originalSize = currentCamera.orthographicSize;
            
            Vector3 targetPosition = new Vector3(
                rewardObject.transform.position.x,
                rewardObject.transform.position.y - 0.5f,
                originalPosition.z
            );
            
            float targetSize = puzzleCameraSize * 0.8f;
            float duration = 0.5f;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                float smoothT = Mathf.SmoothStep(0, 1, t);
                
                currentCamera.transform.position = Vector3.Lerp(originalPosition, targetPosition, smoothT);
                currentCamera.orthographicSize = Mathf.Lerp(originalSize, targetSize, smoothT);
                
                yield return null;
            }

            // Aguardar um momento antes de ativar a UI
            yield return new WaitForSeconds(0.2f);

            // Configurar a UI e ativar interação
            if (keyUIController != null)
            {
                keyUIController.gameObject.SetActive(true);
                SetupKeyInteractionUI();
            }

            isKeyInteractable = true;
            Debug.Log("[CLOCK PUZZLE] Câmera ajustada e UI da chave configurada");
            
            // Debug adicional para verificar o estado
            Debug.Log($"[CLOCK PUZZLE] Estado final: isKeyInteractable={isKeyInteractable}, keyUIController.active={keyUIController?.gameObject.activeSelf}");
        }
    }
    // Configura a UI para interação com a chave
    private void SetupKeyInteractionUI()
    {
        if (rewardObject == null || keyUIController == null)
        {
            Debug.LogError("[CLOCK PUZZLE] rewardObject ou keyUIController é nulo!");
            return;
        }
        
        Debug.Log("[CLOCK PUZZLE] Configurando UI de interação da chave");
        
        // Garantir que o keyUIController esteja ativo para interação
        keyUIController.gameObject.SetActive(true);
        
        // Ativar a UI de interação da chave
        keyUIController.ShowKeyInteractionUI();
        
        // Posicionar o highlight ao redor da chave
        PositionKeyHighlight();
    }

    // Posiciona o highlight retangular ao redor da chave
    private void PositionKeyHighlight()
    {
        if (keyUIController == null)
        {
            Debug.LogError("[CLOCK PUZZLE] keyUIController é nulo!");
            return;
        }
        
        if (keyUIController.keyHighlight == null)
        {
            Debug.LogError("[CLOCK PUZZLE] keyHighlight é nulo no keyUIController!");
            return;
        }
        
        if (rewardObject == null)
        {
            Debug.LogError("[CLOCK PUZZLE] rewardObject é nulo!");
            return;
        }
        
        // Use a referência direta ao puzzleCanvas em vez de procurar pelo Canvas pai
        if (puzzleCanvas == null)
        {
            Debug.LogError("[CLOCK PUZZLE] puzzleCanvas não foi atribuído no Inspector!");
            return;
        }
        
        RectTransform canvasRect = puzzleCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            Debug.LogError("[CLOCK PUZZLE] puzzleCanvas não tem RectTransform!");
            return;
        }
        
        // Obter a câmera atual
        Camera currentCamera = cameraController.GetCurrentCamera();
        if (currentCamera == null)
        {
            Debug.LogError("[CLOCK PUZZLE] Câmera atual é nula!");
            return;
        }
        
        // Pegar o renderer da chave para obter seus limites
        Renderer keyRenderer = rewardObject.GetComponent<Renderer>();
        if (keyRenderer == null)
        {
            Debug.LogError("[CLOCK PUZZLE] Chave não tem um componente Renderer!");
            return;
        }
        
        // Obter os limites da chave
        Bounds keyBounds = keyRenderer.bounds;
        
        // Expandir os limites um pouco para o highlight ser maior que a chave
        keyBounds.Expand(0.1f);
        
        // Converter as posições dos cantos da chave para a tela
        Vector3 minScreenPoint = currentCamera.WorldToScreenPoint(keyBounds.min);
        Vector3 maxScreenPoint = currentCamera.WorldToScreenPoint(keyBounds.max);
        
        // Verificar se a conversão para coordenadas locais está funcionando
        Vector2 minLocalPoint, maxLocalPoint;
        
        // Usar a câmera correta dependendo do modo do canvas
        Camera canvasCamera = puzzleCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : currentCamera;
        
        bool minResult = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            minScreenPoint,
            canvasCamera,
            out minLocalPoint);
        
        bool maxResult = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            maxScreenPoint,
            canvasCamera,
            out maxLocalPoint);
        
        if (!minResult || !maxResult)
        {
            Debug.LogError("[CLOCK PUZZLE] Falha ao converter pontos da tela para pontos locais!");
            return;
        }
        
        // Configurar a posição e tamanho do highlight
        keyUIController.keyHighlight.anchoredPosition = (minLocalPoint + maxLocalPoint) / 2;
        keyUIController.keyHighlight.sizeDelta = new Vector2(
            Mathf.Abs(maxLocalPoint.x - minLocalPoint.x),
            Mathf.Abs(maxLocalPoint.y - minLocalPoint.y)
        );
        
        // Adicionar um pequeno padding ao retângulo
        float padding = 20f; // Aumentado de 15f para 20f
        keyUIController.keyHighlight.sizeDelta += new Vector2(padding * 2, padding * 2);
    }

    private void CheckMouseHoverOnKey()
    {
        // Somente verificar se a chave está ativa e interagível
        if (rewardObject == null || !rewardObject.activeSelf || !isKeyInteractable)
        {
            isHoveringOverKey = false;
            if (keyUIController != null)
                keyUIController.ShowClickIndicator(false, Vector2.zero);
            return;
        }
        
        Camera currentCamera = cameraController.GetCurrentCamera();
        if (currentCamera == null)
            return;
        
        Vector2 mouseWorldPos = currentCamera.ScreenToWorldPoint(Input.mousePosition);
        
        // Usar um círculo um pouco maior para a detecção
        float detectionRadius = 0.3f; // Aumentado para melhor detecção
        RaycastHit2D hit = Physics2D.CircleCast(mouseWorldPos, detectionRadius, Vector2.zero, 0f);
        
        bool wasHovering = isHoveringOverKey;
        isHoveringOverKey = (hit.collider != null && hit.collider.gameObject == rewardObject);
        
        // Se o estado do hover mudou
        if (wasHovering != isHoveringOverKey)
        {
            Debug.Log($"[CLOCK PUZZLE] Estado do hover mudou: {isHoveringOverKey}");
            
            // Atualizar o highlight
            if (keyUIController != null)
            {
                keyUIController.SetHighlightHoverState(isHoveringOverKey);
                UpdateClickIndicator(isHoveringOverKey, currentCamera);
            }
        }
    }

    private void UpdateClickIndicator(bool show, Camera camera)
    {
        if (keyUIController != null && show && rewardObject != null)
        {
            // Posicionar o indicador no centro da chave
            Vector3 keyWorldPos = rewardObject.transform.position;
            Vector2 screenPos = camera.WorldToScreenPoint(keyWorldPos);
            
            // Converter para coordenadas locais do canvas
            RectTransform canvasRect = puzzleCanvas.transform as RectTransform;
            
            // Usar a câmera correta dependendo do modo do canvas
            Camera canvasCamera = puzzleCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera;
            
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvasCamera,
                out localPoint))
            {
                keyUIController.ShowClickIndicator(true, localPoint);
                Debug.Log("[CLOCK PUZZLE] Indicador de clique mostrado em: " + localPoint);
            }
        }
        else if (keyUIController != null)
        {
            keyUIController.ShowClickIndicator(false, Vector2.zero);
        }
    }

    // Coleta a chave e adiciona ao inventário
    private void CollectKey()
    {
        // Verificar se a chave já foi coletada
        if (keyCollected || rewardObject == null)
            return;
        
        Debug.Log("[CLOCK PUZZLE] Tentando coletar a chave...");
        
        // Referência ao inventário
        InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
        
        // Tentar adicionar a chave ao inventário
        bool added = inventory != null && inventory.AddItem(keyItemType);
        
        if (added)
        {
            Debug.Log("[CLOCK PUZZLE] Chave coletada com sucesso!");
            keyCollected = true;
            
            // Desativar o objeto da chave
            rewardObject.SetActive(false);
            
            // Desativar UI de interação da chave
            if (keyUIController != null)
                keyUIController.HideKeyInteractionUI();
            
            // Tocar som de coleta
            if (audioManager != null)
                audioManager.PlayKeyCollect();
            
            // Finalizar a interação com o puzzle
            StartCoroutine(DelayedFinishInteraction());
        }
        else
        {
            Debug.LogError("[CLOCK PUZZLE] Falha ao adicionar a chave ao inventário!");
        }
    }
    private IEnumerator DelayedFinishInteraction()
    {
        // Pequeno delay para que o som de coleta seja reproduzido completamente
        yield return new WaitForSeconds(0f);
        FinishInteraction();
    }
    private void UpdateKeyInteractionUI()
    {
        if (puzzleSolved && isKeyInteractable && !keyCollected)
        {
            // Reposicionar o highlight para caso a câmera tenha se movido
            PositionKeyHighlight();
            
            // Verificar o hover do mouse sobre a chave
            CheckMouseHoverOnKey();
            
            // Debug para verificar se está atualizando
            if (isHoveringOverKey)
            {
                Debug.Log("[CLOCK PUZZLE] Mouse sobre a chave - UI sendo atualizada");
            }
        }
    }
}