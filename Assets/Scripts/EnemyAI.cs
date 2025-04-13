using UnityEngine;
using Pathfinding;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 2f;
    public float chaseRange = 5f;
    public float approachRange = 2.5f; // Nova variável - range intermediário entre chase e attack
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public float nextWaypointDistance = 0.7f; 
    public float smoothTime = 0.3f;

    public enum FacingDirection { Down, Up, Left, Right }
    public enum EnemyState { Idle, Moving, Approaching, Attacking }

    [HideInInspector] public FacingDirection currentDirection;
    [HideInInspector] public bool isMoving;
    [HideInInspector] public EnemyState currentState;
    [HideInInspector] public bool attackAnimationComplete = true;

    private Rigidbody2D rb;
    private float lastAttackTime;
    
    // Componentes de pathfinding
    private Seeker seeker;
    private Path path;
    private int currentWaypoint = 0;
    private bool reachedEndOfPath = false;
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
    
    // Novas variáveis para os aprimoramentos
    public float nodeProximityMultiplier = 1.2f;
    public float edgeFollowingStrength = 0.3f;
    private bool isInAttackMode = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        seeker = GetComponent<Seeker>();
        currentState = EnemyState.Idle;
        lastPosition = rb.position;
        
        // Inicia o primeiro cálculo de caminho
        if (player != null) {
            seeker.StartPath(transform.position, player.position, OnPathComplete);
        }
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

        // Verificação de "stuck" quando estiver se movendo
        if (currentState == EnemyState.Moving || currentState == EnemyState.Approaching)
        {
            if (Vector2.Distance(rb.position, lastPosition) < stuckCheckDistance)
            {
                stuckTime += Time.deltaTime;
                if (stuckTime > stuckThreshold && !isStuck)
                {
                    isStuck = true;
                    Debug.Log("Inimigo preso! Recalculando caminho...");
                    
                    // Adiciona um pequeno offset aleatório à posição do jogador para tentar um caminho diferente
                    Vector2 randomOffset = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 0.5f;
                    seeker.StartPath(transform.position, player.position + (Vector3)randomOffset, OnPathComplete);
                    
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

        // Verifica se pode atacar
        if (distanceToPlayer <= attackRange && Time.time > lastAttackTime + attackCooldown)
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

        // Atualiza o caminho periodicamente
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathUpdateInterval)
        {
            pathUpdateTimer = 0f;
            if (seeker.IsDone() && distanceToPlayer < chaseRange)
            {
                seeker.StartPath(transform.position, player.position, OnPathComplete);
            }
        }

        // Lógica de estados baseada na distância
        if (distanceToPlayer < chaseRange && distanceToPlayer > approachRange)
        {
            // Perseguição normal
            currentState = EnemyState.Moving;
            isMoving = true;
            isInAttackMode = false;
            
            if (path != null)
            {
                FollowPath(1.0f);
            }
        }
        else if (distanceToPlayer <= approachRange && distanceToPlayer > attackRange)
        {
            // Zona de aproximação - movimento mais cuidadoso
            currentState = EnemyState.Approaching;
            isMoving = true;
            isInAttackMode = true;
            
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
        else if (distanceToPlayer >= chaseRange)
        {
            // Fora do alcance, fica parado
            currentState = EnemyState.Idle;
            isMoving = false;
            isInAttackMode = false;
            // Resetar a velocidade quando parar
            currentVelocity = Vector2.zero;
            lastDirection = Vector2.zero;
        }
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
            reachedEndOfPath = true;
            return;
        }
        else
        {
            reachedEndOfPath = false;
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
    }
}