using UnityEngine;
using Pathfinding;
using System.Collections;
using System;

public class EnemyAI : MonoBehaviour, ISaveable
{
    [Serializable]
    public class SaveData
    {
        // Posição atual do inimigo
        public Vector3 position;
        
        // Estado atual do inimigo
        public EnemyState currentState;
        
        // Direção atual que o inimigo está enfrentando
        public FacingDirection currentDirection;
        
        // Tempo desde o último ataque, importante para o cooldown
        public float lastAttackTime;
        
        // Flag se está se movendo
        public bool isMoving;
        
        // Tempo fora do alcance - importante para o comportamento de retorno
        public float timeOutOfRange;
        
        // Flag se está retornando à posição inicial
        public bool isReturning;
    }

    public object GetSaveData()
    {
        SaveData data = new SaveData
        {
            position = transform.position,
            currentState = currentState,
            currentDirection = currentDirection,
            lastAttackTime = lastAttackTime,
            isMoving = isMoving,
            timeOutOfRange = timeOutOfRange,
            isReturning = isReturning
        };
        
        return data;
    }

    public void LoadFromSaveData(object saveData)
    {
        if (saveData is SaveData data)
        {
            transform.position = data.position;
            currentState = data.currentState;
            currentDirection = data.currentDirection;
            lastAttackTime = data.lastAttackTime;
            isMoving = data.isMoving;
            timeOutOfRange = data.timeOutOfRange;
            isReturning = data.isReturning;
            // Se estiver salvando o caminho (path), seria necessário recalculá-lo aqui
            if (isMoving || isReturning)
            {
                Vector3 target = isReturning ? startPosition : player.position;
                seeker.StartPath(transform.position, target, OnPathComplete);
            }
        }
    }

    [Header("Referências e Movimento")]
    public Transform player;
    public float moveSpeed = 2f;
    public float nextWaypointDistance = 0.7f;
    public float smoothTime = 0.3f;

    [Header("Comportamento")]
    public float chaseRange = 5f;
    public float approachRange = 2.5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    
    [Header("Área de Atuação")]
    public bool useOperationalArea = true;
    public LayerMask operationalAreaLayer;
    public string operationalAreaTag = "EnemyArea";
    // Nova propriedade - ID de área específico para este inimigo
    public string assignedAreaID = "default";
    public bool useAreaID = false; // Se true, verifica o ID da área em vez de apenas a tag

    [Header("Retorno à Posição Inicial")]
    public float returnDelay = 5f;
    public bool returnToStartPosition = true;
    public FacingDirection initialDirection = FacingDirection.Down;
    
    // Tolerância para considerar que chegou à posição inicial
    public float returnPositionTolerance = 0.2f;
    // Tempo mínimo parado para confirmar que chegou à posição inicial
    public float stationaryConfirmTime = 0.5f;
    
    public enum FacingDirection { Down, Up, Left, Right }
    public enum EnemyState { Idle, Moving, Approaching, Attacking, Returning }

    [HideInInspector] public FacingDirection currentDirection;
    [HideInInspector] public bool isMoving;
    [HideInInspector] public EnemyState currentState;
    [HideInInspector] public bool attackAnimationComplete = true;

    private Rigidbody2D rb;
    private float lastAttackTime;
    private Vector3 startPosition;
    private bool isPlayerInRange = false;
    private float timeOutOfRange = 0f;
    private bool isReturning = false;
    
    // Componentes de pathfinding
    private Seeker seeker;
    private Path path;
    private int currentWaypoint = 0;
    
    private float pathUpdateTimer = 0f;
    public float pathUpdateInterval = 1.0f;
    
    // Variáveis para suavização de movimento
    private Vector2 currentVelocity;
    private Vector2 lastDirection = Vector2.zero;

    // Variáveis para detecção de "stuck"
    private Vector2 lastPosition;
    private float stuckTime = 0f;
    private bool isStuck = false;
    private float stuckThreshold = 0.5f;
    private float stuckCheckDistance = 0.01f;

    // Variáveis para força de repulsão
    public float obstacleDetectionRadius = 0.7f;
    public float obstacleAvoidanceStrength = 0.2f;
    
    // Variáveis para os aprimoramentos
    public float nodeProximityMultiplier = 1.2f;
    public float edgeFollowingStrength = 0.3f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        seeker = GetComponent<Seeker>();
        currentState = EnemyState.Idle;
        lastPosition = rb.position;
        
        // Salva a posição inicial
        startPosition = transform.position;
        
        // Define a direção inicial configurada no Inspector
        currentDirection = initialDirection;
    }

    void Update()
    {
    if (player == null) return;

    // Se estiver no meio de um ataque, não faz nada até terminar
    if (currentState == EnemyState.Attacking && !attackAnimationComplete)
    {
        return;
    }

    float distanceToPlayer = Vector2.Distance(transform.position, player.position);
    bool playerInOperationalArea = IsPlayerInOperationalArea();
    bool canChasePlayer = !useOperationalArea || playerInOperationalArea;

    // Verifica se o player está em range e se pode persegui-lo
    bool newIsPlayerInRange = distanceToPlayer < chaseRange && canChasePlayer;
    
    // Se o estado de detecção do jogador mudou
    if (isPlayerInRange != newIsPlayerInRange)
    {
        isPlayerInRange = newIsPlayerInRange;
        
        // Se o jogador saiu do alcance, pára o movimento
        if (!isPlayerInRange && currentState != EnemyState.Returning)
        {
            StopMoving();
        }
    }

        // Verificação de "stuck" quando estiver se movendo
        if (currentState == EnemyState.Moving || currentState == EnemyState.Approaching || currentState == EnemyState.Returning)
        {
            if (Vector2.Distance(rb.position, lastPosition) < stuckCheckDistance)
            {
                stuckTime += Time.deltaTime;
                if (stuckTime > stuckThreshold && !isStuck)
                {
                    isStuck = true;
                    Debug.Log("Inimigo preso! Recalculando caminho...");
                    
                    if (currentState == EnemyState.Returning)
                    {
                        seeker.StartPath(transform.position, startPosition, OnPathComplete);
                    }
                    else
                    {
                        // Adiciona um pequeno offset aleatório à posição do jogador para tentar um caminho diferente
                        Vector2 randomOffset = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized * 0.5f;
                        seeker.StartPath(transform.position, player.position + (Vector3)randomOffset, OnPathComplete);
                    }
                    
                    // Força um salto para o próximo waypoint se possível
                    if (path != null && currentWaypoint < path.vectorPath.Count - 1)
                    {
                        currentWaypoint++;
                    }
                }
            }
            else
            {
                stuckTime = 0f;
                isStuck = false;
            }
            
            lastPosition = rb.position;
        }

        // Lógica para retornar à posição inicial
        if (!isPlayerInRange && returnToStartPosition)
        {
            if (currentState != EnemyState.Returning)
            {
                timeOutOfRange += Time.deltaTime;
                
                if (timeOutOfRange >= returnDelay)
                {
                    StartReturnToStart();
                }
            }
            else if (currentState == EnemyState.Returning)
            {
                // Se já chegou perto o suficiente da posição inicial
                float distanceToStart = Vector3.Distance(transform.position, startPosition);
                
                if (distanceToStart < returnPositionTolerance)
                {
                    CompleteReturn();
                }
                else
                {
                    // Não chegou ainda, continua seguindo o caminho
                    if (path != null)
                    {
                        FollowPath(0.8f); // Velocidade reduzida para maior precisão
                    }
                    else if (seeker != null && seeker.IsDone())
                    {
                        // Se não tem caminho, calcula um novo
                        seeker.StartPath(transform.position, startPosition, OnPathComplete);
                    }
                }
            }
        }
        else
        {
            // Reset do temporizador de retorno quando o player está em range
            timeOutOfRange = 0f;
            
            if (isReturning)
            {
                isReturning = false;
                currentState = EnemyState.Idle;
            }

            // Verifica se pode atacar
            if (distanceToPlayer <= attackRange && Time.time > lastAttackTime + attackCooldown && canChasePlayer)
            {
                Vector2 direction = (player.position - transform.position);
                
                // Define a direção do ataque com base na posição do jogador
                UpdateAnimationDirection(direction);
                
                currentState = EnemyState.Attacking;
                isMoving = false;
                attackAnimationComplete = false;
                lastAttackTime = Time.time;
                return;
            }

            // Atualiza o caminho periodicamente quando o player está em range
            if (isPlayerInRange)
            {
                pathUpdateTimer += Time.deltaTime;
                if (pathUpdateTimer >= pathUpdateInterval)
                {
                    pathUpdateTimer = 0f;
                    if (seeker.IsDone())
                    {
                        seeker.StartPath(transform.position, player.position, OnPathComplete);
                    }
                }
            }

            // Lógica de estados baseada na distância
            if (isPlayerInRange && distanceToPlayer > approachRange)
            {
                // Perseguição normal
                currentState = EnemyState.Moving;
                isMoving = true;
                
                if (path != null)
                {
                    FollowPath(1.0f);
                }
            }
            else if (isPlayerInRange && distanceToPlayer <= approachRange && distanceToPlayer > attackRange)
            {
                // Zona de aproximação - movimento mais cuidadoso
                currentState = EnemyState.Approaching;
                isMoving = true;
                
                // Verifica se há linha de visão direta para o jogador
                if (HasLineOfSightToPlayer())
                {
                    // Move-se diretamente em direção ao jogador se não houver obstáculos
                    Vector2 directDirection = (player.position - transform.position).normalized;
                    rb.MovePosition(rb.position + directDirection * moveSpeed * 0.8f * Time.deltaTime);
                    UpdateAnimationDirection(directDirection);
                }
                else if (path != null)
                {
                    // Segue o caminho com velocidade reduzida para maior precisão
                    FollowPath(0.7f);
                }
            }
            else if (!isPlayerInRange && currentState != EnemyState.Returning)
            {
                // Fora do alcance e não está retornando, fica parado
                StopMoving();
            }
        }
    }
    
    // Novo método para parar o movimento adequadamente
    void StopMoving()
    {
        currentState = EnemyState.Idle;
        isMoving = false;
        // Resetar a velocidade quando parar
        currentVelocity = Vector2.zero;
        lastDirection = Vector2.zero;
        // Garante que a animação pare imediatamente
        rb.linearVelocity = Vector2.zero;
    }
    
    // Método para finalizar o retorno à posição inicial
    void CompleteReturn()
    {
        // Para o movimento e retorna ao estado inicial
        currentState = EnemyState.Idle;
        isMoving = false;
        currentDirection = initialDirection;
        
        // Força a posição exata e para qualquer movimento residual
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        currentVelocity = Vector2.zero;
        lastDirection = Vector2.zero;
        
        isReturning = false;
        
        // Limpa o caminho 
        path = null;
        
        // Debug.Log("Inimigo retornou à posição inicial com sucesso");
    }
    
    void StartReturnToStart()
    {
        if (!isReturning && currentState != EnemyState.Returning)
        {
            // Se já estiver muito próximo da posição inicial, apenas finaliza o retorno
            if (Vector3.Distance(transform.position, startPosition) < returnPositionTolerance * 0.5f)
            {
                CompleteReturn();
                return;
            }
            
            isReturning = true;
            currentState = EnemyState.Returning;
            isMoving = true;
            
            // Calcula caminho para a posição inicial
            if (seeker != null && seeker.IsDone())
            {
                seeker.StartPath(transform.position, startPosition, OnPathComplete);
            }
            
            Debug.Log("Inimigo retornando à posição inicial");
        }
    }
    
    bool IsPlayerInOperationalArea()
    {
        if (!useOperationalArea || player == null)
            return true; // Se não usa área operacional, sempre retorna true
            
        // Verifica se o player está dentro de uma área de operação (usando Physics2D.OverlapPoint)
        Collider2D[] playerAreas = Physics2D.OverlapPointAll(player.position, operationalAreaLayer);
        
        foreach (var area in playerAreas)
        {
            // Se estamos usando IDs específicos de área
            if (useAreaID)
            {
                EnemyAreaManager areaManager = area.GetComponent<EnemyAreaManager>();
                if (areaManager != null && areaManager.areaID == assignedAreaID)
                {
                    return true;
                }
            }
            // Se estamos usando apenas tags
            else if (area.CompareTag(operationalAreaTag))
            {
                return true;
            }
        }
        
        return false;
    }
    
    bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;
        
        // Direção ao jogador
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        
        // Distância ao jogador
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Camada que contém obstáculos físicos (ajuste para seu projeto)
        LayerMask obstacleLayer = LayerMask.GetMask("Obstacles");
        
        // Verifica se há linha de visão direta
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer);
        
        // Se não houver colisão, há linha de visão direta
        return hit.collider == null;
    }
    
    void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }
    
    // Verifica se o movimento causaria uma colisão
    bool WouldCollide(Vector2 direction, float distance)
    {
        // Verifica se há colisão na direção do movimento
        LayerMask obstacleLayer = LayerMask.GetMask("Obstacles");
        RaycastHit2D hit = Physics2D.Raycast(rb.position, direction, distance, obstacleLayer);
        return hit.collider != null;
    }
    
    void FollowPath(float speedMultiplier = 1.0f)
    {
        if (path == null)
            return;
            
        if (currentWaypoint >= path.vectorPath.Count)
        {
            // reachedEndOfPath would be set to true here, but it's not used anywhere
            return;
        }

        // Direção base para o próximo waypoint
        Vector2 targetDirection = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;
        
        // Adiciona força de repulsão de obstáculos próximos
        Vector2 avoidanceForce = CalculateObstacleAvoidanceForce();
        
        // Se temos uma força de repulsão, combina com a direção original
        if (avoidanceForce.magnitude > 0)
        {
            targetDirection = (targetDirection + avoidanceForce).normalized;
        }
        
        // SUAVIZAÇÃO: Em vez de mudar a direção instantaneamente, suavize a transição
        Vector2 smoothedDirection = Vector2.SmoothDamp(
            lastDirection.magnitude > 0.1f ? lastDirection : targetDirection, 
            targetDirection, 
            ref currentVelocity, 
            smoothTime
        );
        
        // Armazena a última direção para próxima suavização
        lastDirection = smoothedDirection;
        
        // Verifica se o movimento causaria uma colisão
        if (WouldCollide(smoothedDirection, moveSpeed * speedMultiplier * Time.deltaTime * 1.1f))
        {
            // Tenta encontrar uma direção alternativa
            float angleStep = 30f;
            bool foundAlternative = false;
            
            for (float angle = angleStep; angle <= 180f; angle += angleStep)
            {
                // Tenta uma direção ligeiramente diferente (no sentido horário)
                Vector2 altDirection = Quaternion.Euler(0, 0, angle) * smoothedDirection;
                if (!WouldCollide(altDirection, moveSpeed * speedMultiplier * Time.deltaTime))
                {
                    smoothedDirection = altDirection;
                    foundAlternative = true;
                    break;
                }
                
                // Tenta uma direção ligeiramente diferente (no sentido anti-horário)
                altDirection = Quaternion.Euler(0, 0, -angle) * smoothedDirection;
                if (!WouldCollide(altDirection, moveSpeed * speedMultiplier * Time.deltaTime))
                {
                    smoothedDirection = altDirection;
                    foundAlternative = true;
                    break;
                }
            }
            
            // Se não encontrou alternativa, verifica se está muito próximo de uma parede
            if (!foundAlternative)
            {
                RaycastHit2D wallHit = Physics2D.Raycast(rb.position, smoothedDirection, 0.5f, LayerMask.GetMask("Obstacles"));
                if (wallHit.collider != null)
                {
                    // Se estiver prestes a colidir com uma parede, tenta deslizar ao longo dela
                    Vector2 wallNormal = wallHit.normal;
                    Vector2 slideDirection = Vector2.Reflect(smoothedDirection, wallNormal).normalized;
                    smoothedDirection = Vector2.Lerp(smoothedDirection, slideDirection, 0.8f);
                }
            }
        }
        
        // Move o inimigo com a direção suavizada e multiplicador de velocidade
        rb.MovePosition(rb.position + smoothedDirection * moveSpeed * speedMultiplier * Time.deltaTime);

        // Atualiza a direção de animação usando o método com threshold
        UpdateAnimationDirection(smoothedDirection);

        // Calcula a distância para o próximo waypoint
        float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);
        
        // Se chegou perto o suficiente do waypoint atual, avança para o próximo
        if (distance < nextWaypointDistance)
        {
            currentWaypoint++;
        }
        
        // Avaliação para curvas acentuadas - com suavização adicional para evitar saltos bruscos
        if (currentWaypoint < path.vectorPath.Count - 1)
        {
            Vector2 currentToNext = ((Vector2)path.vectorPath[currentWaypoint + 1] - (Vector2)path.vectorPath[currentWaypoint]).normalized;
            Vector2 positionToCurrent = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;
            
            // Se estiver fazendo uma curva maior que 45 graus e relativamente próximo
            if (Vector2.Dot(currentToNext, positionToCurrent) < 0.7f && distance < nextWaypointDistance * 1.5f)
            {
                currentWaypoint++;
            }
        }
    }
    
    Vector2 CalculateObstacleAvoidanceForce()
    {
        Vector2 avoidanceForce = Vector2.zero;
        Vector2 edgeFollowingForce = Vector2.zero;
        
        Collider2D[] obstacles = Physics2D.OverlapCircleAll(rb.position, obstacleDetectionRadius);
        
        foreach (Collider2D obstacle in obstacles)
        {
            // Ignora o próprio inimigo e o jogador
            if (obstacle.gameObject == gameObject || (player != null && obstacle.gameObject == player.gameObject))
                continue;
                
            // Ignora triggers
            if (obstacle.isTrigger)
                continue;
                
            // Ponto mais próximo na superfície do obstáculo
            Vector2 closestPoint = obstacle.ClosestPoint(transform.position);
            
            // Vetor de afastamento do obstáculo
            Vector2 awayFromObstacle = (rb.position - closestPoint).normalized;
            float distance = Vector2.Distance(rb.position, closestPoint);
            
            // A força é inversamente proporcional à distância (mais perto = mais força)
            float strength = 1f / Mathf.Max(0.1f, distance * nodeProximityMultiplier);
            avoidanceForce += awayFromObstacle * strength;
            
            // Adiciona uma força para seguir a borda do obstáculo (perpendicular à direção de afastamento)
            if (distance < obstacleDetectionRadius * 0.5f)
            {
                // Calcula uma força perpendicular ao vetor de afastamento (tangente à superfície)
                Vector2 edgeDirection = new Vector2(-awayFromObstacle.y, awayFromObstacle.x);
                
                // Orientamos a direção da borda para que seja mais próxima da direção desejada
                Vector2 desiredDirection = player != null ? 
                    ((Vector2)player.position - rb.position).normalized : Vector2.zero;
                    
                if (Vector2.Dot(edgeDirection, desiredDirection) < 0)
                    edgeDirection = -edgeDirection;
                    
                edgeFollowingForce += edgeDirection * (1f / Mathf.Max(0.5f, distance)) * edgeFollowingStrength;
            }
        }
        
        // Normaliza e aplica os fatores de força
        if (avoidanceForce.magnitude > 0)
        {
            avoidanceForce = avoidanceForce.normalized * obstacleAvoidanceStrength;
        }
        
        // Combina forças (evitação de obstáculos tem prioridade, mas seguir bordas ajuda a contornar)
        Vector2 combinedForce = avoidanceForce + edgeFollowingForce;
        if (combinedForce.magnitude > 1f)
            combinedForce = combinedForce.normalized;
        
        return combinedForce;
    }
    
    // Método de atualização da direção de animação com controle de threshold
    void UpdateAnimationDirection(Vector2 direction) 
    {
        // Usa um threshold para evitar mudanças de direção muito pequenas
        float threshold = 0.6f;
        
        // Só muda a direção se a magnitude do movimento for significativa
        if (direction.magnitude > 0.1f) 
        {
            FacingDirection newDirection;
            
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y) * threshold)
                newDirection = direction.x < 0 ? FacingDirection.Left : FacingDirection.Right;
            else if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x) * threshold)
                newDirection = direction.y < 0 ? FacingDirection.Down : FacingDirection.Up;
            else
                return; // Mantém a direção atual se estiver em uma zona de transição
                
            // Só atualiza se a direção for realmente diferente
            if (newDirection != currentDirection)
                currentDirection = newDirection;
        }
    }
    
    public void AttackAnimationFinished()
    {
        Debug.Log("Animação de ataque finalizada");
        attackAnimationComplete = true;
        currentState = EnemyState.Idle;
    }
    
    // Método para reiniciar o inimigo à sua posição original (útil para resetar níveis)
    public void ResetToStartPosition()
    {
        transform.position = startPosition;
        currentDirection = initialDirection;
        currentState = EnemyState.Idle;
        isMoving = false;
        path = null;
        rb.linearVelocity = Vector2.zero;
        currentVelocity = Vector2.zero;
        lastDirection = Vector2.zero;
    }
    
    // Desenha o caminho e os raios de detecção para depuração
    void OnDrawGizmos()
    {
        // Desenha o caminho
        if (path != null)
        {
            for (int i = currentWaypoint; i < path.vectorPath.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(path.vectorPath[i], 0.2f);

                if (i == currentWaypoint)
                {
                    Gizmos.DrawLine(transform.position, path.vectorPath[i]);
                }
                else
                {
                    Gizmos.DrawLine(path.vectorPath[i-1], path.vectorPath[i]);
                }
            }
        }
        
        // Desenha o raio de detecção de obstáculos
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, obstacleDetectionRadius);
        
        // Desenha o raio de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Desenha o raio de perseguição
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // Desenha o raio de aproximação
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, approachRange);
        
        // Desenha a posição inicial
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(startPosition, 0.3f);
        
        // Desenha a área de tolerância para retorno à posição inicial
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(startPosition, returnPositionTolerance);
        
        // Indica a direção inicial com uma seta
        Vector2 directionIndicator = Vector2.zero;
        switch (initialDirection)
        {
            case FacingDirection.Up: directionIndicator = Vector2.up; break;
            case FacingDirection.Down: directionIndicator = Vector2.down; break;
            case FacingDirection.Left: directionIndicator = Vector2.left; break;
            case FacingDirection.Right: directionIndicator = Vector2.right; break;
        }
        
        Gizmos.DrawLine(startPosition, (Vector2)startPosition + directionIndicator * 0.5f);
        
        // Mostra o ID da área atribuída se estiver usando áreas por ID
        if (useAreaID)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "Area ID: " + assignedAreaID);
        }
    }
}