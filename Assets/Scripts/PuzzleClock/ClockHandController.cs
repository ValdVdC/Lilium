
using UnityEngine;
using System;

public class ClockHandController : MonoBehaviour
{
    [Header("Configurações")]
    public int numberOfPositions = 12;     // Número de posições possíveis (12 para horas, 60 para minutos/segundos)
    public float rotationSpeed = 5f;       // Velocidade de rotação
    public bool isActive = false;          // Indica se este ponteiro está ativo para interação
    
    [Header("Configurações de Rotação Contínua")]
    public float initialDelay = 0.3f;      // Tempo inicial antes de começar a rotação contínua
    public float continuousRotationInterval = 0.1f; // Intervalo entre rotações quando a tecla está pressionada
    
    [Header("Estado")]
    [SerializeField]
    private int currentPosition = 0;       // Posição atual (0-11 para horas, 0-59 para minutos/segundos)
    private ClockPuzzleManager.Direction lastMoveDirection;  // Última direção de movimento
    
    // Variáveis para controle de rotação contínua
    private float keyHoldTimer = 0f;
    private float lastRotationTime = 0f;
    private bool isKeyHeld = false;
    private bool isRotatingClockwise = false;
    private bool isRotatingCounterClockwise = false;
    
    // Evento disparado quando o ponteiro é movido
    public event Action<ClockHandController, ClockPuzzleManager.Direction> OnHandMoved;
    
    // Propriedade para acessar a posição atual externamente
    public int CurrentPosition => currentPosition;
    
    // Identificar o tipo de ponteiro para os logs
    private string handType = "desconhecido";
    
    private void Start()
    {
        // Determinar o tipo de ponteiro para logs
        if (numberOfPositions == 12)
            handType = "Horas";
        else if (numberOfPositions == 60)
        {
            // Tenta identificar com base no nome do objeto
            string objName = gameObject.name.ToLower();
            if (objName.Contains("minute") || objName.Contains("minuto"))
                handType = "Minutos";
            else if (objName.Contains("second") || objName.Contains("segundo"))
                handType = "Segundos";
            else
                handType = "Minutos/Segundos";
        }
        
        Debug.Log($"[CLOCK HAND] Ponteiro de {handType} inicializado. Posições: {numberOfPositions}, Posição inicial: {currentPosition}");
    }
    
    private void Update()
    {
        if (!isActive)
            return;
        
        // Verificar input do jogador para pressionar pela primeira vez
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log($"[CLOCK HAND] Rotacionando ponteiro de {handType} no sentido anti-horário");
            RotateCounterClockwise();
            
            // Iniciar controle de rotação contínua
            isKeyHeld = true;
            isRotatingCounterClockwise = true;
            isRotatingClockwise = false;
            keyHoldTimer = 0f;
            lastRotationTime = Time.time;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log($"[CLOCK HAND] Rotacionando ponteiro de {handType} no sentido horário");
            RotateClockwise();
            
            // Iniciar controle de rotação contínua
            isKeyHeld = true;
            isRotatingClockwise = true;
            isRotatingCounterClockwise = false;
            keyHoldTimer = 0f;
            lastRotationTime = Time.time;
        }
        
        // Verificar se as teclas estão sendo mantidas pressionadas
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            if (isRotatingCounterClockwise)
            {
                HandleContinuousRotation(ClockPuzzleManager.Direction.CounterClockwise);
            }
        }
        else
        {
            isRotatingCounterClockwise = false;
        }
        
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            if (isRotatingClockwise)
            {
                HandleContinuousRotation(ClockPuzzleManager.Direction.Clockwise);
            }
        }
        else
        {
            isRotatingClockwise = false;
        }
        
        // Resetar o controle quando ambas as teclas forem soltas
        if ((!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.LeftArrow)) && 
            (!Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.RightArrow)))
        {
            isKeyHeld = false;
            keyHoldTimer = 0f;
        }
    }
    
    // Gerencia a rotação contínua quando uma tecla é mantida pressionada
    private void HandleContinuousRotation(ClockPuzzleManager.Direction direction)
    {
        keyHoldTimer += Time.deltaTime;
        
        // Esperar o tempo inicial antes de começar a rotação contínua
        if (keyHoldTimer >= initialDelay)
        {
            // Verificar se passou tempo suficiente desde a última rotação
            if (Time.time - lastRotationTime >= continuousRotationInterval)
            {
                // Executar a rotação apropriada
                if (direction == ClockPuzzleManager.Direction.Clockwise)
                {
                    RotateClockwise();
                }
                else
                {
                    RotateCounterClockwise();
                }
                
                // Atualizar o tempo da última rotação
                lastRotationTime = Time.time;
            }
        }
    }
    
    // Ativa ou desativa o ponteiro
    public void SetActive(bool active)
    {
        isActive = active;
        
        Debug.Log($"[CLOCK HAND] Ponteiro de {handType} " + (active ? "ATIVADO" : "DESATIVADO"));
        
        // Efeito visual apenas quando o ponteiro está ativo
        if (GetComponent<SpriteRenderer>() != null)
        {
            // Destaque o ponteiro ativo com uma cor mais brilhante, mas apenas quando o puzzle estiver ativo
            bool isInteracting = FindFirstObjectByType<ClockPuzzleManager>()?.clockInteractionUI?.activeSelf ?? false;
            GetComponent<SpriteRenderer>().color = (active && isInteracting) ? 
                new Color(1.0f, 1.0f, 0.7f) : Color.white;
        }
    }
    
    // Configura o ponteiro
    public void SetupHand(int positions)
    {
        numberOfPositions = positions;
        
        // Atualizar o tipo de ponteiro
        if (numberOfPositions == 12)
            handType = "Horas";
        else if (numberOfPositions == 60)
        {
            // Tenta identificar com base no nome do objeto
            string objName = gameObject.name.ToLower();
            if (objName.Contains("minute") || objName.Contains("minuto"))
                handType = "Minutos";
            else if (objName.Contains("second") || objName.Contains("segundo"))
                handType = "Segundos";
            else
                handType = "Minutos/Segundos";
        }
        
        Debug.Log($"[CLOCK HAND] Ponteiro configurado: {handType} com {numberOfPositions} posições");
    }
    
    // Rotaciona o ponteiro no sentido horário
    public void RotateClockwise()
    {
        int oldPosition = currentPosition;
        
        // Avançar para a próxima posição
        currentPosition = (currentPosition + 1) % numberOfPositions;
        
        // Calcular o ângulo de rotação
        float targetAngle = 360f - (currentPosition * (360f / numberOfPositions));
        
        // Aplicar rotação
        transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        
        // Registrar direção do movimento
        lastMoveDirection = ClockPuzzleManager.Direction.Clockwise;
        
        // Log do movimento
        Debug.Log($"[CLOCK HAND] Ponteiro de {handType} rotacionado HORÁRIO: {oldPosition + (handType == "Horas" ? 1 : 0)} -> {currentPosition + (handType == "Horas" ? 1 : 0)}");
        
        // Disparar evento
        OnHandMoved?.Invoke(this, lastMoveDirection);
    }
    
    // Rotaciona o ponteiro no sentido anti-horário
    public void RotateCounterClockwise()
    {
        int oldPosition = currentPosition;
        
        // Voltar para a posição anterior
        currentPosition = (currentPosition - 1 + numberOfPositions) % numberOfPositions;
        
        // Calcular o ângulo de rotação
        float targetAngle = 360f - (currentPosition * (360f / numberOfPositions));
        
        // Aplicar rotação
        transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        
        // Registrar direção do movimento
        lastMoveDirection = ClockPuzzleManager.Direction.CounterClockwise;
        
        // Log do movimento
        Debug.Log($"[CLOCK HAND] Ponteiro de {handType} rotacionado ANTI-HORÁRIO: {oldPosition + (handType == "Horas" ? 1 : 0)} -> {currentPosition + (handType == "Horas" ? 1 : 0)}");
        
        // Disparar evento
        OnHandMoved?.Invoke(this, lastMoveDirection);
    }
    
    // Define a posição do ponteiro diretamente
    public void SetPosition(int position)
    {
        int oldPosition = currentPosition;
        
        // Garantir que a posição está dentro dos limites
        position = Mathf.Clamp(position, 0, numberOfPositions - 1);
        
        // Atualizar posição
        currentPosition = position;
        
        // Calcular o ângulo de rotação
        float targetAngle = 360f - (currentPosition * (360f / numberOfPositions));
        
        // Aplicar rotação
        transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        
        // Log da mudança de posição
        Debug.Log($"[CLOCK HAND] Ponteiro de {handType} POSICIONADO manualmente: {oldPosition + (handType == "Horas" ? 1 : 0)} -> {currentPosition + (handType == "Horas" ? 1 : 0)}");
    }

    // Reseta o ponteiro para a posição inicial (12 horas)
    public void ResetHand()
    {
        int oldPosition = currentPosition;
        SetPosition(0);
        Debug.Log($"[CLOCK HAND] Ponteiro de {handType} RESETADO para posição inicial: {oldPosition + (handType == "Horas" ? 1 : 0)} -> {1}");
    }
    
    // Retorna a última direção de movimento
    public ClockPuzzleManager.Direction GetLastMoveDirection()
    {
        return lastMoveDirection;
    }
}